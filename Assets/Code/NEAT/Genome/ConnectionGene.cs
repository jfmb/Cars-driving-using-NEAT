namespace NeatCore.Genetics
{
    /// <summary>
    /// Represents a connection between two nodes in a NEAT genome.
    /// Each connection has a unique innovation number for historical marking.
    /// </summary>
    public class ConnectionGene
    {
        public int Innovation { get; }
        public int InNodeId { get; }
        public int OutNodeId { get; }
        public float Weight { get; set; }
        public bool Enabled { get; set; }

        public ConnectionGene(int innovation, int inNodeId, int outNodeId, float weight, bool enabled = true)
        {
            Innovation = innovation;
            InNodeId = inNodeId;
            OutNodeId = outNodeId;
            Weight = weight;
            Enabled = enabled;
        }

        public override bool Equals(object obj)
        {
            return obj is ConnectionGene other && Innovation == other.Innovation;
        }

        public override int GetHashCode()
        {
            return Innovation;
        }
    }
}
