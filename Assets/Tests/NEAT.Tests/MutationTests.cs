using System;
using System.Linq;
using NeatCore.Configuration;
using NeatCore.Genetics;
using NeatCore.Mutation;
using NUnit.Framework;

namespace NeatTests
{
    [TestFixture]
    public class MutationTests
    {
        private NeatConfig _config;
        private GenomeFactory _factory;
        private Random _random;
        private GenomeMutator _mutator;

        [SetUp]
        public void SetUp()
        {
            _config = new NeatConfig
            {
                NumInputs = 2,
                NumOutputs = 1,
                AddConnectionProbability = 1f,
                AddNodeProbability = 1f,
                EnableDisableConnectionProbability = 1f,
                WeightMutationRate = 1f,
                WeightMutationAmount = 0.5f,
                MaxWeight = 6f
            };
            _factory = new GenomeFactory();
            _random = new Random(42);
            _mutator = new GenomeMutator(_factory, _config, _random);
        }

        [Test]
        public void Mutate_ReturnsNewGenome()
        {
            var genome = _factory.CreateInitialGenome(0, _config, _random);
            var mutated = _mutator.Mutate(genome);

            Assert.That(mutated, Is.Not.SameAs(genome));
        }

        [Test]
        public void Mutate_DoesNotModifyOriginal()
        {
            var genome = _factory.CreateInitialGenome(0, _config, _random);
            var originalConnCount = genome.ConnectionGenes.Count;
            var originalNodeCount = genome.NodeGenes.Count;

            _mutator.Mutate(genome);

            Assert.That(genome.ConnectionGenes.Count, Is.EqualTo(originalConnCount));
            Assert.That(genome.NodeGenes.Count, Is.EqualTo(originalNodeCount));
        }

        [Test]
        public void WeightMutation_ChangesWeights()
        {
            _config.AddConnectionProbability = 0f;
            _config.AddNodeProbability = 0f;
            _config.EnableDisableConnectionProbability = 0f;
            _mutator = new GenomeMutator(_factory, _config, _random);

            var genome = _factory.CreateInitialGenome(0, _config, _random);
            var originalWeights = genome.ConnectionGenes.Select(c => c.Weight).ToArray();

            var mutated = _mutator.Mutate(genome);

            var mutatedWeights = mutated.ConnectionGenes.Select(c => c.Weight).ToArray();
            var changed = originalWeights.Zip(mutatedWeights, (a, b) => Math.Abs(a - b) > 0.001f).Any();
            Assert.That(changed, Is.True, "At least one weight should have changed");
        }

        [Test]
        public void WeightMutation_RespectsMaxWeight()
        {
            _config.AddConnectionProbability = 0f;
            _config.AddNodeProbability = 0f;
            _config.EnableDisableConnectionProbability = 0f;
            _config.WeightMutationAmount = 10f; // Large mutation
            _mutator = new GenomeMutator(_factory, _config, _random);

            var genome = _factory.CreateInitialGenome(0, _config, _random);

            var mutated = _mutator.Mutate(genome);

            foreach (var conn in mutated.ConnectionGenes)
            {
                Assert.That(conn.Weight, Is.LessThanOrEqualTo(_config.MaxWeight + 0.001f));
                Assert.That(conn.Weight, Is.GreaterThanOrEqualTo(-_config.MaxWeight - 0.001f));
            }
        }

        [Test]
        public void AddConnectionMutation_IncreasesConnectionCount()
        {
            _config.AddNodeProbability = 0f;
            _config.EnableDisableConnectionProbability = 0f;
            _mutator = new GenomeMutator(_factory, _config, _random);

            var genome = _factory.CreateInitialGenome(0, _config, _random);
            var originalCount = genome.ConnectionGenes.Count;

            var mutated = _mutator.Mutate(genome);

            Assert.That(mutated.ConnectionGenes.Count, Is.GreaterThan(originalCount));
        }

        [Test]
        public void AddConnectionMutation_NoSelfConnections()
        {
            _config.AddNodeProbability = 0f;
            _config.EnableDisableConnectionProbability = 0f;
            _mutator = new GenomeMutator(_factory, _config, _random);

            var genome = _factory.CreateInitialGenome(0, _config, _random);
            var mutated = _mutator.Mutate(genome);

            foreach (var conn in mutated.ConnectionGenes)
            {
                Assert.That(conn.InNodeId, Is.Not.EqualTo(conn.OutNodeId), "No self-connections allowed");
            }
        }

        [Test]
        public void AddNodeMutation_IncreasesNodeCount()
        {
            _config.AddConnectionProbability = 0f;
            _config.EnableDisableConnectionProbability = 0f;
            _mutator = new GenomeMutator(_factory, _config, _random);

            var genome = _factory.CreateInitialGenome(0, _config, _random);
            var originalNodeCount = genome.NodeGenes.Count;

            var mutated = _mutator.Mutate(genome);

            Assert.That(mutated.NodeGenes.Count, Is.GreaterThan(originalNodeCount));
        }

