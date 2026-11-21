using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace NeatSimulation.Vehicles
{
    /// <summary>
    /// Samples a fixed fan of ray sensors around the vehicle and caches the ordered readings.
    /// The fan is symmetric around the vehicle's forward direction and ordered from the leftmost
    /// sensor to the rightmost one. The readings list instance and order are stable across frames;
    /// the vehicle controller refreshes it once per physics step, so no per-frame allocation happens.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class VehicleSensors : MonoBehaviour
    {
        [SerializeField] private int sensorCount = 5;
        [Tooltip("Total angular spread of the sensor fan in degrees, symmetric around forward.")]
        [SerializeField] private float sensorSpreadDegrees = 90f;
        [Tooltip("Maximum ray distance in meters; readings are normalized against it.")]
        [SerializeField] private float sensorDistance = 10f;
        [Tooltip("Layers the sensor rays detect. Exclude the vehicle's own layer.")]
        [SerializeField] private LayerMask sensorMask;

        private readonly List<float> _readings = new List<float>();
        private Vector3[] _localDirections;
        private Collider _vehicleCollider;

        /// <summary>
        /// Cached ordered sensor readings normalized to (0, 1]: 1 means a clear ray and smaller
        /// values mean an obstacle within range. Read-only input for the NEAT controller.
        /// </summary>
        public IReadOnlyList<float> Readings => _readings;

        private void Awake()
        {
            Assert.IsTrue(sensorCount > 0, "sensorCount must be > 0");
            Assert.IsTrue(sensorDistance > 0f, "sensorDistance must be > 0");
            Assert.IsTrue(sensorMask.value != 0, "sensorMask must include detectable track layers");
            _vehicleCollider = GetComponent<Collider>();
            Assert.IsNotNull(_vehicleCollider, "vehicleCollider must be != null");
            _localDirections = BuildLocalDirections();
            _readings.Capacity = sensorCount;
            for (var i = 0; i < sensorCount; i++)
            {
                _readings.Add(1f);
            }
        }

        /// <summary>
        /// Casts every sensor ray and refreshes the cached readings in place.
        /// </summary>
        public void Sample()
        {
            var origin = _vehicleCollider.bounds.center;
            for (var i = 0; i < _localDirections.Length; i++)
            {
                var direction = transform.TransformDirection(_localDirections[i]);
                if (Physics.Raycast(origin, direction, out var hit, sensorDistance, sensorMask, QueryTriggerInteraction.Collide))
                {
                    _readings[i] = hit.distance / sensorDistance;
                }
                else
                {
                    _readings[i] = 1f;
                }
            }
        }

        private Vector3[] BuildLocalDirections()
        {
            var directions = new Vector3[sensorCount];
            var halfSpread = sensorSpreadDegrees * 0.5f;
            for (var i = 0; i < sensorCount; i++)
            {
                var spreadFraction = sensorCount == 1 ? 0.5f : (float)i / (sensorCount - 1);
                var angle = Mathf.Lerp(halfSpread, -halfSpread, spreadFraction);
                directions[i] = Quaternion.Euler(0f, angle, 0f) * Vector3.back;
            }

            return directions;
        }

        private void OnDrawGizmosSelected()
        {
            if (_localDirections == null)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            var origin = transform.position;
            for (var i = 0; i < _localDirections.Length; i++)
            {
                Gizmos.DrawRay(origin, transform.TransformDirection(_localDirections[i]) * sensorDistance);
            }
        }
    }
}
