using System;
using System.Collections.Generic;
using System.Linq;
using NeatCore.Configuration;
using NeatCore.Genetics;

namespace NeatCore.Speciation
{
    /// <summary>
    /// Calculates compatibility distance between two genomes.
    /// Distance is based on excess, disjoint, and matching (weight difference) genes.
    /// </summary>
    public class CompatibilityCalculator
    {
        private readonly NeatConfig _config;

        public CompatibilityCalculator(NeatConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Calculate the compatibility distance between two genomes.
        /// Higher distance means more structurally different.
        /// </summary>
        public double CalculateDistance(Genome genome1, Genome genome2)
        {
            var (excess, disjoint, matching) = ClassifyGeneDifferences(genome1, genome2);

            var N = Math.Max(genome1.ConnectionGenes.Count, genome2.ConnectionGenes.Count);
            if (N == 0)
                N = 1; // Avoid division by zero

            var distance = (_config.ExcessCoefficient * excess +
                           _config.DisjointCoefficient * disjoint +
                           _config.WeightCoefficient * matching) / N;

            return distance;
        }

        private (int excess, int disjoint, double matching) ClassifyGeneDifferences(Genome genome1, Genome genome2)
        {
            var innovations1 = new HashSet<int>();
            foreach (var conn in genome1.ConnectionGenes)
                innovations1.Add(conn.Innovation);

            var innovations2 = new HashSet<int>();
            foreach (var conn in genome2.ConnectionGenes)
                innovations2.Add(conn.Innovation);

            // Find common innovations (matching genes)
            var common = new HashSet<int>(innovations1);
            common.IntersectWith(innovations2);

            // Find the maximum innovation number across both genomes.
            // In the original NEAT algorithm, genes with innovation numbers higher
            // than the max innovation of the other genome are classified as excess.
            // Genes below that threshold but not shared are disjoint.
            var maxInnovation1 = 0;
            foreach (var innov in innovations1)
                if (innov > maxInnovation1)
                    maxInnovation1 = innov;

            var maxInnovation2 = 0;
            foreach (var innov in innovations2)
                if (innov > maxInnovation2)
                    maxInnovation2 = innov;

            var minMaxInnovation = Math.Min(maxInnovation1, maxInnovation2);

            // Count excess genes: innovations greater than the other genome's max
            var excess1 = 0;
            foreach (var innov in innovations1)
                if (!common.Contains(innov) && innov > minMaxInnovation)
                    excess1++;

            var excess2 = 0;
            foreach (var innov in innovations2)
                if (!common.Contains(innov) && innov > minMaxInnovation)
                    excess2++;

            var excess = excess1 + excess2;

            // Count disjoint genes: non-common innovations at or below the threshold
            var disjoint1 = 0;
            foreach (var innov in innovations1)
                if (!common.Contains(innov) && innov <= minMaxInnovation)
                    disjoint1++;

            var disjoint2 = 0;
            foreach (var innov in innovations2)
                if (!common.Contains(innov) && innov <= minMaxInnovation)
                    disjoint2++;

            var disjoint = disjoint1 + disjoint2;

            // Calculate weight difference for matching genes
            var connMap2 = new Dictionary<int, ConnectionGene>();
            foreach (var conn in genome2.ConnectionGenes)
                connMap2[conn.Innovation] = conn;

            var matchingWeightDiff = 0.0;
            foreach (var innovation in common)
            {
                var conn1 = genome1.ConnectionGenes.First(c => c.Innovation == innovation);
                var conn2 = connMap2[innovation];
                matchingWeightDiff += Math.Abs(conn1.Weight - conn2.Weight);
            }

            var matching = common.Count > 0 ? matchingWeightDiff / common.Count : 0;

            return (excess, disjoint, matching);
        }
    }

    /// <summary>
    /// Represents a species in the NEAT algorithm.
    /// Each species has a representative genome and a collection of members.
    /// </summary>
    public class Species
    {
        public int Id { get; }
        public Genome Representative { get; private set; }
        public List<Genome> Members { get; }
        public int BestFitness { get; set; }
        public int Age { get; private set; }
        public double AdjustedFitness { get; set; }

        public Species(int id, Genome representative)
        {
            Id = id;
            Representative = representative;
            Members = new List<Genome> { representative };
            representative.SpeciesId = id;
            Age = 0;
            BestFitness = 0;
        }

        public void AddMember(Genome genome)
        {
            genome.SpeciesId = Id;
            Members.Add(genome);
        }

        public void UpdateRepresentative(Genome newRepresentative)
        {
            Representative = newRepresentative;
        }

        public void IncrementAge()
        {
            Age++;
        }

        public bool IsStale(int ageLimit)
        {
            return Age >= ageLimit;
        }
    }

    /// <summary>
    /// Manages speciation: assigning genomes to species and applying fitness sharing.
    /// </summary>
    public class SpeciationManager
    {
        private readonly CompatibilityCalculator _compatibilityCalculator;
        private readonly NeatConfig _config;
        private readonly List<Species> _species;
        private int _nextSpeciesId;

        public SpeciationManager(CompatibilityCalculator compatibilityCalculator, NeatConfig config)
        {
            _compatibilityCalculator = compatibilityCalculator;
            _config = config;
            _species = new List<Species>();
            _nextSpeciesId = 1;
        }

        public List<Species> Species => _species;

        /// <summary>
        /// Assign genomes to existing species or create new species.
        /// </summary>
        public void AssignGenomesToSpecies(List<Genome> population)
        {
            foreach (var genome in population)
            {
                var assigned = false;

                foreach (var species in _species)
                {
                    var distance = _compatibilityCalculator.CalculateDistance(genome, species.Representative);
                    if (distance < _config.CompatibilityThreshold)
                    {
                        species.AddMember(genome);
                        assigned = true;
                        break;
                    }
                }

                if (!assigned)
                {
                    // Create a new species
                    var newSpecies = new Species(_nextSpeciesId++, genome);
                    _species.Add(newSpecies);
                }
            }
        }

        /// <summary>
        /// Apply fitness sharing: adjust fitness based on species size.
        /// Larger species get lower adjusted fitness (more competition).
        /// </summary>
        public void ApplyFitnessSharing()
        {
            foreach (var species in _species)
            {
                var populationSize = species.Members.Count;
                foreach (var member in species.Members)
                {
                    member.AdjustedFitness = member.Fitness / populationSize;
                }

                // Track best fitness in species
                species.BestFitness = species.Members.Count > 0
                    ? species.Members.Max(m => (int)m.Fitness)
                    : 0;
            }
        }

        /// <summary>
        /// Age all species and remove stale ones.
        /// </summary>
        public void AgeSpecies()
        {
            foreach (var species in _species)
            {
                species.IncrementAge();
            }

            // Remove species that haven't improved in a while
            _species.RemoveAll(s => s.IsStale(_config.SpeciesAgeLimit) && s.Members.Count == 0);
        }

        /// <summary>
        /// Reset species for a new generation (keep species structure, clear members).
        /// </summary>
        public void ResetForNewGeneration()
        {
            foreach (var species in _species)
            {
                species.Members.Clear();
            }
        }
    }
}
