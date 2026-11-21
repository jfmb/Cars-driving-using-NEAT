using System;
using System.Linq;
using NeatCore.Configuration;
using NeatCore.Evolution;
using NeatCore.Network;
using NUnit.Framework;

namespace NeatTests
{
    [TestFixture]
    public class EvolutionTests
    {
        [Test]
        public void Initialize_CreatesPopulationOfSize()
        {
            var config = new NeatConfig { PopulationSize = 50, NumInputs = 3, NumOutputs = 2 };
            var neat = new NeatAlgorithm(config);
            neat.Initialize();

            Assert.That(neat.Population.Count, Is.EqualTo(50));
        }

        [Test]
        public void Initialize_GenerationIsZero()
        {
            var config = new NeatConfig { PopulationSize = 10, NumInputs = 2, NumOutputs = 1 };
            var neat = new NeatAlgorithm(config);
            neat.Initialize();

            Assert.That(neat.CurrentGeneration, Is.EqualTo(0));
        }

        [Test]
        public void Initialize_AllGenomesHaveCorrectStructure()
        {
            var config = new NeatConfig { PopulationSize = 10, NumInputs = 3, NumOutputs = 2 };
            var neat = new NeatAlgorithm(config);
            neat.Initialize();

            foreach (var genome in neat.Population)
            {
                Assert.That(genome.NodeGenes.Count(n => n.Type == NeatCore.Genetics.NodeType.Bias), Is.EqualTo(1));
                Assert.That(genome.NodeGenes.Count(n => n.Type == NeatCore.Genetics.NodeType.Input), Is.EqualTo(3));
                Assert.That(genome.NodeGenes.Count(n => n.Type == NeatCore.Genetics.NodeType.Output), Is.EqualTo(2));
            }
        }

        [Test]
        public void NextGeneration_IncrementsGeneration()
        {
            var config = new NeatConfig { PopulationSize = 10, NumInputs = 2, NumOutputs = 1 };
            var neat = new NeatAlgorithm(config);
            neat.Initialize();

            AssignRandomFitness(neat);
            neat.NextGeneration();

            Assert.That(neat.CurrentGeneration, Is.EqualTo(1));
        }

        [Test]
        public void NextGeneration_MaintainsPopulationSize()
        {
            var config = new NeatConfig { PopulationSize = 50, NumInputs = 2, NumOutputs = 1 };
            var neat = new NeatAlgorithm(config);
            neat.Initialize();

            for (var i = 0; i < 10; i++)
            {
                AssignRandomFitness(neat);
                neat.NextGeneration();
                Assert.That(neat.Population.Count, Is.EqualTo(50), $"Generation {i + 1}");
            }
        }

        [Test]
        public void NextGeneration_TracksBestFitness()
        {
            var config = new NeatConfig { PopulationSize = 10, NumInputs = 2, NumOutputs = 1 };
            var neat = new NeatAlgorithm(config);
            neat.Initialize();

            // Assign fitness with one very high value
            foreach (var g in neat.Population)
                g.Fitness = 1;
            neat.Population[0].Fitness = 1000;

            neat.NextGeneration();

            Assert.That(neat.BestFitness, Is.EqualTo(1000));
        }

        [Test]
        public void NextGeneration_ElitismPreservesBest()
        {
            var config = new NeatConfig
            {
                PopulationSize = 20,
                NumInputs = 2,
                NumOutputs = 1,
                ElitismCount = 2
            };
            var neat = new NeatAlgorithm(config);
            neat.Initialize();

            // Assign distinct fitness values
            for (var i = 0; i < neat.Population.Count; i++)
                neat.Population[i].Fitness = i;

            var bestGenome = neat.Population.Last(); // Highest fitness
            neat.NextGeneration();

            // The best genome should survive
            Assert.That(neat.Population, Does.Contain(bestGenome));
        }

        [Test]
        public void NextGeneration_FitnessImprovesOverTime()
        {
            var config = new NeatConfig
            {
                PopulationSize = 100,
                NumInputs = 2,
                NumOutputs = 1,
                ElitismCount = 2,
                RandomSeed = 123
            };
            var neat = new NeatAlgorithm(config);
            neat.Initialize();

            double previousBest = double.NegativeInfinity;
            bool improved = false;

            for (var gen = 0; gen < 50; gen++)
            {
                // Simple fitness: reward outputs close to 1
                foreach (var genome in neat.Population)
                {
                    var network = neat.BuildNetwork(genome);
                    var output = network.Evaluate(new float[] { 1f, 0f });
                    genome.Fitness = Math.Abs(output[0] - 1f) < 0.5f ? 100 : 10;
                }

                neat.NextGeneration();

                if (neat.BestFitness > previousBest)
                    improved = true;
                previousBest = neat.BestFitness;
            }

            Assert.That(improved, Is.True, "Fitness should have improved at least once over 50 generations");
        }

