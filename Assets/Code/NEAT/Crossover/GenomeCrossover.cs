using System;
using System.Collections.Generic;
using System.Linq;
using NeatCore.Configuration;
using NeatCore.Genetics;

namespace NeatCore.Crossover
{
    /// <summary>
    /// Performs NEAT crossover between two parent genomes.
    /// Handles matching, disjoint, and excess genes correctly.
    /// </summary>
    public class GenomeCrossover
    {
        private readonly NeatConfig _config;
        private readonly Random _random;

        public GenomeCrossover(NeatConfig config, Random random)
        {
            _config = config;
            _random = random;
        }

        /// <summary>
        /// Perform crossover between two parent genomes.
        /// Parents should ideally be from the same species (similar topology).
        /// Returns two child genomes.
        /// When parents have equal fitness, either may be treated as the more-fit parent.
        /// </summary>
        public (Genome Child1, Genome Child2) Crossover(Genome parent1, Genome parent2)
        {
            // Determine the more-fit parent. On a tie, prefer parent1 deterministically.
            var better = parent1.Fitness >= parent2.Fitness ? parent1 : parent2;
            var worse = parent1.Fitness >= parent2.Fitness ? parent2 : parent1;

            var child1 = CreateChild(better, worse, 1);
            var child2 = CreateChild(better, worse, 2);

            return (child1, child2);
        }

        private Genome CreateChild(Genome better, Genome worse, int childIndex)
        {
            var childId = better.Id * 10 + childIndex;
            var child = new Genome(childId);
            child.Generation = Math.Max(better.Generation, worse.Generation) + 1;

            // Copy all nodes from both parents
            var nodeIds = new HashSet<int>();
            foreach (var node in better.NodeGenes)
                nodeIds.Add(node.Id);
            foreach (var node in worse.NodeGenes)
                nodeIds.Add(node.Id);

            // Add nodes from better parent first, then any extra from worse
            var betterNodeMap = new Dictionary<int, NodeGene>();
            foreach (var node in better.NodeGenes)
                betterNodeMap[node.Id] = node;

            var worseNodeMap = new Dictionary<int, NodeGene>();
            foreach (var node in worse.NodeGenes)
                worseNodeMap[node.Id] = node;

            foreach (var nodeId in nodeIds)
            {
                if (betterNodeMap.TryGetValue(nodeId, out var betterNode))
                    child.NodeGenes.Add(new NodeGene(betterNode.Id, betterNode.Type));
                else if (worseNodeMap.TryGetValue(nodeId, out var worseNode))
                    child.NodeGenes.Add(new NodeGene(worseNode.Id, worseNode.Type));
            }

            // Align connections by innovation number
            var betterConnMap = new Dictionary<int, ConnectionGene>();
            foreach (var conn in better.ConnectionGenes)
                betterConnMap[conn.Innovation] = conn;

            var worseConnMap = new Dictionary<int, ConnectionGene>();
            foreach (var conn in worse.ConnectionGenes)
                worseConnMap[conn.Innovation] = conn;

            var allInnovations = new HashSet<int>();
            foreach (var innov in betterConnMap.Keys)
                allInnovations.Add(innov);
            foreach (var innov in worseConnMap.Keys)
                allInnovations.Add(innov);

            foreach (var innovation in allInnovations)
            {
                var hasInBetter = betterConnMap.TryGetValue(innovation, out var betterConn);
                var hasInWorse = worseConnMap.TryGetValue(innovation, out var worseConn);

                if (hasInBetter && hasInWorse)
                {
                    // Matching gene — randomly inherit from either parent
                    if (_random.NextDouble() < 0.5f)
                        AddConnectionIfValid(child, betterConn, betterNodeMap.ContainsKey(betterConn.InNodeId) && betterNodeMap.ContainsKey(betterConn.OutNodeId));
                    else
                        AddConnectionIfValid(child, worseConn, worseNodeMap.ContainsKey(worseConn.InNodeId) && worseNodeMap.ContainsKey(worseConn.OutNodeId));
                }
                else if (hasInBetter)
                {
                    // Excess/disjoint gene from the more-fit parent — always inherit it.
                    // This preserves the fitter parent's structural innovations.
                    AddConnectionIfValid(child, betterConn, true);
                }
                // Genes present only in the less-fit parent (excess/disjoint from worse)
                // are dropped — standard NEAT behavior to prevent regression.
            }

            return child;
        }

        private void AddConnectionIfValid(Genome child, ConnectionGene conn, bool nodesExist)
        {
            if (!nodesExist)
                return;

            // Don't add duplicate connections
            if (child.HasConnection(conn.InNodeId, conn.OutNodeId))
                return;

            child.ConnectionGenes.Add(new ConnectionGene(conn.Innovation, conn.InNodeId, conn.OutNodeId, conn.Weight, conn.Enabled));
        }
    }
}
