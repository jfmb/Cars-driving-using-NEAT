using System;
using System.Linq;
using NeatCore.Configuration;
using NeatCore.Crossover;
using NeatCore.Genetics;
using NUnit.Framework;

namespace NeatTests
{
    [TestFixture]
    public class CrossoverTests
    {
        private NeatConfig _config;
        private GenomeFactory _factory;
        private Random _random;
        private GenomeCrossover _crossover;

        [SetUp]
        public void SetUp()
        {
            _config = new NeatConfig
            {
                NumInputs = 2,
                NumOutputs = 1,
                CrossoverProbability = 0.75f
            };
            _factory = new GenomeFactory();
            _random = new Random(42);
            _crossover = new GenomeCrossover(_config, _random);
        }

        [Test]
        public void Crossover_ReturnsTwoChildren()
        {
            var g1 = _factory.CreateInitialGenome(1, _config, _random);
            g1.Fitness = 10;
            var g2 = _factory.CreateInitialGenome(2, _config, _random);
            g2.Fitness = 5;

            var (child1, child2) = _crossover.Crossover(g1, g2);

            Assert.That(child1, Is.Not.Null);
            Assert.That(child2, Is.Not.Null);
            Assert.That(child1, Is.Not.SameAs(child2));
        }

        [Test]
        public void Crossover_ChildrenHaveNodesFromParents()
        {
            var g1 = _factory.CreateInitialGenome(1, _config, _random);
            g1.Fitness = 10;
            var g2 = _factory.CreateInitialGenome(2, _config, _random);
            g2.Fitness = 5;

            var (child1, _) = _crossover.Crossover(g1, g2);

            var parentNodeIds = new System.Collections.Generic.HashSet<int>();
            foreach (var n in g1.NodeGenes) parentNodeIds.Add(n.Id);
            foreach (var n in g2.NodeGenes) parentNodeIds.Add(n.Id);

            foreach (var node in child1.NodeGenes)
            {
                Assert.That(parentNodeIds, Does.Contain(node.Id));
            }
        }

        [Test]
        public void Crossover_ChildrenHaveConnectionsFromParents()
        {
            var g1 = _factory.CreateInitialGenome(1, _config, _random);
            g1.Fitness = 10;
            var g2 = _factory.CreateInitialGenome(2, _config, _random);
            g2.Fitness = 5;

            var (child1, _) = _crossover.Crossover(g1, g2);

            // Child should have connections whose innovations come from parents
            var parentInnovations = new System.Collections.Generic.HashSet<int>();
            foreach (var c in g1.ConnectionGenes) parentInnovations.Add(c.Innovation);
            foreach (var c in g2.ConnectionGenes) parentInnovations.Add(c.Innovation);

            foreach (var conn in child1.ConnectionGenes)
            {
                Assert.That(parentInnovations, Does.Contain(conn.Innovation));
            }
        }

        [Test]
        public void Crossover_DifferentTopologies_HandlesDisjointGenes()
        {
            var g1 = _factory.CreateInitialGenome(1, _config, _random);
            g1.Fitness = 10;

            var g2 = _factory.CreateInitialGenome(2, _config, _random);
            g2.Fitness = 5;
            // Add extra connection to g2
            var newNodeId = _factory.GetNextNodeId();
            g2.NodeGenes.Add(new NodeGene(newNodeId, NodeType.Hidden));
            var innov = _factory.GetInnovationNumber(1, newNodeId);
            g2.ConnectionGenes.Add(new ConnectionGene(innov, 1, newNodeId, 0.5f));

            var (child1, child2) = _crossover.Crossover(g1, g2);

            Assert.That(child1, Is.Not.Null);
            Assert.That(child2, Is.Not.Null);
        }

        [Test]
        public void Crossover_PrefersFitterParent()
        {
            var g1 = _factory.CreateInitialGenome(1, _config, _random);
            g1.Fitness = 100;

            var g2 = _factory.CreateInitialGenome(2, _config, _random);
            g2.Fitness = 1;

            var (child1, _) = _crossover.Crossover(g1, g2);

            // Child should have at least as many nodes as the fitter parent
            Assert.That(child1.NodeGenes.Count, Is.GreaterThanOrEqualTo(g1.NodeGenes.Count));
        }

        [Test]
        public void Crossover_ChildGeneration_Incremented()
        {
            var g1 = _factory.CreateInitialGenome(1, _config, _random);
            g1.Fitness = 10;
            g1.Generation = 5;
            var g2 = _factory.CreateInitialGenome(2, _config, _random);
            g2.Fitness = 5;
            g2.Generation = 5;

            var (child1, _) = _crossover.Crossover(g1, g2);

            Assert.That(child1.Generation, Is.GreaterThan(5));
        }

        [Test]
        public void Crossover_DropsExcessGenesFromLessFitParent()
        {
            // Build two genomes with different topologies.
            // g1 (fitter) has innovations 1, 2, 3.
            // g2 (less fit) has innovations 1, 2, 3, 4, 5 (4 and 5 are excess from g2's perspective).
            // The child should NOT inherit innovations 4 and 5 from the less-fit parent.
            var factory = new GenomeFactory();
            var config = new NeatConfig { NumInputs = 2, NumOutputs = 1 };

            var g1 = factory.CreateInitialGenome(1, config, new Random(42));
            g1.Fitness = 100; // More fit

            var g2 = factory.CreateInitialGenome(2, config, new Random(43));
            g2.Fitness = 10;  // Less fit

            // Add extra connections to g2 with new innovation numbers
            var newNodeId = factory.GetNextNodeId();
            g2.NodeGenes.Add(new NodeGene(newNodeId, NeatCore.Genetics.NodeType.Hidden));
            var excessInnov = factory.GetInnovationNumber(1, newNodeId);
            g2.ConnectionGenes.Add(new ConnectionGene(excessInnov, 1, newNodeId, 0.5f));

            var crossover = new GenomeCrossover(config, new Random(99));
            var (child1, child2) = crossover.Crossover(g1, g2);

            // Child should not have the excess innovation from the less-fit parent
            var childHasExcess = child1.ConnectionGenes.Any(c => c.Innovation == excessInnov);
            Assert.That(childHasExcess, Is.False, "Child should not inherit excess genes from less-fit parent");

            var child2HasExcess = child2.ConnectionGenes.Any(c => c.Innovation == excessInnov);
            Assert.That(child2HasExcess, Is.False, "Child2 should not inherit excess genes from less-fit parent");
        }

        [Test]
        public void Crossover_EqualFitness_ParentsHandledSensibly()
        {
            // When parents have equal fitness, crossover should still produce valid children.
            var g1 = _factory.CreateInitialGenome(1, _config, _random);
            g1.Fitness = 50;
            var g2 = _factory.CreateInitialGenome(2, _config, _random);
            g2.Fitness = 50;

            var (child1, child2) = _crossover.Crossover(g1, g2);

            Assert.That(child1, Is.Not.Null);
            Assert.That(child2, Is.Not.Null);
            Assert.That(child1.ConnectionGenes.Count, Is.GreaterThan(0));
            Assert.That(child2.ConnectionGenes.Count, Is.GreaterThan(0));
        }
    }
}
