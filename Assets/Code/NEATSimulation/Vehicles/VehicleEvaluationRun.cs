using System;
using UnityEngine;

namespace NeatSimulation.Vehicles
{
    /// <summary>
    /// Plain C# state of one evaluation run: cumulative progress along the track route, completed
    /// laps and the collision or time-limit outcome. Fitness is Unity-computed route distance and
    /// never consults NEAT internals; crossing the start line forward is detected by the wrap of
    /// the projected route distance, not by Euclidean displacement.
    /// </summary>
    public class VehicleEvaluationRun
    {
        private readonly TrackProgressPath _trackProgressPath;
        private readonly float _timeLimitSeconds;
        private float _elapsed;
        private float _lastDistance;
        private float _cumulativeDistance;
        private bool _isComplete;

        /// <summary>
        /// True once the run ended by collision or time limit.
        /// </summary>
        public bool IsComplete => _isComplete;

        /// <summary>
        /// Full route laps completed so far; never negative.
        /// </summary>
        public int LapCount => Mathf.Max(0, Mathf.FloorToInt(_cumulativeDistance / _trackProgressPath.TotalLength));

        /// <summary>
        /// Cumulative distance progressed along the route; frozen when the run completes.
        /// </summary>
        public float Fitness => _cumulativeDistance;

        public VehicleEvaluationRun(TrackProgressPath trackProgressPath, float timeLimitSeconds, Vector3 startVehiclePosition)
        {
            _trackProgressPath = trackProgressPath ?? throw new ArgumentNullException(nameof(trackProgressPath));
            if (timeLimitSeconds <= 0f)
            {
                throw new ArgumentException("The evaluation time limit must be > 0.", nameof(timeLimitSeconds));
            }

            _timeLimitSeconds = timeLimitSeconds;
            _lastDistance = _trackProgressPath.GetDistanceAlongPath(startVehiclePosition);
        }

        /// <summary>
        /// Advances the run by one physics step. The collision flag comes from the vehicle
        /// controller; the run completes on the first collision or when the time limit is reached.
        /// </summary>
        public void Tick(float deltaTime, Vector3 vehiclePosition, bool vehicleCollided)
        {
            if (_isComplete)
            {
                return;
            }

            _elapsed += deltaTime;
            AccumulateProgress(vehiclePosition);
            if (vehicleCollided || _elapsed >= _timeLimitSeconds)
            {
                _isComplete = true;
            }
        }

        private void AccumulateProgress(Vector3 vehiclePosition)
        {
            var distance = _trackProgressPath.GetDistanceAlongPath(vehiclePosition);
            var delta = distance - _lastDistance;
            var halfLength = _trackProgressPath.TotalLength * 0.5f;
            if (delta < -halfLength)
            {
                delta += _trackProgressPath.TotalLength;
            }
            else if (delta > halfLength)
            {
                delta -= _trackProgressPath.TotalLength;
            }

            _cumulativeDistance += delta;
            _lastDistance = distance;
        }
    }
}
