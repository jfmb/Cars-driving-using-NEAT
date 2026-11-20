using Services.EventQueue.Classes;
using UnityEngine;

namespace Services.EventQueue.Events.ScriptableObjects
{
    [CreateAssetMenu(fileName = "FloatEventSO", menuName = "ScriptableObjects/Events/Create FloatEventSO")]
    public class FloatEventSO : EventSO
    {
        public FloatEventSO()
        {
            _eventSender = new FloatEvent();
        }
    }
}
