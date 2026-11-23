using UnityEngine;
using UnityEngine.Assertions;

namespace NeatSimulation.Vehicles
{
    /// <summary>
    /// Reports vehicle entries to their own evaluation while remaining reusable by every generation.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class VehicleCheckpoint : MonoBehaviour
    {
        private Collider _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            Assert.IsNotNull(_collider, "collider must be != null");
            Assert.IsTrue(_collider.isTrigger, "checkpoint collider must be a trigger");
        }

        private void OnTriggerEnter(Collider other)
        {
            var evaluation = other.GetComponentInParent<VehicleEvaluation>();
            if (evaluation == null)
            {
                return;
            }

            evaluation.RegisterCheckpoint(this);
        }
    }
}
