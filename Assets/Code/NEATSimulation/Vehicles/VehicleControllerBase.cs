using NeatSimulation.Integration;
using UnityEngine;

namespace NeatSimulation.Vehicles
{
    /// <summary>
    /// Shared controller contract used by simulation and evaluation plumbing.
    /// </summary>
    public abstract class VehicleControllerBase : MonoBehaviour
    {
        public bool HasCollided { get; protected set; }

        public abstract void Initialize(INeatVehicleController controller);
        public abstract void ResetCollisionState();
        public abstract void StopAfterEvaluation();
        public abstract VehicleEvaluationBase GetOrAddEvaluation();
    }
}