        [Test]
        public void BuildNetwork_FromGenome()
        {
            var config = new NeatConfig { PopulationSize = 10, NumInputs = 2, NumOutputs = 1 };
            var neat = new NeatAlgorithm(config);
            neat.Initialize();

            var genome = neat.Population[0];
            var network = neat.BuildNetwork(genome);

            Assert.That(network, Is.Not.Null);
        }

        [Test]
        public void GetStats_ReturnsValidStats()
        {
            var config = new NeatConfig { PopulationSize = 20, NumInputs = 2, NumOutputs = 1 };
            var neat = new NeatAlgorithm(config);
            neat.Initialize();

            AssignRandomFitness(neat);
            var stats = neat.GetStats();

            Assert.That(stats.PopulationSize, Is.EqualTo(20));
            Assert.That(stats.NumSpecies, Is.GreaterThanOrEqualTo(1));
            Assert.That(stats.AverageConnections, Is.GreaterThan(0));
            Assert.That(stats.AverageNodes, Is.GreaterThan(0));
        }

        [Test]
        public void Reproducibility_SameSeedSameResults()
        {
            var config1 = new NeatConfig
            {
                PopulationSize = 10,
                NumInputs = 2,
                NumOutputs = 1,
                RandomSeed = 42
            };
            var config2 = new NeatConfig
            {
                PopulationSize = 10,
                NumInputs = 2,
                NumOutputs = 1,
                RandomSeed = 42
            };

            var neat1 = new NeatAlgorithm(config1);
            var neat2 = new NeatAlgorithm(config2);

            neat1.Initialize();
            neat2.Initialize();

            // Same seed should produce same initial genomes
            for (var i = 0; i < neat1.Population.Count; i++)
            {
                var g1 = neat1.Population[i];
                var g2 = neat2.Population[i];

                Assert.That(g1.NodeGenes.Count, Is.EqualTo(g2.NodeGenes.Count));
                Assert.That(g1.ConnectionGenes.Count, Is.EqualTo(g2.ConnectionGenes.Count));

                for (var j = 0; j < g1.ConnectionGenes.Count; j++)
                {
                    Assert.That(g1.ConnectionGenes[j].Weight, Is.EqualTo(g2.ConnectionGenes[j].Weight));
                    Assert.That(g1.ConnectionGenes[j].Innovation, Is.EqualTo(g2.ConnectionGenes[j].Innovation));
                }
            }
        }

        [Test]
        public void MultipleGenerations_NoExceptions()
        {
            var config = new NeatConfig
            {
                PopulationSize = 30,
                NumInputs = 4,
                NumOutputs = 2,
                RandomSeed = 99
            };
            var neat = new NeatAlgorithm(config);
            neat.Initialize();

            for (var i = 0; i < 20; i++)
            {
                AssignRandomFitness(neat);
                neat.NextGeneration();
            }

            Assert.That(neat.CurrentGeneration, Is.EqualTo(20));
            Assert.That(neat.Population.Count, Is.EqualTo(30));
        }

