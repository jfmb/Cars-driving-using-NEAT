using NeatCore.Network;
using UnityEngine;

namespace NeatSimulation.Integration
{
    /// <summary>
    /// Owns the legacy one-output NEAT population and adapts each genome's steering output.
    /// </summary>
    public sealed class NeatSimulationAdapter : NeatSimulationAdapterBase
    {
        protected override int RequiredOutputCount => 1;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override INeatVehicleController CreateVehicleController(NeuralNetwork network, int inputCount)
        {
            return new NetworkVehicleController(network, inputCount);
        }

        private sealed class NetworkVehicleController : INeatVehicleController
        {
            private readonly NeuralNetwork _network;
            private readonly float[] _inputs;

            public NetworkVehicleController(NeuralNetwork network, int inputCount)
            {
                _network = network;
                _inputs = new float[inputCount];
            }

            public VehicleControlOutput EvaluateControl(System.Collections.Generic.IReadOnlyList<float> sensorReadings)
            {
                if (sensorReadings == null)
                {
                    throw new System.ArgumentNullException(nameof(sensorReadings));
                }

                if (sensorReadings.Count != _inputs.Length)
                {
                    throw new System.ArgumentException(
                        $"Expected {_inputs.Length} sensor readings, received {sensorReadings.Count}.",
                        nameof(sensorReadings));
                }

                for (var i = 0; i < _inputs.Length; i++)
                {
                    _inputs[i] = sensorReadings[i];
                }

                var outputs = _network.Evaluate(_inputs);
                var steering = outputs[0];
                if (float.IsNaN(steering) || float.IsInfinity(steering))
                {
                    return new VehicleControlOutput(0f, 0f);
                }

                return new VehicleControlOutput(Mathf.Clamp(steering, -1f, 1f), 0f);
            }
        }
    }
}
