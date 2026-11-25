using System;
using System.Collections.Generic;
using NeatCore.Configuration;
using NeatCore.Evolution;
using NeatCore.Network;
using UnityEngine;
using UnityEngine.Assertions;

namespace NeatSimulation.Integration
{
    /// <summary>
    /// Owns one adapter component's independent NEAT session and generation fitness reporting.
    /// </summary>
    public abstract class NeatSimulationAdapterBase : MonoBehaviour, INeatSimulationAdapter
    {
        [Header("Evolution")]
        [SerializeField, Min(1)] private int maximumGenerations = 100;
        [SerializeField, Min(1)] private int numInputs = 5;
        [SerializeField] private int numOutputs = 1;
        [SerializeField] private bool useRandomSeed;
        [SerializeField] private int randomSeed;

        [Header("Mutation")]
        [SerializeField, Range(0f, 1f)] private float mutationProbability = 0.8f;
        [SerializeField, Range(0f, 1f)] private float crossoverProbability = 0.75f;
        [SerializeField, Range(0f, 1f)] private float addConnectionProbability = 0.03f;
        [SerializeField, Range(0f, 1f)] private float addNodeProbability = 0.03f;
        [SerializeField, Range(0f, 1f)] private float enableDisableConnectionProbability = 0.01f;
        [SerializeField, Range(0f, 1f)] private float weightMutationRate = 0.5f;
        [SerializeField, Min(0f)] private float weightMutationAmount = 0.5f;
        [SerializeField, Range(0f, 1f)] private float weightReplacementProbability = 0.03f;
        [SerializeField, Min(0f)] private float maxWeight = 6f;

        [Header("Speciation")]
        [SerializeField, Min(0)] private int elitismCount = 1;
        [SerializeField, Min(0.001f)] private float compatibilityThreshold = 3f;
        [SerializeField, Min(0f)] private float excessCoefficient = 1f;
        [SerializeField, Min(0f)] private float disjointCoefficient = 1f;
        [SerializeField, Min(0f)] private float weightCoefficient = 0.2f;
        [SerializeField, Min(0)] private int speciesAgeLimit = 15;
        [SerializeField, Min(0f)] private float speciesDecay = 0.02f;
        [SerializeField] private ActivationFunction activationFunction = ActivationFunction.Tanh;

        private NeatAlgorithm _algorithm;
        private bool[] _fitnessReported;
        private int _reportedFitnessCount;
        private int _populationSize;
        private bool _generationComplete;
        private bool _finished;

        protected void SetNetworkShape(int inputCount, int outputCount)
        {
            numInputs = inputCount;
            numOutputs = outputCount;
        }

        protected virtual int RequiredInputCount => 0;
        protected virtual int RequiredOutputCount => 1;
        protected virtual string OutputCountAssertionMessage =>
            "numOutputs must be exactly 1 for vehicle steering";
        protected virtual string OutputCountExceptionMessage =>
            "The vehicle simulation requires exactly one NEAT output for steering.";

        protected virtual void Awake()
        {
            Assert.IsTrue(maximumGenerations > 0, "maximumGenerations must be > 0");
            Assert.IsTrue(numInputs > 0, "numInputs must be > 0");
            if (RequiredInputCount > 0)
            {
                Assert.IsTrue(numInputs == RequiredInputCount,
                    $"numInputs must be exactly {RequiredInputCount}");
            }

            Assert.IsTrue(numOutputs == RequiredOutputCount, OutputCountAssertionMessage);
        }