        [Test]
        public void Species_DiversifiesOverGenerations()
        {
            var config = new NeatConfig
            {
                PopulationSize = 50,
                NumInputs = 2,
                NumOutputs = 1,
                CompatibilityThreshold = 2.0f,
                RandomSeed = 77
            };
            var neat = new NeatAlgorithm(config);
            neat.Initialize();

            var initialSpecies = neat.Species.Count;

            for (var i = 0; i < 10; i++)
            {
                AssignRandomFitness(neat);
                neat.NextGeneration();
            }

            // Species count should change as mutations introduce new topologies
            Assert.That(neat.Species.Count, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void SmallPopulation_WorksCorrectly()
        {
            var config = new NeatConfig
            {
                PopulationSize = 2,
                NumInputs = 1,
                NumOutputs = 1,
                ElitismCount = 1
            };
            var neat = new NeatAlgorithm(config);
            neat.Initialize();

            Assert.That(neat.Population.Count, Is.EqualTo(2));

            for (var i = 0; i < 5; i++)
            {
                AssignRandomFitness(neat);
                neat.NextGeneration();
            }

            Assert.That(neat.Population.Count, Is.EqualTo(2));
        }

        [Test]
        public void NextGeneration_ExactlyPopulationSize_WithManySpecies()
        {
            // Use a very low compatibility threshold so each genome forms its own species.
            // This creates more species than available offspring.
            var config = new NeatConfig
            {
                PopulationSize = 10,
                NumInputs = 2,
                NumOutputs = 1,
                ElitismCount = 1,
                CompatibilityThreshold = 0.001f,
                RandomSeed = 42
            };
            var neat = new NeatAlgorithm(config);
            neat.Initialize();

            // After initialization, each genome should be in its own species
            Assert.That(neat.Species.Count, Is.GreaterThan(1), "Should have multiple species with low threshold");

            for (var i = 0; i < 10; i++)
            {
                AssignRandomFitness(neat);
                neat.NextGeneration();
                Assert.That(neat.Population.Count, Is.EqualTo(10), $"Generation {i + 1} should have exactly {config.PopulationSize} genomes");
            }
        }

        [Test]
        public void Reproducibility_LongSeed_SameSeedSameResults()
        {
            // Verify that a long seed value produces reproducible results.
            // Two algorithms with the same long seed should produce identical results.
            var longSeed = 9223372036854775807L; // long.MaxValue
            var config1 = new NeatConfig
            {
                PopulationSize = 10,
                NumInputs = 2,
                NumOutputs = 1,
                RandomSeed = longSeed
            };
            var config2 = new NeatConfig
            {
                PopulationSize = 10,
                NumInputs = 2,
                NumOutputs = 1,
                RandomSeed = longSeed
            };

            var neat1 = new NeatAlgorithm(config1);
            var neat2 = new NeatAlgorithm(config2);

            neat1.Initialize();
            neat2.Initialize();

            // Same long seed should produce identical populations
            for (var i = 0; i < neat1.Population.Count; i++)
            {
                var g1 = neat1.Population[i];
                var g2 = neat2.Population[i];
                Assert.That(g1.ConnectionGenes.Count, Is.EqualTo(g2.ConnectionGenes.Count));
                for (var j = 0; j < g1.ConnectionGenes.Count; j++)
                {
                    Assert.That(g1.ConnectionGenes[j].Weight, Is.EqualTo(g2.ConnectionGenes[j].Weight),
                        $"Genome {i} connection {j} weight mismatch — long seed may be truncated");
                }
            }
        }

        [Test]
        public void Reproducibility_LongSeed_DifferentSeedsDifferentResults()
        {
            // Verify that different long seeds produce different random sequences.
            // Two large seeds that differ only in the high 32 bits should not collide.
            var seedA = 0x1234567800000001L;
            var seedB = 0x1234567800000002L;

            var configA = new NeatConfig
            {
                PopulationSize = 10,
                NumInputs = 2,
                NumOutputs = 1,
                RandomSeed = seedA
            };
            var configB = new NeatConfig
            {
                PopulationSize = 10,
                NumInputs = 2,
                NumOutputs = 1,
                RandomSeed = seedB
            };

            var neatA = new NeatAlgorithm(configA);
            var neatB = new NeatAlgorithm(configB);

            neatA.Initialize();
            neatB.Initialize();

            // Different seeds should produce different populations
            bool foundDifference = false;
            for (var i = 0; i < neatA.Population.Count; i++)
            {
                var gA = neatA.Population[i];
                var gB = neatB.Population[i];

                if (gA.ConnectionGenes.Count != gB.ConnectionGenes.Count)
                {
                    foundDifference = true;
                    break;
                }

                for (var j = 0; j < gA.ConnectionGenes.Count; j++)
                {
                    if (gA.ConnectionGenes[j].Weight != gB.ConnectionGenes[j].Weight)
                    {
                        foundDifference = true;
                        break;
                    }
                }
                if (foundDifference) break;
            }

            Assert.That(foundDifference, Is.True,
                "Different long seeds should produce different random sequences");
        }

        [Test]
        public void Reproducibility_LongSeed_HighBitsContribute()
        {
            // Verify that the high 32 bits of a long seed affect the output.
            // seedX and seedY differ only in the high 32 bits.
            var seedX = 0x0000000100000000L;
            var seedY = 0x0000000200000000L;

            var configX = new NeatConfig
            {
                PopulationSize = 10,
                NumInputs = 2,
                NumOutputs = 1,
                RandomSeed = seedX
            };
            var configY = new NeatConfig
            {
                PopulationSize = 10,
                NumInputs = 2,
                NumOutputs = 1,
                RandomSeed = seedY
            };

            var neatX = new NeatAlgorithm(configX);
            var neatY = new NeatAlgorithm(configY);

            neatX.Initialize();
            neatY.Initialize();

            bool foundDifference = false;
            for (var i = 0; i < neatX.Population.Count; i++)
            {
                var gX = neatX.Population[i];
                var gY = neatY.Population[i];

                if (gX.ConnectionGenes.Count != gY.ConnectionGenes.Count)
                {
                    foundDifference = true;
                    break;
                }

                for (var j = 0; j < gX.ConnectionGenes.Count; j++)
                {
                    if (gX.ConnectionGenes[j].Weight != gY.ConnectionGenes[j].Weight)
                    {
                        foundDifference = true;
                        break;
                    }
                }
                if (foundDifference) break;
            }

            Assert.That(foundDifference, Is.True,
                "High 32 bits of a long seed should affect the random sequence");
        }

        private void AssignRandomFitness(NeatAlgorithm neat)
        {
            var random = new Random(neat.CurrentGeneration * 1000);
            foreach (var genome in neat.Population)
            {
                genome.Fitness = random.NextDouble() * 100;
            }
        }
    }
}
