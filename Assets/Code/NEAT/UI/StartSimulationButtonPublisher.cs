using System;
using Services.EventQueue;
using Services.EventQueue.Events.ScriptableObjects;
using UnityEngine;
using UnityEngine.Assertions;

namespace NeatSimulation.UI
{
    /// <summary>
    /// Publishes the existing GameStartsEvent through the project's EventQueue when the Start
    /// Simulation button is clicked. It never invokes simulation components directly; the
    /// simulation coordinator is the event subscriber.
    /// </summary>
    public class StartSimulationButtonPublisher : MonoBehaviour
    {
        [SerializeField] private EventId gameStartsEventId;

        private void Awake()
        {
            Assert.IsNotNull(gameStartsEventId, "gameStartsEventId must be != null");
        }

        /// <summary>
        /// Bound to the button's onClick action in the Inspector.
        /// </summary>
        public void PublishStartSimulation()
        {
            ServiceLocator.GetService<EventQueue>().EnqueueEvent(gameStartsEventId, EventArgs.Empty);
        }
    }
}
