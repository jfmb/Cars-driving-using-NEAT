using System;
using System.Collections.Generic;
using System.Linq;
using NeatCore.Configuration;
using NeatCore.Genetics;
using NeatCore.Speciation;

namespace NeatCore.Selection
{
    /// <summary>
    /// Handles parent selection, elitism, and generating the next population.
    /// Uses fitness-proportionate selection within each species.
    /// </summary>
    public class PopulationSelector
    {
        private readonly NeatConfig _config;
        private readonly Random _random;

        public PopulationSelector(NeatConfig config, Random random)
        {
            _config = config;
            _random = random;
        }

        /// <summary>
        /// Select the best genomes across all species for elitism.
        /// </summary>
        public List<Genome> SelectElites(List<Species> species)
        {
            if (_config.ElitismCount <= 0)
                return new List<Genome>();

            var allGenomes = new List<Genome>();
            foreach (var s in species)
            {
                allGenomes.AddRange(s.Members);
            }

            var sorted = allGenomes.OrderByDescending(g => g.Fitness).ToList();
            return sorted.Take(_config.ElitismCount).ToList();
        }

        /// <summary>
        /// Select a parent from a species using fitness-proportionate selection.
        /// </summary>
        public Genome SelectParent(Species species)
        {
            if (species.Members.Count == 0)
                return species.Representative;
            if (species.Members.Count == 1)
                return species.Members[0];

            // Sum of adjusted fitness
            var totalFitness = 0.0;
            foreach (var member in species.Members)
            {
                totalFitness += Math.Max(member.AdjustedFitness, 0.001); // Avoid zero fitness
            }

            // Roulette wheel selection
            var target = _random.NextDouble() * totalFitness;
            var cumulative = 0.0;
            foreach (var member in species.Members)
            {
                cumulative += Math.Max(member.AdjustedFitness, 0.001);
                if (cumulative >= target)
                    return member;
            }

            return species.Members[_random.Next(species.Members.Count)];
        }

        /// <summary>
        /// Calculate the number of offspring each species should produce.
        /// Species with better adjusted fitness get more offspring.
        /// Guarantees the total allocated offspring equals <paramref name="totalOffspring"/>.
        /// When there are more species than offspring, some species receive 0 offspring.
        /// </summary>
        public Dictionary<int, int> AllocateOffspring(List<Species> species, int totalOffspring)
        {
            var allocation = new Dictionary<int, int>();
            if (species.Count == 0)
                return allocation;

            if (totalOffspring <= 0)
                return allocation;

            // Calculate total adjusted fitness
            var totalAdjustedFitness = 0.0;
            foreach (var s in species)
            {
                totalAdjustedFitness += Math.Max(s.Members.Count > 0
                    ? s.Members.Max(m => m.AdjustedFitness)
                    : 0.001, 0.001);
            }

            if (totalAdjustedFitness <= 0)
                totalAdjustedFitness = species.Count; // Equal distribution

            // Allocate proportionally using floor, then distribute remainder
            var speciesList = species.ToList();
            var remaining = totalOffspring;

            // First pass: floor allocation
            for (var i = 0; i < speciesList.Count; i++)
            {
                var s = speciesList[i];
                var bestFitness = s.Members.Count > 0
                    ? s.Members.Max(m => m.AdjustedFitness)
                    : 0.001;

                var proportion = Math.Max(bestFitness, 0.001) / totalAdjustedFitness;
                var count = (int)Math.Floor(proportion * totalOffspring);
                allocation[s.Id] = count;
                remaining -= count;
            }

            // Second pass: distribute remainder one at a time to species that need it most
            // Use fractional remainders to decide priority
            var fractionalParts = new List<(int speciesId, double fractional)>();
            foreach (var kvp in allocation)
            {
                var s = speciesList.First(sp => sp.Id == kvp.Key);
                var bestFitness = s.Members.Count > 0
                    ? s.Members.Max(m => m.AdjustedFitness)
                    : 0.001;
                var proportion = Math.Max(bestFitness, 0.001) / totalAdjustedFitness;
                var fractional = proportion * totalOffspring - kvp.Value;
                fractionalParts.Add((kvp.Key, fractional));
            }

            fractionalParts.Sort((a, b) => b.fractional.CompareTo(a.fractional));

            for (var i = 0; i < remaining && i < fractionalParts.Count; i++)
            {
                allocation[fractionalParts[i].speciesId]++;
            }

            return allocation;
        }
    }
}
