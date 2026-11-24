using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

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
        private VehicleControllerBase _vehicleController;

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

        private void Awake()
        {
            _vehicleController = GetComponent<VehicleControllerBase>();
            Assert.IsNotNull(_vehicleController, "vehicleController must be != null");
        }

        private void FixedUpdate()
        {
            if (_run == null || _run.IsComplete)
            {
                return;
            }

            _run.Tick(Time.fixedDeltaTime, _vehicleController.HasCollided);
            if (_run.IsComplete)
            {
                _vehicleController.StopAfterEvaluation();
            }
        }

        /// <summary>
        /// Starts a fresh checkpoint-based evaluation with the given time limit and resets the
        /// vehicle's collision state.
        /// </summary>
        public override void BeginEvaluation(IReadOnlyList<VehicleCheckpoint> checkpoints, float timeLimitSeconds)
        {
            _vehicleController.ResetCollisionState();
            _run = new VehicleEvaluationRun(checkpoints, timeLimitSeconds);
        }

        public override void RegisterCheckpoint(VehicleCheckpoint checkpoint)
        {
            _run?.TryReachCheckpoint(checkpoint);
        }
    }
}
