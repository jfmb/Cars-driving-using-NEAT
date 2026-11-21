using NeatSimulation.Vehicles;
using UnityEngine;

namespace NeatSimulation.Simulation
{
    /// <summary>
    /// Bookkeeping record for one individual of the population during a simulation start: the vehicle
    /// instance created for it, its evaluation slot, and whether its Unity-computed fitness has
    /// already been reported to the NEAT adapter (so fitness is reported exactly once).
    /// </summary>
    public sealed class IndividualEvaluation
    {
        public IndividualEvaluation(int individualId, GameObject vehicleInstance, VehicleEvaluation evaluation)
        {
            IndividualId = individualId;
            VehicleInstance = vehicleInstance;
            Evaluation = evaluation;
        }

        public int IndividualId { get; }

        public GameObject VehicleInstance { get; }

        public VehicleEvaluation Evaluation { get; }

        public bool HasReportedFitness { get; set; }
    }
}
