using System.Collections.Generic;
using UnityEngine;

namespace NeatSimulation.Vehicles
{
    /// <summary>
    /// Measures one vehicle's checkpoint progress, survival time, collision and evaluation time-limit
    /// outcome. The coordinator starts each evaluation and reports its fitness to the NEAT adapter.
    /// </summary>
    [RequireComponent(typeof(VehicleController))]
    public class VehicleEvaluation : VehicleEvaluationBase
    {
        private VehicleEvaluationRun _run;

        /// <summary>
        /// Fitness from reached checkpoints and survival time; frozen when the evaluation completes.
        /// 0 before the first BeginEvaluation.
        /// </summary>
        public override float Fitness => _run?.Fitness ?? 0f;

        /// <summary>
        /// True once the current evaluation ended by collision or time limit; false before
        /// BeginEvaluation is called.
        /// </summary>
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

        /// <summary>
        /// Starts a fresh checkpoint-based evaluation with the given time limit and resets the
        /// vehicle's collision state.
        /// </summary>
        public override void BeginEvaluation(IReadOnlyList<VehicleCheckpoint> checkpoints, float timeLimitSeconds)
        {
            ResetVehicleCollisionState();
            _run = new VehicleEvaluationRun(checkpoints, timeLimitSeconds);
        }

        protected override void TryReachCheckpoint(VehicleCheckpoint checkpoint)
        {
            _run?.TryReachCheckpoint(checkpoint);
        }
    }
}
