using System;
using System.Collections.Generic;
using NeatCore.Configuration;
using NeatCore.Genetics;
using NeatCore.Speciation;

namespace NeatTests
{
    /// <summary>
    /// Standalone test runner that can be invoked via Unity's -executeMethod flag.
    /// Used when the Unity Test Framework cannot run in batch mode.
    /// </summary>
    public class ConsoleTestRunner
    {
        private static int _passed;
        private static int _failed;
        private static List<string> _failures;

        public static void RunAllTests()
        {
            _passed = 0;
            _failed = 0;
            _failures = new List<string>();

            Console.WriteLine("=== NEAT Console Test Runner ===");

            RunSpeciationTests();
            RunGenomeTests();
            RunNeuralNetworkTests();
            RunMutationTests();
            RunCrossoverTests();

            Console.WriteLine($"\n=== Results: {_passed} passed, {_failed} failed ===");
            if (_failed > 0)
            {
                Console.WriteLine("\nFailures:");
                foreach (var f in _failures)
                    Console.WriteLine($"  FAIL: {f}");
            }

            // Unity's -executeMethod requires this to be void
        }

        private static void RunTest(string name, Action test)
        {
            try
            {
                test();
                _passed++;
                Console.WriteLine($"  PASS: {name}");
            }
            catch (Exception ex)
            {
                _failed++;
                var msg = $"{name}: {ex.Message}";
                _failures.Add(msg);
                Console.WriteLine($"  FAIL: {msg}");
            }
        }

        private static void AssertEqual(double expected, double actual, string message)
        {
            if (Math.Abs(expected - actual) > 0.001)
                throw new AssertException($"{message}: expected {expected}, got {actual}");
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition)
                throw new AssertException(message);
        }

        private static void AssertGreater(double value, double threshold, string message)
        {
            if (value <= threshold)
                throw new AssertException($"{message}: {value} is not greater than {threshold}");
        }

        #region Speciation Tests

