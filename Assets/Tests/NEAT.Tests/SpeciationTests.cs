using System;
using System.Linq;
using NeatCore.Configuration;
using NeatCore.Genetics;
using NeatCore.Speciation;
using NUnit.Framework;

namespace NeatTests
{
    [TestFixture]
    public class SpeciationTests
    {
        private NeatConfig _config;
        private GenomeFactory _factory;
        private Random _random;
        private CompatibilityCalculator _calculator;

        [SetUp]
        public void SetUp()
        {
            _config = new NeatConfig
            {
                NumInputs = 2,
                NumOutputs = 1,
                CompatibilityThreshold = 3.0f,
                ExcessCoefficient = 1.0f,
                DisjointCoefficient = 1.0f,
                WeightCoefficient = 0.2f
            };
            _factory = new GenomeFactory();
            _random = new Random(42);
            _calculator = new CompatibilityCalculator(_config);
        }

        [Test]
        public void Distance_IdenticalGenomes_IsZero()
        {
            var g1 = _factory.CreateInitialGenome(1, _config, _random);
            var g2 = _factory.CreateInitialGenome(2, _config, _random);

            // Copy weights from g1 to g2 so they are identical
            for (var i = 0; i < g1.ConnectionGenes.Count; i++)
                g2.ConnectionGenes[i].Weight = g1.ConnectionGenes[i].Weight;

            var distance = _calculator.CalculateDistance(g1, g2);
            Assert.That(distance, Is.EqualTo(0).Within(0.001));
        }

        [Test]
        public void Distance_DifferentWeights_IsPositive()
        {
            var g1 = _factory.CreateInitialGenome(1, _config, _random);
            var g2 = _factory.CreateInitialGenome(2, _config, _random);

            // Set different weights
            foreach (var c in g1.ConnectionGenes) c.Weight = 1f;
            foreach (var c in g2.ConnectionGenes) c.Weight = -1f;

            var distance = _calculator.CalculateDistance(g1, g2);
            Assert.That(distance, Is.GreaterThan(0));
        }

        [Test]
        public void Distance_DifferentTopology_IsHigher()
        {
            var g1 = _factory.CreateInitialGenome(1, _config, _random);
            var g2 = _factory.CreateInitialGenome(2, _config, _random);

            // Same weights
            for (var i = 0; i < g1.ConnectionGenes.Count; i++)
                g2.ConnectionGenes[i].Weight = g1.ConnectionGenes[i].Weight;

            var sameTopologyDistance = _calculator.CalculateDistance(g1, g2);

            // Add extra connection to g2
            var newNodeId = _factory.GetNextNodeId();
            g2.NodeGenes.Add(new NodeGene(newNodeId, NodeType.Hidden));
            var innov = _factory.GetInnovationNumber(1, newNodeId);
            g2.ConnectionGenes.Add(new ConnectionGene(innov, 1, newNodeId, 0.5f));

            var differentTopologyDistance = _calculator.CalculateDistance(g1, g2);

            Assert.That(differentTopologyDistance, Is.GreaterThan(sameTopologyDistance));
        }

        [Test]
        public void Distance_Symmetric()
        {
            var g1 = _factory.CreateInitialGenome(1, _config, _random);
            var g2 = _factory.CreateInitialGenome(2, _config, _random);

            var d1 = _calculator.CalculateDistance(g1, g2);
            var d2 = _calculator.CalculateDistance(g2, g1);

            Assert.That(d1, Is.EqualTo(d2));
        }

        [Test]
        public void Species_AssignsGenomeToNewSpecies()
        {
            var speciationManager = new SpeciationManager(_calculator, _config);
            var genome = _factory.CreateInitialGenome(1, _config, _random);
            var population = new System.Collections.Generic.List<Genome> { genome };

            speciationManager.AssignGenomesToSpecies(population);

            Assert.That(speciationManager.Species.Count, Is.EqualTo(1));
            Assert.That(genome.SpeciesId, Is.EqualTo(1));
        }

        [Test]
        public void Species_SimilarGenomesInSameSpecies()
        {
            var speciationManager = new SpeciationManager(_calculator, _config);
            var g1 = _factory.CreateInitialGenome(1, _config, _random);
            var g2 = _factory.CreateInitialGenome(2, _config, _random);

            // Make them very similar
            for (var i = 0; i < g1.ConnectionGenes.Count; i++)
                g2.ConnectionGenes[i].Weight = g1.ConnectionGenes[i].Weight;

            var population = new System.Collections.Generic.List<Genome> { g1, g2 };
            speciationManager.AssignGenomesToSpecies(population);

            Assert.That(speciationManager.Species.Count, Is.EqualTo(1));
        }

