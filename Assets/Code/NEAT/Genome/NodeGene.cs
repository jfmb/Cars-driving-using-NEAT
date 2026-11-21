namespace NeatCore.Genetics
{
    public enum NodeType
    {
        Bias,
        Input,
        Output,
        Hidden
    }

    /// <summary>
    /// Represents a single node in a NEAT genome.
    /// </summary>
    public class NodeGene
    {
        public int Id { get; }
        public NodeType Type { get; }

        public NodeGene(int id, NodeType type)
        {
            Id = id;
            Type = type;
        }

        public override bool Equals(object obj)
        {
            return obj is NodeGene other && Id == other.Id && Type == other.Type;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Id * 397) ^ ((int)Type);
            }
        }
    }
}