        /// <summary>
        /// Initializes the first generation or supplies the next one after all fitness results
        /// from the previous generation have been received. An empty list signals the generation limit.
        /// </summary>
        public IReadOnlyList<INeatVehicleController> CreatePopulation(int populationSize)
        {
            if (_finished)
            {
                return Array.Empty<INeatVehicleController>();
            }

            if (_algorithm == null)
            {
                _populationSize = populationSize;
                _algorithm = new NeatAlgorithm(CreateConfig(populationSize));
                _algorithm.Initialize();
            }
            else
            {
                if (populationSize != _populationSize)
                {
                    throw new InvalidOperationException(
                        "Population size cannot change while the NEAT simulation is running.");
                }

                if (!_generationComplete)
                {
                    throw new InvalidOperationException(
                        "The next population cannot be created before every individual is evaluated.");
                }

                _generationComplete = false;
            }

            var population = _algorithm.Population;
            _fitnessReported = new bool[population.Count];
            _reportedFitnessCount = 0;
            var controllers = new List<INeatVehicleController>(population.Count);
            for (var i = 0; i < population.Count; i++)
            {
                var network = _algorithm.BuildNetwork(population[i]);
                controllers.Add(CreateVehicleController(network, numInputs));
            }

            return controllers;
        }

        /// <summary>
        /// Records Unity-computed fitness exactly once for the matching population slot.
        /// The population evolves only after every slot has reported.
        /// </summary>
        public void ReportFitness(int individualId, float fitness)
        {
            if (_algorithm == null || _generationComplete || individualId < 0 ||
                individualId >= _fitnessReported.Length)
            {
                Debug.LogWarning(
                    $"[{GetType().Name}] Ignored fitness for invalid or inactive individual {individualId}.");
                return;
            }

            if (_fitnessReported[individualId])
            {
                Debug.LogWarning(
                    $"[{GetType().Name}] Ignored duplicate fitness for individual {individualId}.");
                return;
            }

            if (float.IsNaN(fitness) || float.IsInfinity(fitness))
            {
                Debug.LogWarning(
                    $"[{GetType().Name}] Individual {individualId} produced non-finite fitness; using 0.");
                fitness = 0f;
            }

            _algorithm.Population[individualId].Fitness = fitness;
            _fitnessReported[individualId] = true;
            _reportedFitnessCount++;
            if (_reportedFitnessCount != _fitnessReported.Length)
            {
                return;
            }

            _generationComplete = true;
            if (_algorithm.CurrentGeneration + 1 >= maximumGenerations)
            {
                _finished = true;
                return;
            }

            _algorithm.NextGeneration();
            Debug.Log("Generation number " + _algorithm.CurrentGeneration);
        }

        protected abstract INeatVehicleController CreateVehicleController(
            NeuralNetwork network,
            int inputCount);

        private NeatConfig CreateConfig(int populationSize)
        {
            if (maximumGenerations < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumGenerations),
                    "The generation limit must be at least 1.");
            }

            if (numOutputs != RequiredOutputCount)
            {
                throw new InvalidOperationException(OutputCountExceptionMessage);
            }

            if (RequiredInputCount > 0 && numInputs != RequiredInputCount)
            {
                throw new InvalidOperationException(
                    $"The vehicle simulation requires exactly {RequiredInputCount} NEAT inputs.");
            }

            var config = new NeatConfig
            {
                PopulationSize = populationSize,
                NumInputs = numInputs,
                NumOutputs = numOutputs,
                MutationProbability = mutationProbability,
                CrossoverProbability = crossoverProbability,
                AddConnectionProbability = addConnectionProbability,
                AddNodeProbability = addNodeProbability,
                EnableDisableConnectionProbability = enableDisableConnectionProbability,
                WeightMutationRate = weightMutationRate,
                WeightMutationAmount = weightMutationAmount,
                WeightReplacementProbability = weightReplacementProbability,
                MaxWeight = maxWeight,
                ElitismCount = elitismCount,
                CompatibilityThreshold = compatibilityThreshold,
                ExcessCoefficient = excessCoefficient,
                DisjointCoefficient = disjointCoefficient,
                WeightCoefficient = weightCoefficient,
                SpeciesAgeLimit = speciesAgeLimit,
                SpeciesDecay = speciesDecay,
                ActivationFunction = activationFunction,
                RandomSeed = useRandomSeed ? randomSeed : (long?)null
            };

            return config.Validate();
        }
    }
}
