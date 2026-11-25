using System.Collections.Generic;

namespace NeatSimulation.Integration
{
    /// <summary>
    /// Adapts one NEAT individual to normalized vehicle controls.
    /// </summary>
    public interface INeatVehicleController
    {
        VehicleControlOutput EvaluateControl(IReadOnlyList<float> sensorReadings);
    }
}