        private static void RunSpeciationTests()
        {
            Console.WriteLine("\n--- Speciation Tests ---");

            var config = new NeatConfig
            {
                NumInputs = 2,
                NumOutputs = 1,
                CompatibilityThreshold = 3.0f,
                ExcessCoefficient = 1.0f,
                DisjointCoefficient = 1.0f,
                WeightCoefficient = 0.2f
            };
            var factory = new GenomeFactory();
            var random = new Random(42);
            var calculator = new CompatibilityCalculator(config);

            RunTest("Distance_IdenticalGenomes_IsZero", () =>
            {
                var g1 = factory.CreateInitialGenome(1, config, random);
                var g2 = factory.CreateInitialGenome(2, config, random);
                for (var i = 0; i < g1.ConnectionGenes.Count; i++)
                    g2.ConnectionGenes[i].Weight = g1.ConnectionGenes[i].Weight;
                var distance = calculator.CalculateDistance(g1, g2);
                AssertEqual(0, distance, "Identical genomes should have zero distance");
            });

            RunTest("Distance_DifferentWeights_IsPositive", () =>
            {
                var g1 = factory.CreateInitialGenome(1, config, random);
                var g2 = factory.CreateInitialGenome(2, config, random);
                foreach (var c in g1.ConnectionGenes) c.Weight = 1f;
                foreach (var c in g2.ConnectionGenes) c.Weight = -1f;
                var distance = calculator.CalculateDistance(g1, g2);
                AssertGreater(distance, 0, "Different weights should produce positive distance");
            });

            RunTest("Distance_Symmetric", () =>
            {
                var g1 = factory.CreateInitialGenome(1, config, random);
                var g2 = factory.CreateInitialGenome(2, config, random);
                var d1 = calculator.CalculateDistance(g1, g2);
                var d2 = calculator.CalculateDistance(g2, g1);
                AssertEqual(d1, d2, "Distance should be symmetric");
            });

            // --- Excess / Disjoint classification tests ---

            RunTest("Classification_SameTopology_OnlyMatchingGenes", () =>
            {
                var g1 = factory.CreateInitialGenome(1, config, random);
                var g2 = factory.CreateInitialGenome(2, config, random);
                for (var i = 0; i < g1.ConnectionGenes.Count; i++)
                    g2.ConnectionGenes[i].Weight = g1.ConnectionGenes[i].Weight;
                var distance = calculator.CalculateDistance(g1, g2);
                AssertEqual(0, distance, "Identical genomes should have zero distance");
            });

            RunTest("Classification_ExcessGenes_WhenOneGenomeHasHigherInnovations", () =>
            {
                var g1 = factory.CreateInitialGenome(1, config, random);
                var g2 = factory.CreateInitialGenome(2, config, random);
                var newNodeId = factory.GetNextNodeId();
                var innov4 = factory.GetInnovationNumber(1, newNodeId);
                g2.NodeGenes.Add(new NodeGene(newNodeId, NodeType.Hidden));
                g2.ConnectionGenes.Add(new ConnectionGene(innov4, 1, newNodeId, 0.5f));
                var newNodeId2 = factory.GetNextNodeId();
                var innov5 = factory.GetInnovationNumber(newNodeId, newNodeId2);
                g2.NodeGenes.Add(new NodeGene(newNodeId2, NodeType.Hidden));
                g2.ConnectionGenes.Add(new ConnectionGene(innov5, newNodeId, newNodeId2, 0.3f));
                var distance = calculator.CalculateDistance(g1, g2);
                AssertGreater(distance, 0, "Excess genes should increase distance");
                var N = Math.Max(g1.ConnectionGenes.Count, g2.ConnectionGenes.Count);
                var expectedExcessContribution = config.ExcessCoefficient * 2.0 / N;
                AssertGreater(distance, expectedExcessContribution - 0.001, "Distance should include excess gene contribution");
            });

            RunTest("Classification_DisjointGenes_WithSameMaxInnovation", () =>
            {
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

                var distance = calculator.CalculateDistance(g1, g2);
                var expected = (1.0 * 0 + 1.0 * 2 + 0.2 * 0) / 3;
                AssertEqual(expected, distance, $"Expected {expected}, got {distance}");
            });

            RunTest("Classification_ExcessAndDisjointMixed", () =>
            {
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

                var distance = calculator.CalculateDistance(g1, g2);
                var expected = (1.0 * 0 + 1.0 * 4 + 0.2 * 0) / 5;
                AssertEqual(expected, distance, $"Expected {expected}, got {distance}");
            });

            RunTest("Classification_ExcessGenes_WithDifferentMaxInnovations", () =>
            {
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

                var distance = calculator.CalculateDistance(g1, g2);
                var expected = (1.0 * 3 + 1.0 * 0 + 0.2 * 0) / 6;
                AssertEqual(expected, distance, $"Expected {expected}, got {distance}");
            });

            RunTest("Classification_BothExcessAndDisjoint", () =>
            {
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

                var distance = calculator.CalculateDistance(g1, g2);
                var expected = (1.0 * 2 + 1.0 * 4 + 0.2 * 0) / 5;
                AssertEqual(expected, distance, $"Expected {expected}, got {distance}");
            });

            RunTest("Classification_MatchingWeightDifference_AppliedCorrectly", () =>
            {
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
                g2.ConnectionGenes.Add(new ConnectionGene(1, 0, 3, -1.0f));
                g2.ConnectionGenes.Add(new ConnectionGene(2, 1, 3, 1.0f));

                var distance = calculator.CalculateDistance(g1, g2);
                var expected = (0.2 * 1.5) / 2;
                AssertEqual(expected, distance, $"Expected {expected}, got {distance}");
            });

            RunTest("Classification_NoGenes_ReturnsZero", () =>
            {
                var g1 = new Genome(1);
                var g2 = new Genome(2);
                var distance = calculator.CalculateDistance(g1, g2);
                AssertEqual(0, distance, "Empty genomes should have zero distance");
            });

            RunTest("Classification_Coefficients_AffectDistance", () =>
            {
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
                g2.ConnectionGenes.Add(new ConnectionGene(3, 1, 3, 0.8f));

                var distance1 = calculator.CalculateDistance(g1, g2);

                var highDisjointConfig = new NeatConfig
                {
                    NumInputs = 2,
                    NumOutputs = 1,
                    CompatibilityThreshold = 3.0f,
                    ExcessCoefficient = 1.0f,
                    DisjointCoefficient = 5.0f,
                    WeightCoefficient = 0.2f
                };
                var calculator2 = new CompatibilityCalculator(highDisjointConfig);
                var distance2 = calculator2.CalculateDistance(g1, g2);

                AssertGreater(distance2, distance1, "Higher disjoint coefficient should increase distance");
            });

            RunTest("Species_AssignsGenomeToNewSpecies", () =>
            {
                var speciationManager = new SpeciationManager(calculator, config);
                var genome = factory.CreateInitialGenome(1, config, random);
                var population = new List<Genome> { genome };
                speciationManager.AssignGenomesToSpecies(population);
                AssertTrue(speciationManager.Species.Count == 1, "Should create 1 species");
                AssertTrue(genome.SpeciesId == 1, "Genome should be assigned to species 1");
            });

            RunTest("Species_SimilarGenomesInSameSpecies", () =>
            {
                var speciationManager = new SpeciationManager(calculator, config);
                var g1 = factory.CreateInitialGenome(1, config, random);
                var g2 = factory.CreateInitialGenome(2, config, random);
                for (var i = 0; i < g1.ConnectionGenes.Count; i++)
                    g2.ConnectionGenes[i].Weight = g1.ConnectionGenes[i].Weight;
                var population = new List<Genome> { g1, g2 };
                speciationManager.AssignGenomesToSpecies(population);
                AssertTrue(speciationManager.Species.Count == 1, "Similar genomes should be in same species");
            });

            RunTest("FitnessSharing_ReducesFitnessInLargerSpecies", () =>
            {
                var speciationManager = new SpeciationManager(calculator, config);
                var g1 = factory.CreateInitialGenome(1, config, random);
                g1.Fitness = 100;
                var g2 = factory.CreateInitialGenome(2, config, random);
                g2.Fitness = 100;
                for (var i = 0; i < g1.ConnectionGenes.Count; i++)
                    g2.ConnectionGenes[i].Weight = g1.ConnectionGenes[i].Weight;
                var population = new List<Genome> { g1, g2 };
                speciationManager.AssignGenomesToSpecies(population);
                speciationManager.ApplyFitnessSharing();
                AssertEqual(50, g1.AdjustedFitness, "Adjusted fitness should be 50");
                AssertEqual(50, g2.AdjustedFitness, "Adjusted fitness should be 50");
            });
        }

