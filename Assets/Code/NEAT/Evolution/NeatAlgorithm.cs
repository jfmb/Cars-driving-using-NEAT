using System;
using System.Collections.Generic;
using System.Linq;
using NeatCore.Configuration;
using NeatCore.Crossover;
using NeatCore.Genetics;
using NeatCore.Mutation;
using NeatCore.Network;
using NeatCore.Selection;
using NeatCore.Speciation;

namespace NeatCore.Evolution
{
    /// <summary>
    /// Main entry point for the NEAT algorithm.
    /// Manages the evolution lifecycle: initialization, evaluation, and generation advancement.
    /// </summary>
    public class NeatAlgorithm
    {
        private readonly NeatConfig _config;
        private readonly Random _random;
        private readonly GenomeFactory _genomeFactory;
        private readonly GenomeMutator _mutator;
        private readonly GenomeCrossover _crossover;
        private readonly CompatibilityCalculator _compatibilityCalculator;
        private readonly SpeciationManager _speciationManager;
        private readonly PopulationSelector _selector;

        private List<Genome> _population;
        private int _currentGeneration;
        private Genome _bestGenome;
        private double _bestFitness;

        /// <summary>
        /// Current generation number (0-based).
        /// </summary>
        public int CurrentGeneration => _currentGeneration;

        /// <summary>
        /// The best genome found so far across all generations.
        /// </summary>
        public Genome BestGenome => _bestGenome;

        /// <summary>
        /// The best fitness value found so far.
        /// </summary>
        public double BestFitness => _bestFitness;

        /// <summary>
        /// Current population of genomes.
        /// </summary>
        public List<Genome> Population => _population;

        /// <summary>
        /// Current species list.
        /// </summary>
        public List<Species> Species => _speciationManager.Species;

        public NeatAlgorithm(NeatConfig config)
            : this(config, config.RandomSeed.HasValue
                ? new Random(config.RandomSeed.Value.ToIntSeed())
                : new Random())
        {
        }

        public NeatAlgorithm(NeatConfig config, Random random)
        {
            _config = config.Validate();
            _random = random;
            _genomeFactory = new GenomeFactory();
            _mutator = new GenomeMutator(_genomeFactory, _config, _random);
            _crossover = new GenomeCrossover(_config, _random);
            _compatibilityCalculator = new CompatibilityCalculator(_config);
            _speciationManager = new SpeciationManager(_compatibilityCalculator, _config);
            _selector = new PopulationSelector(_config, _random);
            _population = new List<Genome>();
            _currentGeneration = 0;
            _bestFitness = double.NegativeInfinity;
        }

        /// <summary>
        /// Initialize the algorithm and create the first population.
        /// </summary>
        public void Initialize()
        {
            _population.Clear();
            _speciationManager.ResetForNewGeneration();

            for (var i = 0; i < _config.PopulationSize; i++)
            {
                var genome = _genomeFactory.CreateInitialGenome(i, _config, _random);
                genome.Generation = 0;
                _population.Add(genome);
            }

            // Assign initial species
            _speciationManager.AssignGenomesToSpecies(_population);
            _currentGeneration = 0;
            _bestFitness = double.NegativeInfinity;
            _bestGenome = null;
        }

