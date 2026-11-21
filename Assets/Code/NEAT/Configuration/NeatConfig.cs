using System;

namespace NeatCore.Configuration
{
    /// <summary>
    /// Central configuration for the NEAT algorithm.
    /// All evolutionary parameters are controlled through this object.
    /// </summary>
    public class NeatConfig
    {
        // Population
        public int PopulationSize { get; set; } = 150;
        public int NumInputs { get; set; } = 4;
        public int NumOutputs { get; set; } = 2;

        // Mutation probabilities
        public float MutationProbability { get; set; } = 0.8f;
        public float CrossoverProbability { get; set; } = 0.75f;
        public float AddConnectionProbability { get; set; } = 0.03f;
        public float AddNodeProbability { get; set; } = 0.03f;
        public float EnableDisableConnectionProbability { get; set; } = 0.01f;

        // Mutation parameters
        public float WeightMutationRate { get; set; } = 0.5f;
        public float WeightMutationAmount { get; set; } = 0.5f;
        public float WeightReplacementProbability { get; set; } = 0.03f;
        public float MaxWeight { get; set; } = 6f;

        // Elitism
        public int ElitismCount { get; set; } = 1;

        // Speciation
        public float CompatibilityThreshold { get; set; } = 3.0f;
        public float ExcessCoefficient { get; set; } = 1.0f;
        public float DisjointCoefficient { get; set; } = 1.0f;
        public float WeightCoefficient { get; set; } = 0.2f;

        // Species
        public int SpeciesAgeLimit { get; set; } = 15;
        public float SpeciesDecay { get; set; } = 0.02f;

        // Activation function
        public ActivationFunction ActivationFunction { get; set; } = ActivationFunction.Tanh;

        // Random seed (0 for non-deterministic)
        public long? RandomSeed { get; set; } = null;

        public NeatConfig Validate()
        {
            if (PopulationSize < 2)
                throw new ArgumentException("PopulationSize must be at least 2.", nameof(PopulationSize));
            if (NumInputs < 1)
                throw new ArgumentException("NumInputs must be at least 1.", nameof(NumInputs));
            if (NumOutputs < 1)
                throw new ArgumentException("NumOutputs must be at least 1.", nameof(NumOutputs));
            if (MutationProbability < 0f || MutationProbability > 1f)
                throw new ArgumentException("MutationProbability must be between 0 and 1.", nameof(MutationProbability));
            if (CrossoverProbability < 0f || CrossoverProbability > 1f)
                throw new ArgumentException("CrossoverProbability must be between 0 and 1.", nameof(CrossoverProbability));
            if (ElitismCount < 0 || ElitismCount >= PopulationSize)
                throw new ArgumentException("ElitismCount must be between 0 and PopulationSize-1.", nameof(ElitismCount));
            if (CompatibilityThreshold <= 0f)
                throw new ArgumentException("CompatibilityThreshold must be positive.", nameof(CompatibilityThreshold));

            return this;
        }
    }

    public enum ActivationFunction
    {
        Tanh,
        Sigmoid,
        ReLU,
        BinaryStep
    }
}
