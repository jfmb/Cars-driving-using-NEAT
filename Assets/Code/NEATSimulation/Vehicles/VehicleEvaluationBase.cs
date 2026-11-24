using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace NeatSimulation.Vehicles
{
    /// <summary>
    /// Shared evaluation contract used by the coordinator and ordered checkpoints.
    /// </summary>
    public abstract class VehicleEvaluationBase : MonoBehaviour
    {
        private VehicleControllerBase _vehicleController;

        public abstract float Fitness { get; }
        public abstract bool IsComplete { get; }
        protected abstract bool HasRun { get; }
        protected bool VehicleHasCollided => _vehicleController.HasCollided;

        protected virtual void Awake()
        {
            _vehicleController = GetComponent<VehicleControllerBase>();
            Assert.IsNotNull(_vehicleController, "vehicleController must be != null");
        }

        protected virtual void FixedUpdate()
        {
            if (!HasRun || IsComplete)
            {
                return;
            }

            AdvanceRun(Time.fixedDeltaTime, _vehicleController.HasCollided);
            if (IsComplete)
            {
                _vehicleController.StopAfterEvaluation();
            }
        }

        public abstract void BeginEvaluation(
            IReadOnlyList<VehicleCheckpoint> checkpoints,
            float timeLimitSeconds);

        public void RegisterCheckpoint(VehicleCheckpoint checkpoint)
        {
            TryReachCheckpoint(checkpoint);
            if (IsComplete)
            {
                _vehicleController.StopAfterEvaluation();
            }
        }

        protected void ResetVehicleCollisionState()
        {
            _vehicleController.ResetCollisionState();
        }

        protected abstract void AdvanceRun(float deltaTime, bool vehicleCollided);
        protected abstract void TryReachCheckpoint(VehicleCheckpoint checkpoint);
    }
}
