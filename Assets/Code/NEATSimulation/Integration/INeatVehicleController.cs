using System.Collections.Generic;

namespace NeatSimulation.Integration
{
    /// <summary>
    /// Adapts one NEAT individual to the vehicle's normalized steering output.
    /// </summary>
    public interface INeatVehicleController
    {
        float EvaluateSteering(IReadOnlyList<float> sensorReadings);
    }
}
