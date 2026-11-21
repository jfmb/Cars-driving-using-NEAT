using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeatSimulation.Vehicles
{
    /// <summary>
    /// Ordered, explicitly closed track centerline. Owns the projection of world positions onto the
    /// route and cumulative distance queries; lap accounting builds on this, never on Euclidean
    /// displacement between frames. Consecutive points must be distinct and the last point connects
    /// back to the first, closing the loop. The centerline is serialized so a scene coordinator can
    /// configure it in the Inspector; the projection data is derived once on first use.
    /// </summary>
    [Serializable]
    public class TrackProgressPath
    {
        [Tooltip("Ordered centerline points of the closed route; the last point connects back to the first.")]
        [SerializeField] private Vector3[] centerlinePoints;

        private bool _isBuilt;
        private Vector3[] _points;
        private float[] _segmentLengths;
        private float[] _cumulativeDistances;
        private float _totalLength;

        /// <summary>
        /// Cumulative length of the whole closed route.
        /// </summary>
        public float TotalLength
        {
            get
            {
                EnsureBuilt();
                return _totalLength;
            }
        }

        /// <summary>
        /// Builds the path from an explicitly ordered centerline; used by code-constructed routes.
        /// </summary>
        public TrackProgressPath(IReadOnlyList<Vector3> closedCenterline)
        {
            if (closedCenterline == null)
            {
                throw new ArgumentNullException(nameof(closedCenterline));
            }

            if (closedCenterline.Count < 3)
            {
                throw new ArgumentException("A closed centerline needs at least 3 points.", nameof(closedCenterline));
            }

            centerlinePoints = new Vector3[closedCenterline.Count];
            for (var i = 0; i < closedCenterline.Count; i++)
            {
                centerlinePoints[i] = closedCenterline[i];
            }
        }

        /// <summary>
        /// Parameterless constructor used by Unity serialization; the Inspector supplies the
        /// centerline points.
        /// </summary>
        public TrackProgressPath()
        {
        }

        /// <summary>
        /// Projects the world position onto the closed centerline and returns the cumulative route
        /// distance of the closest point, wrapped into [0, TotalLength).
        /// </summary>
        public float GetDistanceAlongPath(Vector3 position)
        {
            EnsureBuilt();

            var bestIndex = 0;
            var bestT = 0f;
            var bestSquaredDistance = float.MaxValue;

            for (var i = 0; i < _points.Length; i++)
            {
                var segment = _points[(i + 1) % _points.Length] - _points[i];
                var t = Mathf.Clamp01(Vector3.Dot(position - _points[i], segment) / segment.sqrMagnitude);
                var squaredDistance = (position - (_points[i] + segment * t)).sqrMagnitude;
                if (squaredDistance < bestSquaredDistance)
                {
                    bestSquaredDistance = squaredDistance;
                    bestIndex = i;
                    bestT = t;
                }
            }

            var distance = _cumulativeDistances[bestIndex] + bestT * _segmentLengths[bestIndex];
            if (distance >= _totalLength)
            {
                distance -= _totalLength;
            }

            return distance;
        }

        /// <summary>
        /// Returns the world position and forward direction at the given cumulative route distance,
        /// wrapping distances outside [0, TotalLength).
        /// </summary>
        public TrackPose GetPoseAtDistance(float distance)
        {
            EnsureBuilt();

            var wrappedDistance = Mathf.Repeat(distance, _totalLength);
            var segmentIndex = FindSegmentIndex(wrappedDistance);
            var nextIndex = (segmentIndex + 1) % _points.Length;
            var segmentT = (wrappedDistance - _cumulativeDistances[segmentIndex]) / _segmentLengths[segmentIndex];
            var position = Vector3.Lerp(_points[segmentIndex], _points[nextIndex], segmentT);
            var forward = (_points[nextIndex] - _points[segmentIndex]).normalized;
            return new TrackPose(position, forward);
        }

        private void EnsureBuilt()
        {
            if (_isBuilt)
            {
                return;
            }

            ValidateCenterline();
            BuildProjectionData();
            _isBuilt = true;
        }

        private void ValidateCenterline()
        {
            if (centerlinePoints == null || centerlinePoints.Length < 3)
            {
                throw new ArgumentException("A closed centerline needs at least 3 points.", nameof(centerlinePoints));
            }
        }

        private void BuildProjectionData()
        {
            _points = centerlinePoints;
            _segmentLengths = new float[_points.Length];
            _cumulativeDistances = new float[_points.Length];
            var cumulative = 0f;
            for (var i = 0; i < _points.Length; i++)
            {
                var segment = _points[(i + 1) % _points.Length] - _points[i];
                var segmentLength = segment.magnitude;
                if (segmentLength <= 0f)
                {
                    throw new ArgumentException("Consecutive centerline points must be distinct.", nameof(centerlinePoints));
                }

                _segmentLengths[i] = segmentLength;
                _cumulativeDistances[i] = cumulative;
                cumulative += segmentLength;
            }

            _totalLength = cumulative;
        }

        private int FindSegmentIndex(float wrappedDistance)
        {
            var low = 0;
            var high = _points.Length - 1;
            while (low < high)
            {
                var middle = (low + high + 1) / 2;
                if (_cumulativeDistances[middle] <= wrappedDistance)
                {
                    low = middle;
                }
                else
                {
                    high = middle - 1;
                }
            }

            return low;
        }
    }
}
