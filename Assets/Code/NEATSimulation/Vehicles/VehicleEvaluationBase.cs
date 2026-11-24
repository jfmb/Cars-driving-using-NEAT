using System.Collections.Generic;
using UnityEngine;

namespace NeatSimulation.Vehicles
{
    /// <summary>
    /// Shared evaluation contract used by the coordinator and ordered checkpoints.
    /// </summary>
    public abstract class VehicleEvaluationBase : MonoBehaviour
    {
        public abstract float Fitness { get; }
        public abstract bool IsComplete { get; }

        public abstract void BeginEvaluation(
            IReadOnlyList<VehicleCheckpoint> checkpoints,
            float timeLimitSeconds);

        public abstract void RegisterCheckpoint(VehicleCheckpoint checkpoint);
    }
}