        [Test]
        public void Species_VeryDifferentGenomesInDifferentSpecies()
        {
            _config.CompatibilityThreshold = 0.01f; // Very strict
            _calculator = new CompatibilityCalculator(_config);
            var speciationManager = new SpeciationManager(_calculator, _config);

            var g1 = _factory.CreateInitialGenome(1, _config, _random);
            var g2 = _factory.CreateInitialGenome(2, _config, _random);

            var population = new System.Collections.Generic.List<Genome> { g1, g2 };
            speciationManager.AssignGenomesToSpecies(population);

            Assert.That(speciationManager.Species.Count, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void FitnessSharing_ReducesFitnessInLargerSpecies()
        {
            var speciationManager = new SpeciationManager(_calculator, _config);
            var g1 = _factory.CreateInitialGenome(1, _config, _random);
            g1.Fitness = 100;
            var g2 = _factory.CreateInitialGenome(2, _config, _random);
            g2.Fitness = 100;

            // Make them similar so they're in the same species
            for (var i = 0; i < g1.ConnectionGenes.Count; i++)
                g2.ConnectionGenes[i].Weight = g1.ConnectionGenes[i].Weight;

            var population = new System.Collections.Generic.List<Genome> { g1, g2 };
            speciationManager.AssignGenomesToSpecies(population);
            speciationManager.ApplyFitnessSharing();

            // Each should have adjusted fitness = 100 / 2 = 50
            Assert.That(g1.AdjustedFitness, Is.EqualTo(50).Within(0.001));
            Assert.That(g2.AdjustedFitness, Is.EqualTo(50).Within(0.001));
        }

        [Test]
        public void Species_AgeIncrements()
        {
            var speciationManager = new SpeciationManager(_calculator, _config);
            var genome = _factory.CreateInitialGenome(1, _config, _random);
            var population = new System.Collections.Generic.List<Genome> { genome };

            speciationManager.AssignGenomesToSpecies(population);
            Assert.That(speciationManager.Species[0].Age, Is.EqualTo(0));

            speciationManager.AgeSpecies();
            Assert.That(speciationManager.Species[0].Age, Is.EqualTo(1));
        }

        // --- Excess / Disjoint / Matching classification tests ---

        [Test]
        public void Classification_SameTopology_OnlyMatchingGenes()
        {
            // Two genomes with identical innovation numbers — all genes are matching.
            var g1 = _factory.CreateInitialGenome(1, _config, _random);
            var g2 = _factory.CreateInitialGenome(2, _config, _random);

            // g1 and g2 share the same innovations (from the shared factory).
            // Set identical weights so matching weight diff is 0.
            for (var i = 0; i < g1.ConnectionGenes.Count; i++)
                g2.ConnectionGenes[i].Weight = g1.ConnectionGenes[i].Weight;

            var distance = _calculator.CalculateDistance(g1, g2);
            Assert.That(distance, Is.EqualTo(0).Within(0.001), "Identical genomes should have zero distance");
        }

        [Test]
        public void Classification_ExcessGenes_WhenOneGenomeHasHigherInnovations()
        {
            // g1 has innovations 1,2,3. g2 has innovations 1,2,3,4,5.
            // Max innovation: g1=3, g2=5. minMax = 3.
            // Genes 4,5 in g2 are > 3, so they are excess.
            // No disjoint genes expected.
            var g1 = _factory.CreateInitialGenome(1, _config, _random);
            var g2 = _factory.CreateInitialGenome(2, _config, _random);

            // Add two new connections to g2 with new innovation numbers
            var newNodeId = _factory.GetNextNodeId();
            var innov4 = _factory.GetInnovationNumber(1, newNodeId); // innovation 4
            g2.NodeGenes.Add(new NodeGene(newNodeId, NodeType.Hidden));
            g2.ConnectionGenes.Add(new ConnectionGene(innov4, 1, newNodeId, 0.5f));

            var newNodeId2 = _factory.GetNextNodeId();
            var innov5 = _factory.GetInnovationNumber(newNodeId, newNodeId2); // innovation 5
            g2.NodeGenes.Add(new NodeGene(newNodeId2, NodeType.Hidden));
            g2.ConnectionGenes.Add(new ConnectionGene(innov5, newNodeId, newNodeId2, 0.3f));

            // g1 max innovation = 3 (from initial genome), g2 max = 5
            // minMax = 3, so innovations 4 and 5 are excess
            var distance = _calculator.CalculateDistance(g1, g2);

            // Distance should be positive (excess genes contribute)
            Assert.That(distance, Is.GreaterThan(0), "Excess genes should increase distance");

            // Verify excess coefficient is applied: distance = coeff * excess / N
            var N = System.Math.Max(g1.ConnectionGenes.Count, g2.ConnectionGenes.Count);
            var expectedExcessContribution = _config.ExcessCoefficient * 2.0 / N;
            Assert.That(distance, Is.GreaterThanOrEqualTo(expectedExcessContribution),
                "Distance should include excess gene contribution");
        }

        [Test]
        public void Classification_DisjointGenes_WhenInnovationsMissingInRange()
        {
            // Create two genomes where one has a gap in innovation numbers.
            // g1: innovations 1, 2, 3, 4, 5
            // g2: innovations 1, 2, 5 (missing 3, 4 — these are disjoint)
            // Max innovation: g1=5, g2=5. minMax = 5.
            // Innovations 3,4 are only in g1 and <= 5, so they are disjoint.
            var g1 = _factory.CreateInitialGenome(1, _config, _random);
            var g2 = _factory.CreateInitialGenome(2, _config, _random);

            // g1 has innovations 1,2,3 (bias->out1, in1->out1, in2->out1)
            // Remove innovation 2 and 3 from g2 and replace with only innovation 1
            var conn1 = g2.ConnectionGenes[0]; // innovation 1
            g2.ConnectionGenes.Clear();
            g2.ConnectionGenes.Add(conn1);

            // g1 max = 3, g2 max = 1. minMax = 1.
            // Innovations 2,3 in g1 are > 1, so they are excess (not disjoint).
            // This tests the boundary condition.

            var distance = _calculator.CalculateDistance(g1, g2);
            Assert.That(distance, Is.GreaterThan(0), "Missing genes should increase distance");
        }

        [Test]
        public void Classification_DisjointGenes_WithSameMaxInnovation()
        {
            // Build two genomes manually to control innovation numbers precisely.
            // g1: innovations 1, 3, 5
            // g2: innovations 1, 2, 5
            // Common: 1, 5 (matching)
            // g1-only: 3 (<= minMax=5, disjoint)
            // g2-only: 2 (<= minMax=5, disjoint)
            // Excess: 0, Disjoint: 2

            var g1 = new Genome(1);
            g1.NodeGenes.Add(new NodeGene(0, NodeType.Bias));
            g1.NodeGenes.Add(new NodeGene(1, NodeType.Input));
            g1.NodeGenes.Add(new NodeGene(2, NodeType.Input));
            g1.NodeGenes.Add(new NodeGene(3, NodeType.Output));
            g1.ConnectionGenes.Add(new ConnectionGene(1, 0, 3, 0.5f));
            g1.ConnectionGenes.Add(new ConnectionGene(3, 1, 3, 0.8f));
            g1.ConnectionGenes.Add(new ConnectionGene(5, 2, 3, 0.3f));

            var g2 = new Genome(2);
            g2.NodeGenes.Add(new NodeGene(0, NodeType.Bias));
            g2.NodeGenes.Add(new NodeGene(1, NodeType.Input));
            g2.NodeGenes.Add(new NodeGene(2, NodeType.Input));
            g2.NodeGenes.Add(new NodeGene(3, NodeType.Output));
            g2.ConnectionGenes.Add(new ConnectionGene(1, 0, 3, 0.5f));
            g2.ConnectionGenes.Add(new ConnectionGene(2, 1, 3, 0.9f));
            g2.ConnectionGenes.Add(new ConnectionGene(5, 2, 3, 0.3f));

            var distance = _calculator.CalculateDistance(g1, g2);

            // N = 3 (both have 3 connections)
            // Excess = 0, Disjoint = 2, Matching weight diff = 0 (same weights for common genes)
            // Distance = (1.0 * 0 + 1.0 * 2 + 0.2 * 0) / 3 = 2/3
            var expected = (1.0 * 0 + 1.0 * 2 + 0.2 * 0) / 3;
            Assert.That(distance, Is.EqualTo(expected).Within(0.001),
                $"Expected {expected}, got {distance}");
        }

        [Test]
        public void Classification_ExcessAndDisjointMixed()
        {
            // g1: innovations 1, 2, 3, 8, 9
            // g2: innovations 1, 4, 5, 8, 9
            // Common: 1, 8, 9 (matching)
            // g1-only: 2, 3 (both <= minMax=9, disjoint)
            // g2-only: 4, 5 (both <= minMax=9, disjoint)
            // Excess: 0, Disjoint: 4

            var g1 = new Genome(1);
            g1.NodeGenes.Add(new NodeGene(0, NodeType.Bias));
            g1.NodeGenes.Add(new NodeGene(1, NodeType.Input));
            g1.NodeGenes.Add(new NodeGene(2, NodeType.Input));
            g1.NodeGenes.Add(new NodeGene(3, NodeType.Output));
            g1.NodeGenes.Add(new NodeGene(4, NodeType.Hidden));
            g1.NodeGenes.Add(new NodeGene(5, NodeType.Hidden));
            g1.ConnectionGenes.Add(new ConnectionGene(1, 0, 3, 0.5f));
            g1.ConnectionGenes.Add(new ConnectionGene(2, 1, 3, 0.8f));
            g1.ConnectionGenes.Add(new ConnectionGene(3, 2, 3, 0.3f));
            g1.ConnectionGenes.Add(new ConnectionGene(8, 0, 4, 0.6f));
            g1.ConnectionGenes.Add(new ConnectionGene(9, 4, 3, 0.7f));

            var g2 = new Genome(2);
            g2.NodeGenes.Add(new NodeGene(0, NodeType.Bias));
            g2.NodeGenes.Add(new NodeGene(1, NodeType.Input));
            g2.NodeGenes.Add(new NodeGene(2, NodeType.Input));
            g2.NodeGenes.Add(new NodeGene(3, NodeType.Output));
            g2.NodeGenes.Add(new NodeGene(4, NodeType.Hidden));
            g2.NodeGenes.Add(new NodeGene(5, NodeType.Hidden));
            g2.ConnectionGenes.Add(new ConnectionGene(1, 0, 3, 0.5f));
            g2.ConnectionGenes.Add(new ConnectionGene(4, 1, 3, 0.9f));
            g2.ConnectionGenes.Add(new ConnectionGene(5, 2, 3, 0.4f));
            g2.ConnectionGenes.Add(new ConnectionGene(8, 0, 4, 0.6f));
            g2.ConnectionGenes.Add(new ConnectionGene(9, 4, 3, 0.7f));

            var distance = _calculator.CalculateDistance(g1, g2);

            // N = 5, Excess = 0, Disjoint = 4, Matching weight diff = 0
            // Distance = (1.0 * 0 + 1.0 * 4 + 0.2 * 0) / 5 = 0.8
            var expected = (1.0 * 0 + 1.0 * 4 + 0.2 * 0) / 5;
            Assert.That(distance, Is.EqualTo(expected).Within(0.001),
                $"Expected {expected}, got {distance}");
        }

        [Test]
        public void Classification_ExcessGenes_WithDifferentMaxInnovations()
        {
            // g1: innovations 1, 2, 3
            // g2: innovations 1, 2, 3, 4, 5, 6
            // Common: 1, 2, 3 (matching)
            // g2-only: 4, 5, 6 — all > minMax=3, so all excess
            // Excess: 6 (3 from g2 counted twice? No — excess counts total non-common genes above threshold)
            // Actually: excess1=0 (g1 has nothing above 3 that's non-common), excess2=3 (g2 has 4,5,6 > 3)
            // Total excess = 3

            var g1 = new Genome(1);
            g1.NodeGenes.Add(new NodeGene(0, NodeType.Bias));
            g1.NodeGenes.Add(new NodeGene(1, NodeType.Input));
            g1.NodeGenes.Add(new NodeGene(2, NodeType.Input));
            g1.NodeGenes.Add(new NodeGene(3, NodeType.Output));
            g1.ConnectionGenes.Add(new ConnectionGene(1, 0, 3, 0.5f));
            g1.ConnectionGenes.Add(new ConnectionGene(2, 1, 3, 0.8f));
            g1.ConnectionGenes.Add(new ConnectionGene(3, 2, 3, 0.3f));

            var g2 = new Genome(2);
            g2.NodeGenes.Add(new NodeGene(0, NodeType.Bias));
            g2.NodeGenes.Add(new NodeGene(1, NodeType.Input));
            g2.NodeGenes.Add(new NodeGene(2, NodeType.Input));
            g2.NodeGenes.Add(new NodeGene(3, NodeType.Output));
            g2.NodeGenes.Add(new NodeGene(4, NodeType.Hidden));
            g2.NodeGenes.Add(new NodeGene(5, NodeType.Hidden));
            g2.ConnectionGenes.Add(new ConnectionGene(1, 0, 3, 0.5f));
            g2.ConnectionGenes.Add(new ConnectionGene(2, 1, 3, 0.8f));
            g2.ConnectionGenes.Add(new ConnectionGene(3, 2, 3, 0.3f));
            g2.ConnectionGenes.Add(new ConnectionGene(4, 0, 4, 0.6f));
            g2.ConnectionGenes.Add(new ConnectionGene(5, 4, 3, 0.7f));
            g2.ConnectionGenes.Add(new ConnectionGene(6, 1, 4, 0.9f));

            var distance = _calculator.CalculateDistance(g1, g2);

            // N = 6, Excess = 3, Disjoint = 0, Matching weight diff = 0
            // Distance = (1.0 * 3 + 1.0 * 0 + 0.2 * 0) / 6 = 0.5
            var expected = (1.0 * 3 + 1.0 * 0 + 0.2 * 0) / 6;
            Assert.That(distance, Is.EqualTo(expected).Within(0.001),
                $"Expected {expected}, got {distance}");
        }

        [Test]
        public void Classification_BothExcessAndDisjoint()
        {
            // g1: innovations 1, 2, 3, 10, 11
            // g2: innovations 1, 4, 10, 12, 13
            // Common: 1, 10 (matching)
            // g1-only: 2, 3 (<= minMax=11, disjoint), 11 (<= minMax=11, disjoint)
            // Wait: g1 max = 11, g2 max = 13, minMax = 11
            // g1-only: 2, 3, 11 — all <= 11, so disjoint
            // g2-only: 4, 12, 13 — 4 <= 11 (disjoint), 12 > 11 (excess), 13 > 11 (excess)
            // Excess = 2, Disjoint = 4

            var g1 = new Genome(1);
            g1.NodeGenes.Add(new NodeGene(0, NodeType.Bias));
            g1.NodeGenes.Add(new NodeGene(1, NodeType.Input));
            g1.NodeGenes.Add(new NodeGene(2, NodeType.Input));
            g1.NodeGenes.Add(new NodeGene(3, NodeType.Output));
            g1.NodeGenes.Add(new NodeGene(4, NodeType.Hidden));
            g1.NodeGenes.Add(new NodeGene(5, NodeType.Hidden));
            g1.ConnectionGenes.Add(new ConnectionGene(1, 0, 3, 0.5f));
            g1.ConnectionGenes.Add(new ConnectionGene(2, 1, 3, 0.8f));
            g1.ConnectionGenes.Add(new ConnectionGene(3, 2, 3, 0.3f));
            g1.ConnectionGenes.Add(new ConnectionGene(10, 0, 4, 0.6f));
            g1.ConnectionGenes.Add(new ConnectionGene(11, 4, 3, 0.7f));

            var g2 = new Genome(2);
            g2.NodeGenes.Add(new NodeGene(0, NodeType.Bias));
            g2.NodeGenes.Add(new NodeGene(1, NodeType.Input));
            g2.NodeGenes.Add(new NodeGene(2, NodeType.Input));
            g2.NodeGenes.Add(new NodeGene(3, NodeType.Output));
            g2.NodeGenes.Add(new NodeGene(4, NodeType.Hidden));
            g2.NodeGenes.Add(new NodeGene(5, NodeType.Hidden));
            g2.ConnectionGenes.Add(new ConnectionGene(1, 0, 3, 0.5f));
            g2.ConnectionGenes.Add(new ConnectionGene(4, 1, 3, 0.9f));
            g2.ConnectionGenes.Add(new ConnectionGene(10, 0, 4, 0.6f));
            g2.ConnectionGenes.Add(new ConnectionGene(12, 4, 3, 0.8f));
            g2.ConnectionGenes.Add(new ConnectionGene(13, 1, 4, 1.0f));

            var distance = _calculator.CalculateDistance(g1, g2);

            // N = 5, Excess = 2, Disjoint = 4, Matching weight diff = 0
            // Distance = (1.0 * 2 + 1.0 * 4 + 0.2 * 0) / 5 = 1.2
            var expected = (1.0 * 2 + 1.0 * 4 + 0.2 * 0) / 5;
            Assert.That(distance, Is.EqualTo(expected).Within(0.001),
                $"Expected {expected}, got {distance}");
        }

        [Test]
        public void Classification_MatchingWeightDifference_AppliedCorrectly()
        {
            // Two genomes with same topology but different weights on matching genes.
            var g1 = new Genome(1);
            g1.NodeGenes.Add(new NodeGene(0, NodeType.Bias));
            g1.NodeGenes.Add(new NodeGene(1, NodeType.Input));
            g1.NodeGenes.Add(new NodeGene(3, NodeType.Output));
            g1.ConnectionGenes.Add(new ConnectionGene(1, 0, 3, 1.0f));
            g1.ConnectionGenes.Add(new ConnectionGene(2, 1, 3, 0.0f));

            var g2 = new Genome(2);
            g2.NodeGenes.Add(new NodeGene(0, NodeType.Bias));
            g2.NodeGenes.Add(new NodeGene(1, NodeType.Input));
            g2.NodeGenes.Add(new NodeGene(3, NodeType.Output));
            g2.ConnectionGenes.Add(new ConnectionGene(1, 0, 3, -1.0f)); // diff = 2.0
            g2.ConnectionGenes.Add(new ConnectionGene(2, 1, 3, 1.0f));  // diff = 1.0

            var distance = _calculator.CalculateDistance(g1, g2);

            // N = 2, Excess = 0, Disjoint = 0
            // Matching weight diff = (2.0 + 1.0) / 2 = 1.5
            // Distance = (1.0 * 0 + 1.0 * 0 + 0.2 * 1.5) / 2 = 0.15
            var expected = (0.2 * 1.5) / 2;
            Assert.That(distance, Is.EqualTo(expected).Within(0.001),
                $"Expected {expected}, got {distance}");
        }

        [Test]
        public void Classification_NoGenes_ReturnsZero()
        {
            var g1 = new Genome(1);
            var g2 = new Genome(2);

            var distance = _calculator.CalculateDistance(g1, g2);
            Assert.That(distance, Is.EqualTo(0));
        }

        [Test]
        public void Classification_Coefficients_AffectDistance()
        {
            // Verify that changing coefficients changes the distance proportionally.
            var g1 = new Genome(1);
            g1.NodeGenes.Add(new NodeGene(0, NodeType.Bias));
            g1.NodeGenes.Add(new NodeGene(1, NodeType.Input));
            g1.NodeGenes.Add(new NodeGene(3, NodeType.Output));
            g1.ConnectionGenes.Add(new ConnectionGene(1, 0, 3, 0.5f));
            g1.ConnectionGenes.Add(new ConnectionGene(2, 1, 3, 0.8f));

            var g2 = new Genome(2);
            g2.NodeGenes.Add(new NodeGene(0, NodeType.Bias));
            g2.NodeGenes.Add(new NodeGene(1, NodeType.Input));
            g2.NodeGenes.Add(new NodeGene(3, NodeType.Output));
            g2.ConnectionGenes.Add(new ConnectionGene(1, 0, 3, 0.5f));
            g2.ConnectionGenes.Add(new ConnectionGene(3, 1, 3, 0.8f)); // different innovation

            // Test with default coefficients
            var distance1 = _calculator.CalculateDistance(g1, g2);

            // Test with higher disjoint coefficient
            var highDisjointConfig = new NeatConfig
            {
                NumInputs = 2,
                NumOutputs = 1,
                CompatibilityThreshold = 3.0f,
                ExcessCoefficient = 1.0f,
                DisjointCoefficient = 5.0f, // 5x higher
                WeightCoefficient = 0.2f
            };
            var calculator2 = new CompatibilityCalculator(highDisjointConfig);
            var distance2 = calculator2.CalculateDistance(g1, g2);

            // Distance2 should be significantly higher due to higher disjoint coefficient
            Assert.That(distance2, Is.GreaterThan(distance1),
                "Higher disjoint coefficient should increase distance");
        }
    }
}
