using NeatSimulation.Integration;
using UnityEngine;
using UnityEngine.Assertions;

namespace NeatSimulation.Vehicles
{
    /// <summary>
    /// Drives one vehicle with a constant positive engine speed and continuous steering in [-1, 1].
    /// Without an initialized NEAT controller the vehicle stands still: no steering or movement is
    /// fabricated to compensate for the missing NEAT Core.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(VehicleSensors))]
    public class VehicleController : MonoBehaviour
    {
        private const string TrackLayerName = "Track";
        private const float MinSteering = -1f;
        private const float MaxSteering = 1f;

        [Tooltip("Constant forward speed in meters per second; must stay positive.")]
        [SerializeField] private float speed = 5f;
        [Tooltip("Turn rate in degrees per second applied at full steering.")]
        [SerializeField] private float steeringDegreesPerSecond = 180f;

        private INeatVehicleController _controller;
        private VehicleSensors _sensors;
        private int _trackLayer;
        private Rigidbody _rigidbody;
        private float _steeringRadiansPerSecond;

        /// <summary>
        /// True from the vehicle's first physics collision until the state is reset for the next
        /// evaluation.
        /// </summary>
        public bool HasCollided { get; private set; }

        private void Awake()
        {
            Assert.IsTrue(speed > 0f, "speed must be > 0");
            Assert.IsTrue(steeringDegreesPerSecond > 0f, "steeringDegreesPerSecond must be > 0");
            _trackLayer = LayerMask.NameToLayer(TrackLayerName);
            Assert.IsTrue(_trackLayer >= 0, $"Layer '{TrackLayerName}' must exist");
            _rigidbody = GetComponent<Rigidbody>();
            _sensors = GetComponent<VehicleSensors>();
            Assert.IsNotNull(_rigidbody, "rigidbody must be != null");
            Assert.IsNotNull(_sensors, "sensors must be != null");
            _steeringRadiansPerSecond = steeringDegreesPerSecond * Mathf.Deg2Rad;
        }

        private void FixedUpdate()
        {
            if (_controller == null)
            {
                return;
            }

            _sensors.Sample();
            var steering = Mathf.Clamp(_controller.EvaluateSteering(_sensors.Readings), MinSteering, MaxSteering);
            // The raceCarRed model's nose points along local -Z.
            _rigidbody.linearVelocity = -transform.forward * speed;
            _rigidbody.angularVelocity = transform.up * (steering * _steeringRadiansPerSecond);
        }

        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.layer == _trackLayer)
            {
                return;
            }

            RegisterCollision();
        }

        private void OnTriggerEnter(Collider other)
        {
            // Debug.LogWarning(
            //     $"[{name}] Trigger entered: {other.name}, " +
            //     $"Layer: {LayerMask.LayerToName(other.gameObject.layer)}",
            //     other);
            RegisterCollision();
        }

        private void RegisterCollision()
        {
            // Debug.LogWarning(
            //     $"[{name}] Collision detected. HasCollided={HasCollided}",
            //     this);
            //
            HasCollided = true;
            StopDriving();
        }

        private void StopDriving()
        {
            _controller = null;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        /// <summary>
        /// Connects the optional NEAT policy for this vehicle. A null controller leaves the vehicle
        /// stationary.
        /// </summary>
        public void Initialize(INeatVehicleController controller)
        {
            _controller = controller;
        }

        /// <summary>
        /// Clears the collision flag before starting a new evaluation.
        /// </summary>
        public void ResetCollisionState()
        {
            HasCollided = false;
        }

        public void StopAfterEvaluation()
        {
            StopDriving();
        }
    }
}
