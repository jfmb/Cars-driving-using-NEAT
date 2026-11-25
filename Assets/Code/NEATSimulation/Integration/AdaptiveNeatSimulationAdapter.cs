using System;
using System.Collections.Generic;
using NeatCore.Network;
using UnityEngine;

namespace NeatSimulation.Integration
{
    /// <summary>
    /// Owns an independent two-output NEAT population for steering and normalized target speed.
    /// </summary>
    public sealed class AdaptiveNeatSimulationAdapter : NeatSimulationAdapterBase
    {
        protected override int RequiredInputCount => 5;
        protected override int RequiredOutputCount => 2;
        protected override string OutputCountAssertionMessage =>
            "numOutputs must be exactly 2 for adaptive vehicle control";
        protected override string OutputCountExceptionMessage =>
            "The adaptive vehicle simulation requires exactly two NEAT outputs.";

        private void Reset()
        {
            SetNetworkShape(5, 2);
        }

        protected override void Awake()
        {
            base.Awake();
        }

        protected override INeatVehicleController CreateVehicleController(NeuralNetwork network, int inputCount)
        {
            return new NetworkAdaptiveVehicleController(network, inputCount);
        }

        private sealed class NetworkAdaptiveVehicleController : INeatVehicleController
        {
            private readonly NeuralNetwork _network;
            private readonly float[] _inputs;

            public NetworkAdaptiveVehicleController(NeuralNetwork network, int inputCount)
            {
                _network = network;
                _inputs = new float[inputCount];
            }

            public VehicleControlOutput EvaluateControl(IReadOnlyList<float> sensorReadings)
            {
                if (sensorReadings == null)
                {
                    throw new ArgumentNullException(nameof(sensorReadings));
                }

                if (sensorReadings.Count != _inputs.Length)
                {
                    throw new ArgumentException(
                        $"Expected {_inputs.Length} sensor readings, received {sensorReadings.Count}.",
                        nameof(sensorReadings));
                }

                for (var i = 0; i < _inputs.Length; i++)
                {
                    _inputs[i] = sensorReadings[i];
                }

                var outputs = _network.Evaluate(_inputs);
                return new VehicleControlOutput(
                    NormalizeOutput(outputs[0]),
                    NormalizeOutput(outputs[1]));
            }

            private static float NormalizeOutput(float output)
            {
                if (float.IsNaN(output) || float.IsInfinity(output))
                {
                    return 0f;
                }

                return Mathf.Clamp(output, -1f, 1f);
            }
        }
    }
}
