using System.Collections.Generic;

namespace NeatSimulation.Integration
{
    /// <summary>
    /// Bridges the Unity evaluation lifecycle to the NEAT Core without exposing genome types.
    /// </summary>
    public interface INeatSimulationAdapter
    {
        IReadOnlyList<INeatVehicleController> CreatePopulation(int populationSize);
        void ReportFitness(int individualId, float fitness);
    }
}