        #endregion

        #region Genome Tests

        private static void RunGenomeTests()
        {
            Console.WriteLine("\n--- Genome Tests ---");

            var config = new NeatConfig { NumInputs = 2, NumOutputs = 1 };
            var factory = new GenomeFactory();
            var random = new Random(42);

            RunTest("Genome_CreatesInitialGenome", () =>
            {
                var genome = factory.CreateInitialGenome(1, config, random);
                AssertTrue(genome.Id == 1, "Genome ID should be 1");
                AssertTrue(genome.NodeGenes.Count == 4, "Should have 4 nodes (bias + 2 inputs + 1 output)");
                AssertTrue(genome.ConnectionGenes.Count == 3, "Should have 3 connections (bias->out, in1->out, in2->out)");
            });

            RunTest("Genome_InnovationNumbersUnique", () =>
            {
                var genome = factory.CreateInitialGenome(1, config, random);
                var innovations = new HashSet<int>();
                foreach (var conn in genome.ConnectionGenes)
                    innovations.Add(conn.Innovation);
                AssertTrue(innovations.Count == genome.ConnectionGenes.Count, "All innovation numbers should be unique");
            });

            RunTest("Genome_HasConnection", () =>
            {
                var genome = factory.CreateInitialGenome(1, config, random);
                var firstConn = genome.ConnectionGenes[0];
                AssertTrue(genome.HasConnection(firstConn.InNodeId, firstConn.OutNodeId), "Should find existing connection");
                AssertTrue(!genome.HasConnection(999, 888), "Should not find non-existent connection");
            });
        }

        #endregion

        #region Neural Network Tests

