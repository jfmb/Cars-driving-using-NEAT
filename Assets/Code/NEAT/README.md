# NEAT Core

Pure C# implementation of the **NEAT (NeuroEvolution of Augmenting Topologies)** algorithm. Completely independent of Unity — reusable in any C# application.

## Overview

NEAT evolves both the weights **and** topology of neural networks. Unlike fixed-architecture genetic algorithms, NEAT can grow networks from minimal structures, protecting structural innovations through speciation.

## Architecture

| Component | Responsibility |
|---|---|
| `NeatConfig` | Central configuration for all evolutionary parameters |
| `Genome` | Genetic representation: node genes + connection genes |
| `GenomeFactory` | Creates initial genomes; manages innovation numbers |
| `NeuralNetwork` | Executable network built from a genome (feed-forward) |
| `GenomeMutator` | Mutation operators (weight, add connection, add node, toggle) |
| `GenomeCrossover` | Crossover with innovation-number alignment |
| `CompatibilityCalculator` | Genome distance for speciation |
| `SpeciationManager` | Species assignment and fitness sharing |
| `PopulationSelector` | Fitness-proportionate selection and elitism |
| `NeatAlgorithm` | Main entry point — evolution lifecycle |

## Quick Start

```csharp
using NeatCore.Configuration;
using NeatCore.Evolution;
using NeatCore.Genetics;
using NeatCore.Network;

// 1. Configure
var config = new NeatConfig
{
    PopulationSize = 150,
    NumInputs = 4,
    NumOutputs = 2,
    RandomSeed = 42  // For reproducibility
};

// 2. Initialize
var neat = new NeatAlgorithm(config);
neat.Initialize();

// 3. Evolution loop
for (int gen = 0; gen < 100; gen++)
{
    // Evaluate each genome
    foreach (var genome in neat.Population)
    {
        var network = neat.BuildNetwork(genome);
        float[] outputs = network.Evaluate(new float[] { 1f, 0f, 1f, 0f });
        
        // Assign fitness (your application logic)
        genome.Fitness = CalculateFitness(outputs);
    }

    // Advance to next generation
    neat.NextGeneration();
}

// 4. Use the best genome
var bestNetwork = neat.BuildNetwork(neat.BestGenome);
float[] result = bestNetwork.Evaluate(new float[] { 1f, 0f, 1f, 0f });
```

## Configuration Parameters

| Parameter | Default | Description |
|---|---|---|
| `PopulationSize` | 150 | Number of genomes per generation |
| `NumInputs` | 4 | Input nodes |
| `NumOutputs` | 2 | Output nodes |
| `MutationProbability` | 0.8 | Chance a child is mutated |
| `CrossoverProbability` | 0.75 | Chance of crossover vs. mutation |
| `AddConnectionProbability` | 0.03 | Chance to add a new connection |
| `AddNodeProbability` | 0.03 | Chance to add a hidden node |
| `ElitismCount` | 1 | Best genomes carried to next generation |
| `CompatibilityThreshold` | 3.0 | Max distance for same species |
| `ActivationFunction` | Tanh | Activation for hidden nodes |
| `RandomSeed` | null | Seed for reproducibility |

## Key Design Decisions

- **Feed-forward only**: No recurrent networks in this implementation.
- **Output nodes use linear activation**: Only hidden nodes apply the configured activation function.
- **Bias nodes always output 1**: Connected to all output nodes initially.
- **Innovation numbers**: Persistent across the entire evolution, ensuring correct gene alignment during crossover.
- **Deterministic**: Set `RandomSeed` for reproducible experiments.

## File Structure

```
Assets/Code/NEAT/
├── Configuration/
│   └── NeatConfig.cs          # Configuration + ActivationFunction enum
├── Genetics/
│   ├── NodeGene.cs            # Node representation + NodeType enum
│   ├── ConnectionGene.cs      # Connection with innovation number
│   └── Genome.cs              # Genome + GenomeFactory
├── Network/
│   └── NeuralNetwork.cs       # Executable network + topological sort
├── Mutation/
│   └── GenomeMutator.cs       # All mutation operators
├── Crossover/
│   └── GenomeCrossover.cs     # Crossover with alignment
├── Speciation/
│   └── SpeciationManager.cs   # CompatibilityCalculator, Species, SpeciationManager
├── Selection/
│   └── PopulationSelector.cs  # Selection + elitism + offspring allocation
├── Evolution/
│   └── NeatAlgorithm.cs       # Main API + PopulationStats
└── NEAT.asmdef                # Assembly definition (no Unity references)

Assets/Tests/NEAT.Tests/
├── GenomeTests.cs             # Genome creation, innovation numbers
├── NeuralNetworkTests.cs      # Network execution, activations
├── MutationTests.cs           # All mutation operators
├── CrossoverTests.cs          # Crossover behavior
├── SpeciationTests.cs         # Distance, species, fitness sharing
├── EvolutionTests.cs          # Full lifecycle, elitism, reproducibility
├── ConfigTests.cs             # Configuration validation
└── NEATTests.asmdef           # Test assembly definition
```

## Limitations

- No recurrent network support.
- Excess/disjoint gene classification simplified (all non-common genes treated as disjoint).
- No persistence/serialization.
- No parallel evaluation support.
