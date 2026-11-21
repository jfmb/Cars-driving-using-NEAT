using System;
using System.Collections.Generic;
using System.Linq;
using NeatCore.Configuration;
using NeatCore.Genetics;

namespace NeatCore.Mutation
{
    /// <summary>
    /// Applies NEAT mutation operators to a genome.
    /// Each mutation respects innovation numbers and prevents invalid topologies.
    /// </summary>
    public class GenomeMutator
    {
        private readonly GenomeFactory _genomeFactory;
        private readonly NeatConfig _config;
        private readonly Random _random;

        public GenomeMutator(GenomeFactory genomeFactory, NeatConfig config, Random random)
        {
            _genomeFactory = genomeFactory;
            _config = config;
            _random = random;
        }

        /// <summary>
        /// Apply mutation to a genome based on configured probabilities.
        /// Returns a new mutated genome (does not modify the original).
        /// Probabilities form a coherent distribution: add-node, add-connection,
        /// toggle-connection are checked in order against a single random value
        /// so each operator fires at its configured rate.
        /// </summary>
        public Genome Mutate(Genome parent)
        {
            var child = CloneGenome(parent);
            var roll = _random.NextDouble();

            if (roll < _config.AddNodeProbability)
            {
                AddNodeMutation(child);
            }
            else if (roll < _config.AddNodeProbability + _config.AddConnectionProbability)
            {
                AddConnectionMutation(child);
            }
            else if (roll < _config.AddNodeProbability + _config.AddConnectionProbability + _config.EnableDisableConnectionProbability)
            {
                ToggleConnectionMutation(child);
            }
            else
            {
                WeightMutation(child);
            }

            return child;
        }

        private void WeightMutation(Genome genome)
        {
            foreach (var conn in genome.ConnectionGenes)
            {
                if (_random.NextDouble() < _config.WeightMutationRate)
                {
                    // Mutate weight
                    conn.Weight += (float)(_random.NextDouble() * 2 - 1) * _config.WeightMutationAmount;

                    // Clamp weight
                    if (conn.Weight > _config.MaxWeight)
                        conn.Weight = _config.MaxWeight;
                    else if (conn.Weight < -_config.MaxWeight)
                        conn.Weight = -_config.MaxWeight;
                }

                // Occasionally replace weight entirely
                if (_random.NextDouble() < _config.WeightReplacementProbability)
                {
                    conn.Weight = (float)(_random.NextDouble() * 2 - 1) * _config.MaxWeight;
                }
            }
        }

        private void AddConnectionMutation(Genome genome)
        {
            // Get all node IDs
            var nodeIds = genome.NodeGenes.Select(n => n.Id).ToList();

            // Try to find a valid connection that doesn't already exist
            var attempts = 0;
            while (attempts < 20)
            {
                var fromId = nodeIds[_random.Next(nodeIds.Count)];
                var toId = nodeIds[_random.Next(nodeIds.Count)];

                // Prevent self-connections
                if (fromId == toId)
                {
                    attempts++;
                    continue;
                }

                // Prevent duplicate connections
                if (genome.HasConnection(fromId, toId))
                {
                    attempts++;
                    continue;
                }

                // Prevent connecting two output nodes
                var fromNode = genome.NodeGenes.First(n => n.Id == fromId);
                var toNode = genome.NodeGenes.First(n => n.Id == toId);
                if (fromNode.Type == NodeType.Output || toNode.Type == NodeType.Input)
                {
                    attempts++;
                    continue;
                }

                // Add the new connection
                var weight = (float)(_random.NextDouble() * 2 - 1);
                var innovation = _genomeFactory.GetInnovationNumber(fromId, toId);
                genome.ConnectionGenes.Add(new ConnectionGene(innovation, fromId, toId, weight));
                return;
            }

            // If we can't add a connection, fall back to weight mutation
            WeightMutation(genome);
        }

        private void AddNodeMutation(Genome genome)
        {
            // Find an enabled connection to split
            var enabledConns = genome.ConnectionGenes.Where(c => c.Enabled).ToList();
            if (enabledConns.Count == 0)
            {
                WeightMutation(genome);
                return;
            }

            var conn = enabledConns[_random.Next(enabledConns.Count)];
            conn.Enabled = false;

            // Create new node
            var newNodeId = _genomeFactory.GetNextNodeId();
            genome.NodeGenes.Add(new NodeGene(newNodeId, NodeType.Hidden));

            // Connection from old input to new node (weight = 1)
            var innovation1 = _genomeFactory.GetInnovationNumber(conn.InNodeId, newNodeId);
            genome.ConnectionGenes.Add(new ConnectionGene(innovation1, conn.InNodeId, newNodeId, 1f));

            // Connection from new node to old output (keep original weight)
            var innovation2 = _genomeFactory.GetInnovationNumber(newNodeId, conn.OutNodeId);
            genome.ConnectionGenes.Add(new ConnectionGene(innovation2, newNodeId, conn.OutNodeId, conn.Weight));
        }

        private void ToggleConnectionMutation(Genome genome)
        {
            if (genome.ConnectionGenes.Count == 0)
                return;

            // Try to enable a disabled connection
            var disabledConns = genome.ConnectionGenes.Where(c => !c.Enabled).ToList();
            if (disabledConns.Count > 0 && _random.NextDouble() < 0.5f)
            {
                var conn = disabledConns[_random.Next(disabledConns.Count)];
                conn.Enabled = true;
                return;
            }

            // Otherwise, disable a random enabled connection
            var enabledConns = genome.ConnectionGenes.Where(c => c.Enabled).ToList();
            if (enabledConns.Count > 0)
            {
                var conn = enabledConns[_random.Next(enabledConns.Count)];
                conn.Enabled = false;
            }
        }

        private Genome CloneGenome(Genome source)
        {
            var clone = new Genome(source.Id);
            clone.Generation = source.Generation;
            clone.SpeciesId = source.SpeciesId;

            foreach (var node in source.NodeGenes)
            {
                clone.NodeGenes.Add(new NodeGene(node.Id, node.Type));
            }

            foreach (var conn in source.ConnectionGenes)
            {
                clone.ConnectionGenes.Add(new ConnectionGene(conn.Innovation, conn.InNodeId, conn.OutNodeId, conn.Weight, conn.Enabled));
            }

            return clone;
        }
    }
}