        private static void RunNeuralNetworkTests()
        {
            Console.WriteLine("\n--- Neural Network Tests ---");

            var config = new NeatConfig { NumInputs = 2, NumOutputs = 1 };
            var factory = new GenomeFactory();
            var random = new Random(42);

            RunTest("Network_EvaluatesBasicGenome", () =>
            {
                var genome = factory.CreateInitialGenome(1, config, random);
                var network = NeatCore.Network.NeuralNetwork.BuildFromGenome(genome, ActivationFunction.Tanh);
                var inputs = new float[] { 1f, 0.5f };
                var outputs = network.Evaluate(inputs);
                AssertTrue(outputs.Length == 1, "Should have 1 output");
            });

            RunTest("Network_InputCountMismatch_Throws", () =>
            {
                var genome = factory.CreateInitialGenome(1, config, random);
                var network = NeatCore.Network.NeuralNetwork.BuildFromGenome(genome, ActivationFunction.Tanh);
                try
                {
                    network.Evaluate(new float[] { 1f });
                    throw new AssertException("Should throw for wrong input count");
                }
                catch (ArgumentException)
                {
                    // Expected
                }
            });

            RunTest("Network_DeterministicWithSameWeights", () =>
            {
                var genome = factory.CreateInitialGenome(1, config, random);
                // Set all weights to known values
                foreach (var c in genome.ConnectionGenes) c.Weight = 1f;
                var network = NeatCore.Network.NeuralNetwork.BuildFromGenome(genome, ActivationFunction.Tanh);
                var inputs = new float[] { 1f, 1f };
                var output1 = network.Evaluate(inputs);
                var output2 = network.Evaluate(inputs);
                AssertEqual(output1[0], output2[0], "Same inputs should produce same outputs");
            });
        }

        #endregion

        #region Mutation Tests

        private static void RunMutationTests()
        {
            Console.WriteLine("\n--- Mutation Tests ---");

            var config = new NeatConfig { NumInputs = 2, NumOutputs = 1 };
            var factory = new GenomeFactory();
            var random = new Random(42);

            RunTest("Mutation_AddNode_IncreasesNodeCount", () =>
            {
                // Force add node mutation by setting high probability
                var mutConfig = new NeatConfig
                {
                    NumInputs = 2,
                    NumOutputs = 1,
                    AddNodeProbability = 1.0f
                };
                var mutFactory = new GenomeFactory();
                var mutator = new NeatCore.Mutation.GenomeMutator(mutFactory, mutConfig, new Random(42));
                var genome = mutFactory.CreateInitialGenome(1, mutConfig, new Random(42));
                var countBefore = genome.NodeGenes.Count;
                var mutated = mutator.Mutate(genome);
                AssertTrue(mutated.NodeGenes.Count > countBefore, "Add node should increase node count");
            });

            RunTest("Mutation_ProducesValidGenome", () =>
            {
                var mutator = new NeatCore.Mutation.GenomeMutator(factory, config, new Random(42));
                var genome = factory.CreateInitialGenome(1, config, random);
                var mutated = mutator.Mutate(genome);
                AssertTrue(mutated != null, "Mutated genome should not be null");
                AssertTrue(mutated.ConnectionGenes.Count > 0, "Mutated genome should have connections");
            });
        }

        #endregion

        #region Crossover Tests

        private static void RunCrossoverTests()
        {
            Console.WriteLine("\n--- Crossover Tests ---");

            var config = new NeatConfig { NumInputs = 2, NumOutputs = 1 };
            var factory = new GenomeFactory();
            var random = new Random(42);

            RunTest("Crossover_SameTopology_ProducesValidOffspring", () =>
            {
                var g1 = factory.CreateInitialGenome(1, config, random);
                g1.Fitness = 100;
                var g2 = factory.CreateInitialGenome(2, config, random);
                g2.Fitness = 80;
                var crossover = new NeatCore.Crossover.GenomeCrossover(config, new Random(42));
                var (child1, child2) = crossover.Crossover(g1, g2);
                AssertTrue(child1 != null, "Child1 should not be null");
                AssertTrue(child2 != null, "Child2 should not be null");
                AssertTrue(child1.ConnectionGenes.Count > 0, "Child1 should have connections");
            });
        }

        #endregion

        private class AssertException : Exception
        {
            public AssertException(string message) : base(message) { }
        }
    }
}
