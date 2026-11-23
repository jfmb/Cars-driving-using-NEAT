using System;
using System.Collections.Generic;

namespace NeatSimulation.Vehicles
{
    /// <summary>
    /// Plain C# state of one evaluation run: ordered checkpoint progress, survival time and the
    /// collision or time-limit outcome. Fitness is Unity-computed and never consults NEAT internals.
    /// </summary>
    public class VehicleEvaluationRun
    {
        private const float CheckpointReward = 100f;

        private readonly IReadOnlyList<VehicleCheckpoint> _checkpoints;
        private readonly HashSet<VehicleCheckpoint> _reachedCheckpoints = new HashSet<VehicleCheckpoint>();
        private readonly float _timeLimitSeconds;
        private float _elapsed;
        private int _nextCheckpointIndex;
        private int _checkpointsReached;
        private bool _isComplete;

        /// <summary>
        /// True once the run ended by collision or time limit.
        /// </summary>
        public bool IsComplete => _isComplete;

        /// <summary>
        /// Number of checkpoints reached in the configured order.
        /// </summary>
        public int CheckpointsReached => _checkpointsReached;

        /// <summary>
        /// Checkpoint reward plus survival time; frozen when the run completes.
        /// </summary>
        public float Fitness => _checkpointsReached * CheckpointReward + _elapsed;

        public VehicleEvaluationRun(IReadOnlyList<VehicleCheckpoint> checkpoints, float timeLimitSeconds)
        {
            _checkpoints = checkpoints ?? throw new ArgumentNullException(nameof(checkpoints));
            if (timeLimitSeconds <= 0f)
            {
                throw new ArgumentException("The evaluation time limit must be > 0.", nameof(timeLimitSeconds));
            }

            _timeLimitSeconds = timeLimitSeconds;
        }

        /// <summary>
        /// Advances the run by one physics step. The collision flag comes from the vehicle
        /// controller; the run completes on the first collision or when the time limit is reached.
        /// </summary>
        public void Tick(float deltaTime, bool vehicleCollided)
        {
            if (_isComplete)
            {
                return;
            }

            if (vehicleCollided)
            {
                _isComplete = true;
                return;
            }

            _elapsed = Math.Min(_elapsed + deltaTime, _timeLimitSeconds);
            if (_elapsed >= _timeLimitSeconds)
            {
                _isComplete = true;
            }
        }

        public bool TryReachCheckpoint(VehicleCheckpoint checkpoint)
        {
            if (_isComplete || checkpoint == null || _reachedCheckpoints.Contains(checkpoint) ||
                _nextCheckpointIndex >= _checkpoints.Count || checkpoint != _checkpoints[_nextCheckpointIndex])
            {
                return false;
            }

            _reachedCheckpoints.Add(checkpoint);
            _nextCheckpointIndex++;
            _checkpointsReached++;
            return true;
        }
    }
}
