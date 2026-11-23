using System;
using System.Collections.Generic;
using System.Linq;

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
}
