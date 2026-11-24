using System;
using NeatSimulation.Integration;
using UnityEngine;
using UnityEngine.Assertions;

namespace NeatSimulation.Vehicles
{
    /// <summary>
    /// Drives one vehicle from steering and normalized target-speed controls, changing speed progressively.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(VehicleSensors))]
    public class AdaptiveSpeedVehicleController : VehicleControllerBase
    {
        private const string TrackLayerName = "Track";
        private const float MinSteering = -1f;
        private const float MaxSteering = 1f;

        [Header("Speed")]
        [Tooltip("Minimum forward speed in meters per second.")]
        [SerializeField, Min(0.01f)] private float minimumSpeed = 5f;
        [Tooltip("Maximum forward speed in meters per second.")]
        [SerializeField, Min(0.01f)] private float maximumSpeed = 30f;
        [Tooltip("Speed increase in meters per second, per second.")]
        [SerializeField, Min(0.01f)] private float accelerationRate = 6f;
        [Tooltip("Speed decrease in meters per second, per second.")]
        [SerializeField, Min(0.01f)] private float decelerationRate = 10f;

        [Header("Steering")]
        [Tooltip("Turn rate in degrees per second applied at full steering.")]
        [SerializeField, Min(0.01f)] private float steeringDegreesPerSecond = 180f;

        private INeatVehicleController _controller;
        private VehicleSensors _sensors;
        private int _trackLayer;
        private Rigidbody _rigidbody;
        private float _steeringRadiansPerSecond;
        private float _currentSpeed;

        private void Awake()
        {
            ValidateConfiguration();

            _trackLayer = LayerMask.NameToLayer(TrackLayerName);
            Assert.IsTrue(_trackLayer >= 0, $"Layer '{TrackLayerName}' must exist");
            _rigidbody = GetComponent<Rigidbody>();
            _sensors = GetComponent<VehicleSensors>();
            Assert.IsNotNull(_rigidbody, "rigidbody must be != null");
            Assert.IsNotNull(_sensors, "sensors must be != null");
            _steeringRadiansPerSecond = steeringDegreesPerSecond * Mathf.Deg2Rad;
            _currentSpeed = minimumSpeed;
        }

        private void FixedUpdate()
        {
            if (_controller == null)
            {
                return;
            }

            _sensors.Sample();
            var control = _controller.EvaluateControl(_sensors.Readings);
            var steering = IsFinite(control.Steering)
                ? Mathf.Clamp(control.Steering, MinSteering, MaxSteering)
                : 0f;
            var targetSpeed = GetTargetSpeed(control.NormalizedTargetSpeed);
            var speedChangeRate = targetSpeed > _currentSpeed ? accelerationRate : decelerationRate;
            _currentSpeed = Mathf.MoveTowards(
                _currentSpeed,
                targetSpeed,
                speedChangeRate * Time.fixedDeltaTime);

            // The raceCarRed model's nose points along local -Z.
            _rigidbody.linearVelocity = -transform.forward * _currentSpeed;
            _rigidbody.angularVelocity = transform.up * (steering * _steeringRadiansPerSecond);
        }

        private float GetTargetSpeed(float normalizedTargetSpeed)
        {
            if (!IsFinite(normalizedTargetSpeed))
            {
                normalizedTargetSpeed = 0f;
            }

            var speedFraction = (Mathf.Clamp(normalizedTargetSpeed, -1f, 1f) + 1f) * 0.5f;
            return Mathf.Lerp(minimumSpeed, maximumSpeed, speedFraction);
        }

        private void ValidateConfiguration()
        {
            ValidatePositiveFinite(minimumSpeed, nameof(minimumSpeed));
            ValidatePositiveFinite(maximumSpeed, nameof(maximumSpeed));
            ValidatePositiveFinite(accelerationRate, nameof(accelerationRate));
            ValidatePositiveFinite(decelerationRate, nameof(decelerationRate));
            ValidatePositiveFinite(steeringDegreesPerSecond, nameof(steeringDegreesPerSecond));
            if (maximumSpeed < minimumSpeed)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumSpeed),
                    "Maximum speed must be greater than or equal to minimum speed.");
            }
        }

        private static void ValidatePositiveFinite(float value, string parameterName)
        {
            if (!IsFinite(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    "Value must be finite and greater than zero.");
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.layer == _trackLayer)
            {
                return;
            }

            RegisterCollision(other.collider);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<VehicleCheckpoint>() != null)
            {
                return;
            }

            RegisterCollision(other);
        }

        private void RegisterCollision(Collider other)
        {
            Debug.LogWarning(
                $"[{name}] Collision with {other.name}, " +
                $"Layer: {LayerMask.LayerToName(other.gameObject.layer)}");
            HasCollided = true;
            StopDriving();
        }

        private void StopDriving()
        {
            _controller = null;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        public override void Initialize(INeatVehicleController controller)
        {
            _controller = controller;
        }

        public override void ResetCollisionState()
        {
            HasCollided = false;
        }

        public override void StopAfterEvaluation()
        {
            StopDriving();
        }

        public override VehicleEvaluationBase GetOrAddEvaluation()
        {
            var evaluation = GetComponent<AdaptiveVehicleEvaluation>();
            if (evaluation == null)
            {
                evaluation = gameObject.AddComponent<AdaptiveVehicleEvaluation>();
            }

            return evaluation;
        }
    }
}
