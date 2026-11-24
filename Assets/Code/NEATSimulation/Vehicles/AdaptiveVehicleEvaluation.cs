using System.Collections.Generic;
using UnityEngine;

namespace NeatSimulation.Vehicles
{
    /// <summary>
    /// Measures ordered circuit progress and ranks clean completion by elapsed time.
    /// </summary>
    [RequireComponent(typeof(AdaptiveSpeedVehicleController))]
    public sealed class AdaptiveVehicleEvaluation : VehicleEvaluationBase
    {
        private AdaptiveVehicleEvaluationRun _run;

        public override float Fitness => _run?.Fitness ?? 0f;
        public override bool IsComplete => _run?.IsComplete ?? false;
        protected override bool HasRun => _run != null;

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
        }

        protected override void AdvanceRun(float deltaTime, bool vehicleCollided)
        {
            _run.Tick(deltaTime, vehicleCollided);
        }

        public override void BeginEvaluation(
            IReadOnlyList<VehicleCheckpoint> checkpoints,
            float timeLimitSeconds)
        {
            ResetVehicleCollisionState();
            _run = new AdaptiveVehicleEvaluationRun(checkpoints, timeLimitSeconds);
        }

        protected override void TryReachCheckpoint(VehicleCheckpoint checkpoint)
        {
            _run?.TryReachCheckpoint(checkpoint);
        }
    }
}
