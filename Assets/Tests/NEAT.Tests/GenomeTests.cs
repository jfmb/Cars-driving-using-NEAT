using System;
using System.Linq;
using NeatCore.Configuration;
using NeatCore.Genetics;
using NeatCore.Network;
using NUnit.Framework;

namespace NeatTests
{
    [TestFixture]
    public class GenomeTests
    {
        private NeatConfig _config;
        private GenomeFactory _factory;
        private Random _random;

        [SetUp]
        public void SetUp()
        {
            _config = new NeatConfig
            {
                PopulationSize = 10,
                NumInputs = 3,
                NumOutputs = 2,
                RandomSeed = 42
            };
            _factory = new GenomeFactory();
            _random = new Random(42);
        }

        [Test]
        public void CreateInitialGenome_HasCorrectNodeCount()
        {
            var genome = _factory.CreateInitialGenome(0, _config, _random);

            // 1 bias + 3 inputs + 2 outputs = 6 nodes
            Assert.That(genome.NodeGenes.Count, Is.EqualTo(6));
        }

        [Test]
        public void CreateInitialGenome_HasCorrectNodeTypes()
        {
            var genome = _factory.CreateInitialGenome(0, _config, _random);

            Assert.That(genome.NodeGenes.Count(n => n.Type == NodeType.Bias), Is.EqualTo(1));
            Assert.That(genome.NodeGenes.Count(n => n.Type == NodeType.Input), Is.EqualTo(3));
            Assert.That(genome.NodeGenes.Count(n => n.Type == NodeType.Output), Is.EqualTo(2));
        }

        [Test]
        public void CreateInitialGenome_HasCorrectConnectionCount()
        {
            var genome = _factory.CreateInitialGenome(0, _config, _random);

            // Bias -> 2 outputs = 2 connections
            // 3 inputs -> 2 outputs = 6 connections
            // Total = 8 connections
            Assert.That(genome.ConnectionGenes.Count, Is.EqualTo(8));
        }

        [Test]
        public void CreateInitialGenome_AllConnectionsEnabled()
        {
            var genome = _factory.CreateInitialGenome(0, _config, _random);

            Assert.That(genome.ConnectionGenes.All(c => c.Enabled), Is.True);
        }

        [Test]
        public void CreateInitialGenome_NoHiddenNodes()
        {
            var genome = _factory.CreateInitialGenome(0, _config, _random);

            Assert.That(genome.NodeGenes.Any(n => n.Type == NodeType.Hidden), Is.False);
        }

        [Test]
        public void CreateInitialGenome_WeightsWithinRange()
        {
            var genome = _factory.CreateInitialGenome(0, _config, _random);

            foreach (var conn in genome.ConnectionGenes)
            {
                Assert.That(conn.Weight, Is.GreaterThanOrEqualTo(-1f));
                Assert.That(conn.Weight, Is.LessThanOrEqualTo(1f));
            }
        }

        [Test]
        public void InnovationNumbers_AreUnique()
        {
            var genome = _factory.CreateInitialGenome(0, _config, _random);

            var innovations = genome.ConnectionGenes.Select(c => c.Innovation).ToArray();
            Assert.That(innovations.Distinct().Count(), Is.EqualTo(innovations.Length));
        }

        [Test]
        public void InnovationNumbers_SameConnectionSameInnovation()
        {
            var innov1 = _factory.GetInnovationNumber(1, 5);
            var innov2 = _factory.GetInnovationNumber(1, 5);

            Assert.That(innov1, Is.EqualTo(innov2));
        }

        [Test]
        public void InnovationNumbers_DifferentConnectionDifferentInnovation()
        {
            var innov1 = _factory.GetInnovationNumber(1, 5);
            var innov2 = _factory.GetInnovationNumber(2, 5);

            Assert.That(innov1, Is.Not.EqualTo(innov2));
        }

        [Test]
        public void HasConnection_DetectsExistingConnection()
        {
            var genome = _factory.CreateInitialGenome(0, _config, _random);

            // Input 1 (ID=1) should connect to output 1 (ID=4)
            Assert.That(genome.HasConnection(1, 4), Is.True);
        }

        [Test]
        public void HasConnection_ReturnsFalseForNonExistent()
        {
            var genome = _factory.CreateInitialGenome(0, _config, _random);

            // No connection from output to input
            Assert.That(genome.HasConnection(4, 1), Is.False);
        }

        [Test]
        public void GenomeId_IsPreserved()
        {
            var genome = _factory.CreateInitialGenome(42, _config, _random);
            Assert.That(genome.Id, Is.EqualTo(42));
        }

        [Test]
        public void Genome_ConstructorOnlyRequiresId()
        {
            // The Genome constructor only requires an ID; numInputs/numOutputs are no longer stored.
            var genome = new Genome(99);
            Assert.That(genome.Id, Is.EqualTo(99));
            Assert.That(genome.NodeGenes.Count, Is.EqualTo(0));
            Assert.That(genome.ConnectionGenes.Count, Is.EqualTo(0));
        }
    }
}
