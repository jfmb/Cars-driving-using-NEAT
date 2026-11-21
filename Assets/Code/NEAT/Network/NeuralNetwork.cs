using System;
using System.Collections.Generic;
using System.Linq;
using NeatCore.Configuration;
using NeatCore.Genetics;

namespace NeatCore.Network
{
    /// <summary>
    /// Executable neural network built from a genome.
    /// Supports arbitrary topologies evolved by NEAT, including hidden nodes.
    /// Uses topological sorting for feed-forward evaluation.
    /// </summary>
    public class NeuralNetwork
    {
        private readonly Dictionary<int, NeuralNode> _nodes;
        private readonly List<int> _evaluationOrder;
        private readonly int[] _inputNodeIds;
        private readonly int[] _outputNodeIds;
        private readonly ActivationFunction _activationFunction;

        private NeuralNetwork(
            Dictionary<int, NeuralNode> nodes,
            List<int> evaluationOrder,
            int[] inputNodeIds,
            int[] outputNodeIds,
            ActivationFunction activationFunction)
        {
            _nodes = nodes;
            _evaluationOrder = evaluationOrder;
            _inputNodeIds = inputNodeIds;
            _outputNodeIds = outputNodeIds;
            _activationFunction = activationFunction;
        }

        /// <summary>
        /// Evaluate the network with the given inputs and return outputs.
        /// </summary>
        public float[] Evaluate(float[] inputs)
        {
            if (inputs == null)
                throw new ArgumentNullException(nameof(inputs));
            if (inputs.Length != _inputNodeIds.Length)
                throw new ArgumentException($"Expected {_inputNodeIds.Length} inputs, got {inputs.Length}.");

            // Set input node activations
            for (var i = 0; i < inputs.Length; i++)
                _nodes[_inputNodeIds[i]].Activation = inputs[i];

            // Evaluate in topological order
            foreach (var nodeId in _evaluationOrder)
            {
                var node = _nodes[nodeId];
                if (node.Type == NodeType.Input)
                    continue; // Inputs already set

                var sum = 0f;
                foreach (var (fromId, weight) in node.IncomingConnections)
                {
                    sum += _nodes[fromId].Activation * weight;
                }

                node.Activation = ApplyActivation(sum, node.Type);
            }

            // Read outputs
            var outputs = new float[_outputNodeIds.Length];
            for (var i = 0; i < outputs.Length; i++)
                outputs[i] = _nodes[_outputNodeIds[i]].Activation;

            return outputs;
        }

        private float ApplyActivation(float value, NodeType type)
        {
            // Bias nodes always output 1
            if (type == NodeType.Bias)
                return 1f;

            // Output nodes use linear activation (no activation function)
            if (type == NodeType.Output)
                return value;

            // Hidden nodes use the configured activation function
            return ApplyActivationFunction(value);
        }

        private float ApplyActivationFunction(float value)
        {
            return _activationFunction switch
            {
                ActivationFunction.Tanh => (float)Math.Tanh(value),
                ActivationFunction.Sigmoid => 1f / (1f + (float)Math.Exp(-value)),
                ActivationFunction.ReLU => value > 0f ? value : 0f,
                ActivationFunction.BinaryStep => value >= 0f ? 1f : 0f,
                _ => (float)Math.Tanh(value)
            };
        }

        /// <summary>
        /// Build an executable network from a genome.
        /// </summary>
        public static NeuralNetwork BuildFromGenome(Genome genome, ActivationFunction activationFunction)
        {
            var nodes = new Dictionary<int, NeuralNode>();

            // Create all nodes
            foreach (var nodeGene in genome.NodeGenes)
            {
                nodes[nodeGene.Id] = new NeuralNode(nodeGene.Id, nodeGene.Type);
            }

            // Wire enabled connections
            foreach (var conn in genome.ConnectionGenes)
            {
                if (!conn.Enabled)
                    continue;

                var targetNode = nodes[conn.OutNodeId];
                targetNode.IncomingConnections.Add((conn.InNodeId, conn.Weight));
            }

            // Topological sort for evaluation order
            var evaluationOrder = TopologicalSort(nodes);

            var inputNodeIds = genome.NodeGenes
                .Where(n => n.Type == NodeType.Input)
                .Select(n => n.Id)
                .ToArray();

            var outputNodeIds = genome.NodeGenes
                .Where(n => n.Type == NodeType.Output)
                .Select(n => n.Id)
                .ToArray();

            return new NeuralNetwork(nodes, evaluationOrder, inputNodeIds, outputNodeIds, activationFunction);
        }

        private static List<int> TopologicalSort(Dictionary<int, NeuralNode> nodes)
        {
            // Build adjacency list and in-degree count (only for enabled connections)
            var adjacency = new Dictionary<int, List<int>>();
            var inDegree = new Dictionary<int, int>();

            foreach (var kvp in nodes)
            {
                adjacency[kvp.Key] = new List<int>();
                inDegree[kvp.Key] = 0;
            }

            foreach (var kvp in nodes)
            {
                var nodeId = kvp.Key;
                var node = kvp.Value;
                foreach (var (fromId, _) in node.IncomingConnections)
                {
                    adjacency[fromId].Add(nodeId);
                    inDegree[nodeId]++;
                }
            }

            // Kahn's algorithm
            var queue = new List<int>();
            foreach (var kvp in inDegree)
            {
                if (kvp.Value == 0)
                    queue.Add(kvp.Key);
            }

            var result = new List<int>();
            while (queue.Count > 0)
            {
                var node = queue[0];
                queue.RemoveAt(0);
                result.Add(node);

                foreach (var neighbor in adjacency[node])
                {
                    inDegree[neighbor]--;
                    if (inDegree[neighbor] == 0)
                        queue.Add(neighbor);
                }
            }

            if (result.Count != nodes.Count)
                throw new InvalidOperationException("Cycle detected in network topology. Feed-forward networks cannot have cycles.");

            return result;
        }
    }

    /// <summary>
    /// A node in the executable neural network.
    /// </summary>
    internal class NeuralNode
    {
        public int Id { get; }
        public NodeType Type { get; }
        public float Activation { get; set; }
        public List<(int fromId, float weight)> IncomingConnections { get; }

        public NeuralNode(int id, NodeType type)
        {
            Id = id;
            Type = type;
            Activation = 0f;
            IncomingConnections = new List<(int, float)>();
        }
    }
}
