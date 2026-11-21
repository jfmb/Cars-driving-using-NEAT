using System.Collections.Generic;
using System.Linq;
using NeatCore.Configuration;

namespace NeatCore.Genetics
{
    /// <summary>
    /// A NEAT genome containing node genes and connection genes.
    /// Fitness is assigned by the external application after evaluation.
    /// </summary>
    public class Genome
    {
        public int Id { get; }
        public List<NodeGene> NodeGenes { get; private set; }
        public List<ConnectionGene> ConnectionGenes { get; private set; }
        public double Fitness { get; set; }
        public double AdjustedFitness { get; set; }
        public int SpeciesId { get; set; }
        public int Generation { get; set; }

        public Genome(int id)
        {
            Id = id;
            NodeGenes = new List<NodeGene>();
            ConnectionGenes = new List<ConnectionGene>();
        }

        public bool HasConnection(int fromId, int toId)
        {
            return ConnectionGenes.Any(c => c.InNodeId == fromId && c.OutNodeId == toId);
        }

        public override string ToString()
        {
            return $"Genome {Id}: {NodeGenes.Count} nodes, {ConnectionGenes.Count} connections, fitness={Fitness:F2}";
        }
    }

    /// <summary>
    /// Factory for creating initial genomes and managing innovation numbers.
    /// Innovation numbers ensure homologous alignment during crossover.
    /// </summary>
    public class GenomeFactory
    {
        private int _nextInnovation;
        private int _nextNodeId;
        private readonly Dictionary<(int, int), int> _innovationMap;

        public GenomeFactory()
        {
            _nextInnovation = 1;
            _nextNodeId = 1;
            _innovationMap = new Dictionary<(int, int), int>();
        }

        public int GetInnovationNumber(int fromId, int toId)
        {
            var key = (fromId, toId);
            if (_innovationMap.TryGetValue(key, out var innovation))
                return innovation;

            innovation = _nextInnovation++;
            _innovationMap[key] = innovation;
            return innovation;
        }

        public int GetNextNodeId()
        {
            return _nextNodeId++;
        }

        /// <summary>
        /// Create an initial genome connecting all inputs and bias to all outputs.
        /// </summary>
        public Genome CreateInitialGenome(int genomeId, NeatConfig config, System.Random random)
        {
            var genome = new Genome(genomeId);

            // Add bias node (ID = 0)
            var biasId = 0;
            genome.NodeGenes.Add(new NodeGene(biasId, NodeType.Bias));

            // Add input nodes (ID = 1..NumInputs)
            for (var i = 0; i < config.NumInputs; i++)
            {
                var inputId = i + 1;
                genome.NodeGenes.Add(new NodeGene(inputId, NodeType.Input));
            }

            // Add output nodes (ID = NumInputs+1..NumInputs+NumOutputs)
            for (var i = 0; i < config.NumOutputs; i++)
            {
                var outputId = config.NumInputs + 1 + i;
                genome.NodeGenes.Add(new NodeGene(outputId, NodeType.Output));
            }

            // Connect bias to each output
            for (var i = 0; i < config.NumOutputs; i++)
            {
                var outputId = config.NumInputs + 1 + i;
                var weight = (float)(random.NextDouble() * 2 - 1);
                var innovation = GetInnovationNumber(biasId, outputId);
                genome.ConnectionGenes.Add(new ConnectionGene(innovation, biasId, outputId, weight));
            }

            // Connect each input to each output
            for (var i = 0; i < config.NumInputs; i++)
            {
                var inputId = i + 1;
                for (var j = 0; j < config.NumOutputs; j++)
                {
                    var outputId = config.NumInputs + 1 + j;
                    var weight = (float)(random.NextDouble() * 2 - 1);
                    var innovation = GetInnovationNumber(inputId, outputId);
                    genome.ConnectionGenes.Add(new ConnectionGene(innovation, inputId, outputId, weight));
                }
            }

            return genome;
        }
    }
}
