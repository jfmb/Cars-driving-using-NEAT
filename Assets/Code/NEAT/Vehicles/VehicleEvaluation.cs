using UnityEngine;
using UnityEngine.Assertions;

namespace NeatSimulation.Vehicles
{
    /// <summary>
    /// Measures one evaluation run for a vehicle: path-based progress, lap counting, collision and
    /// evaluation time-limit outcomes. The coordinator calls BeginEvaluation per evaluation slot
    /// and reads Fitness, LapCount and IsComplete; the run itself stays independent of the NEAT
    /// algorithm.
    /// </summary>
    [RequireComponent(typeof(VehicleController))]
    public class VehicleEvaluation : MonoBehaviour
    {
        private VehicleEvaluationRun _run;
        private VehicleController _vehicleController;

        /// <summary>
        /// Cumulative distance progressed along the route; frozen when the evaluation completes.
        /// 0 before the first BeginEvaluation.
        /// </summary>
        public float Fitness => _run?.Fitness ?? 0f;

        /// <summary>
        /// Full route laps completed so far; 0 before the first BeginEvaluation.
        /// </summary>
        public int LapCount => _run?.LapCount ?? 0;

        /// <summary>
        /// True once the current evaluation ended by collision or time limit; false before
        /// BeginEvaluation is called.
        /// </summary>
        public bool IsComplete => _run?.IsComplete ?? false;

        private void Awake()
        {
            _vehicleController = GetComponent<VehicleController>();
            Assert.IsNotNull(_vehicleController, "vehicleController must be != null");
        }

        private void FixedUpdate()
        {
            if (_run == null || _run.IsComplete)
            {
                return;
            }

            _run.Tick(Time.fixedDeltaTime, transform.position, _vehicleController.HasCollided);
            if (_run.IsComplete)
            {
                _vehicleController.StopAfterEvaluation();
            }
        }

        /// <summary>
        /// Starts a fresh path-based evaluation with the given time limit and resets the vehicle's
        /// collision state.
        /// </summary>
        public void BeginEvaluation(TrackProgressPath trackProgressPath, float timeLimitSeconds)
        {
            _vehicleController.ResetCollisionState();
            _run = new VehicleEvaluationRun(trackProgressPath, timeLimitSeconds, transform.position);
        }
    }
}
