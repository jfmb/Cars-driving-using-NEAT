using System;
using System.Collections.Generic;

namespace NeatSimulation.Vehicles
{
    /// <summary>
    /// Plain C# adaptive run state with ordered progress and time-ranked clean completion.
    /// </summary>
    public sealed class AdaptiveVehicleEvaluationRun
    {
        private const float CleanCompletionReward = 2f;

        private readonly IReadOnlyList<VehicleCheckpoint> _checkpoints;
        private readonly float _timeLimitSeconds;
        private float _elapsed;
        private int _nextCheckpointIndex;
        private int _checkpointsReached;
        private bool _isComplete;
        private bool _isCleanCompletion;

        public bool IsComplete => _isComplete;
        public bool IsCleanCompletion => _isCleanCompletion;
        public int CheckpointsReached => _checkpointsReached;

        public float Fitness => _isCleanCompletion
            ? CleanCompletionReward + (_timeLimitSeconds - _elapsed)
            : _checkpoints.Count == 0
                ? 0f
                : (float)_checkpointsReached / (_checkpoints.Count + 1f);

        public AdaptiveVehicleEvaluationRun(IReadOnlyList<VehicleCheckpoint> checkpoints, float timeLimitSeconds)
        {
            _checkpoints = checkpoints ?? throw new ArgumentNullException(nameof(checkpoints));
            var configuredCheckpoints = new HashSet<VehicleCheckpoint>();
            for (var i = 0; i < _checkpoints.Count; i++)
            {
                if (_checkpoints[i] == null)
                {
                    throw new ArgumentException(
                        $"Checkpoint at index {i} must not be null.",
                        nameof(checkpoints));
                }

                if (!configuredCheckpoints.Add(_checkpoints[i]))
                {
                    throw new ArgumentException(
                        $"Checkpoint at index {i} is already present earlier in the route.",
                        nameof(checkpoints));
                }
            }

            if (!IsFinite(timeLimitSeconds) || timeLimitSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(timeLimitSeconds),
                    "The evaluation time limit must be finite and greater than zero.");
            }

            _timeLimitSeconds = timeLimitSeconds;
        }

        public void Tick(float deltaTime, bool vehicleCollided)
        {
            if (_isComplete && !_isCleanCompletion)
            {
                return;
            }

            if (!IsFinite(deltaTime) || deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(deltaTime),
                    "The evaluation delta time must be finite and non-negative.");
            }

            if (vehicleCollided)
            {
                _isCleanCompletion = false;
                _isComplete = true;
                return;
            }

            if (_isComplete)
            {
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
            if (_isComplete || checkpoint == null || _nextCheckpointIndex >= _checkpoints.Count ||
                checkpoint != _checkpoints[_nextCheckpointIndex])
            {
                return false;
            }

            _nextCheckpointIndex++;
            _checkpointsReached++;
            if (_nextCheckpointIndex == _checkpoints.Count)
            {
                _isCleanCompletion = true;
                _isComplete = true;
            }

            return true;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
