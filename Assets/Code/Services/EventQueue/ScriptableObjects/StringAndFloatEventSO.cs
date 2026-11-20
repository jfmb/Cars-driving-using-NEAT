using Services.EventQueue.Classes;
using UnityEngine;

namespace Services.EventQueue.Events.ScriptableObjects.StringAndIntEventSO
{
    [CreateAssetMenu(fileName = "StringAndFloatEventSO", menuName = "ScriptableObjects/Events/Create StringAndFloatEventSO")]

    public class StringAndFloatEventSO : EventSO
    {
        public StringAndFloatEventSO()
        {
            _eventSender = new StringAndFloatEvent();
        }
    }
}