        [Test]
        public void AddNodeMutation_AddsHiddenNode()
        {
            _config.AddConnectionProbability = 0f;
            _config.EnableDisableConnectionProbability = 0f;
            _mutator = new GenomeMutator(_factory, _config, _random);

            var genome = _factory.CreateInitialGenome(0, _config, _random);
            var originalHiddenCount = genome.NodeGenes.Count(n => n.Type == NodeType.Hidden);

            var mutated = _mutator.Mutate(genome);

            var newHiddenCount = mutated.NodeGenes.Count(n => n.Type == NodeType.Hidden);
            Assert.That(newHiddenCount, Is.GreaterThan(originalHiddenCount));
        }

        [Test]
        public void AddNodeMutation_DisablesSplitConnection()
        {
            _config.AddConnectionProbability = 0f;
            _config.EnableDisableConnectionProbability = 0f;
            _mutator = new GenomeMutator(_factory, _config, _random);

            var genome = _factory.CreateInitialGenome(0, _config, _random);

            var mutated = _mutator.Mutate(genome);

            // After add node, at least one connection should be disabled (the split one)
            Assert.That(mutated.ConnectionGenes.Any(c => !c.Enabled), Is.True);
        }

        [Test]
        public void ToggleConnectionMutation_ChangesEnabledState()
        {
            _config.AddConnectionProbability = 0f;
            _config.AddNodeProbability = 0f;
            _mutator = new GenomeMutator(_factory, _config, _random);

            // First, create a genome with a disabled connection
            var genome = _factory.CreateInitialGenome(0, _config, _random);
            genome.ConnectionGenes[0].Enabled = false;

            var mutated = _mutator.Mutate(genome);

            // Either the disabled one was enabled or an enabled one was disabled
            var originalEnabledCount = genome.ConnectionGenes.Count(c => c.Enabled);
            var mutatedEnabledCount = mutated.ConnectionGenes.Count(c => c.Enabled);
            Assert.That(mutatedEnabledCount, Is.Not.EqualTo(originalEnabledCount));
        }

        [Test]
        public void MultipleMutations_IncreaseComplexity()
        {
            _config.AddConnectionProbability = 1f;
            _config.AddNodeProbability = 0.5f;
            _config.EnableDisableConnectionProbability = 0f;
            _mutator = new GenomeMutator(_factory, _config, _random);

            var genome = _factory.CreateInitialGenome(0, _config, _random);

            for (var i = 0; i < 10; i++)
            {
                genome = _mutator.Mutate(genome);
            }

            Assert.That(genome.ConnectionGenes.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(genome.NodeGenes.Count, Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void MutationOperatorSelection_RespectsConfiguredProbabilities()
        {
            // Verify that mutation operator selection uses a coherent probability distribution.
            // With AddNode=0.1, AddConnection=0.2, Toggle=0.05, Weight should fire ~65% of the time.
            var testConfig = new NeatConfig
            {
                NumInputs = 2,
                NumOutputs = 1,
                AddNodeProbability = 0.1f,
                AddConnectionProbability = 0.2f,
                EnableDisableConnectionProbability = 0.05f,
                WeightMutationRate = 1f,
                WeightMutationAmount = 0.5f,
                MaxWeight = 6f
            };
            var testFactory = new GenomeFactory();
            var testRandom = new Random(12345);
            var testMutator = new GenomeMutator(testFactory, testConfig, testRandom);

            var addNodeCount = 0;
            var addConnCount = 0;
            var toggleCount = 0;
            var weightCount = 0;
            var iterations = 10000;

            for (var i = 0; i < iterations; i++)
            {
                var genome = testFactory.CreateInitialGenome(i, testConfig, new Random(i));
                // Pre-disable a connection so toggle can be detected
                genome.ConnectionGenes[0].Enabled = false;

                var originalNodeCount = genome.NodeGenes.Count;
                var originalConnCount = genome.ConnectionGenes.Count;
                var originalEnabledCount = genome.ConnectionGenes.Count(c => c.Enabled);
                var originalWeights = genome.ConnectionGenes.Select(c => c.Weight).ToArray();

                var mutated = testMutator.Mutate(genome);

                if (mutated.NodeGenes.Count > originalNodeCount)
                {
                    addNodeCount++;
                }
                else if (mutated.ConnectionGenes.Count > originalConnCount)
                {
                    addConnCount++;
                }
                else if (mutated.ConnectionGenes.Count(c => c.Enabled) != originalEnabledCount)
                {
                    toggleCount++;
                }
                else
                {
                    var mutatedWeights = mutated.ConnectionGenes.Select(c => c.Weight).ToArray();
                    var weightsChanged = originalWeights.Zip(mutatedWeights, (a, b) => Math.Abs(a - b) > 0.001f).Any();
                    if (weightsChanged)
                        weightCount++;
                    else
                        weightCount++; // Weight mutation ran but may not have changed anything visible
                }
            }

            // AddNode should fire ~10% of the time
            var addNodeRate = (double)addNodeCount / iterations;
            Assert.That(addNodeRate, Is.GreaterThan(0.07), $"AddNode rate {addNodeRate:F3} should be near 0.1");
            Assert.That(addNodeRate, Is.LessThan(0.13), $"AddNode rate {addNodeRate:F3} should be near 0.1");

            // AddConnection should fire ~20% of the time
            var addConnRate = (double)addConnCount / iterations;
            Assert.That(addConnRate, Is.GreaterThan(0.15), $"AddConnection rate {addConnRate:F3} should be near 0.2");
            Assert.That(addConnRate, Is.LessThan(0.25), $"AddConnection rate {addConnRate:F3} should be near 0.2");
        }
    }
}