        /// <summary>
        /// Advance to the next generation.
        /// Call this after assigning fitness values to all genomes in the current population.
        /// </summary>
        public void NextGeneration()
        {
            // Track the best genome
            foreach (var genome in _population)
            {
                if (genome.Fitness > _bestFitness)
                {
                    _bestFitness = genome.Fitness;
                    _bestGenome = genome;
                }
            }

            // Apply fitness sharing
            _speciationManager.ApplyFitnessSharing();

            // Select elites
            var elites = _selector.SelectElites(_speciationManager.Species);

            // Allocate offspring per species
            var offspringNeeded = _config.PopulationSize - elites.Count;
            var allocation = _selector.AllocateOffspring(_speciationManager.Species, offspringNeeded);

            // Generate new population
            var newPopulation = new List<Genome>(elites);

            foreach (var kvp in allocation)
            {
                var speciesId = kvp.Key;
                var count = kvp.Value;
                var species = _speciationManager.Species.FirstOrDefault(s => s.Id == speciesId);

                if (species == null || species.Members.Count == 0)
                {
                    // Species disappeared — fill with random mutations
                    for (var i = 0; i < count; i++)
                    {
                        var randomGenome = _population[_random.Next(_population.Count)];
                        var mutated = _mutator.Mutate(randomGenome);
                        mutated.Generation = _currentGeneration + 1;
                        newPopulation.Add(mutated);
                    }
                    continue;
                }

                for (var i = 0; i < count; i++)
                {
                    Genome child;

                    // Crossover or mutation
                    if (species.Members.Count > 1 && _random.NextDouble() < _config.CrossoverProbability)
                    {
                        // Crossover
                        var parent1 = _selector.SelectParent(species);
                        Genome parent2;
                        do
                        {
                            parent2 = _selector.SelectParent(species);
                        } while (parent2.Id == parent1.Id && species.Members.Count > 1);

                        var (child1, _) = _crossover.Crossover(parent1, parent2);
                        child = child1;
                    }
                    else
                    {
                        // Mutation only
                        var parent = _selector.SelectParent(species);
                        child = _mutator.Mutate(parent);
                    }

                    child.Generation = _currentGeneration + 1;
                    newPopulation.Add(child);
                }
            }

            // Ensure population size
            while (newPopulation.Count < _config.PopulationSize)
            {
                var randomGenome = _population[_random.Next(_population.Count)];
                var mutated = _mutator.Mutate(randomGenome);
                mutated.Generation = _currentGeneration + 1;
                newPopulation.Add(mutated);
            }

            _population = newPopulation;

            // Age species and assign new genomes to species
            _speciationManager.AgeSpecies();
            _speciationManager.ResetForNewGeneration();
            _speciationManager.AssignGenomesToSpecies(_population);

            // Update representatives to best member
            foreach (var species in _speciationManager.Species)
            {
                if (species.Members.Count > 0)
                {
                    var best = species.Members.OrderByDescending(g => g.Fitness).First();
                    species.UpdateRepresentative(best);
                }
            }

            _currentGeneration++;
        }

        /// <summary>
        /// Build an executable neural network from a genome.
        /// </summary>
        public NeuralNetwork BuildNetwork(Genome genome)
        {
            return NeuralNetwork.BuildFromGenome(genome, _config.ActivationFunction);
        }

        /// <summary>
        /// Get statistics about the current population.
        /// </summary>
        public PopulationStats GetStats()
        {
            if (_population.Count == 0)
                return new PopulationStats(0, 0, 0, 0, 0, 0, 0);

            var fitnesses = _population.Select(g => g.Fitness).ToList();
            var avgFitness = fitnesses.Average();
            var maxFitness = fitnesses.Max();
            var minFitness = fitnesses.Min();

            var avgConnections = _population.Average(g => g.ConnectionGenes.Count);
            var avgNodes = _population.Average(g => g.NodeGenes.Count);
            var numSpecies = _speciationManager.Species.Count;

            return new PopulationStats(
                _population.Count,
                numSpecies,
                (float)avgFitness,
                (float)maxFitness,
                (float)minFitness,
                (float)avgConnections,
                (float)avgNodes
            );
        }
    }

    /// <summary>
    /// Statistics about the current population.
    /// </summary>
    public class PopulationStats
    {
        public int PopulationSize { get; }
        public int NumSpecies { get; }
        public float AverageFitness { get; }
        public float MaxFitness { get; }
        public float MinFitness { get; }
        public float AverageConnections { get; }
        public float AverageNodes { get; }

        public PopulationStats(int populationSize, int numSpecies, float averageFitness,
            float maxFitness, float minFitness, float averageConnections, float averageNodes)
        {
            PopulationSize = populationSize;
            NumSpecies = numSpecies;
            AverageFitness = averageFitness;
            MaxFitness = maxFitness;
            MinFitness = minFitness;
            AverageConnections = averageConnections;
            AverageNodes = averageNodes;
        }
    }
}
