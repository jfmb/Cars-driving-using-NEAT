using System;
using Services.EventQueue;
using Services.EventQueue.Events.ScriptableObjects;
using UnityEngine;

public class SimpleEventSender : MonoBehaviour
{
    [SerializeField] private EventId simpleEventId;

    public void SendSimpleEventFromAnimation()
    {
        Debug.Log("Sending start countdown event");
        ServiceLocator.GetService<EventQueue>().EnqueueEvent(simpleEventId, EventArgs.Empty);
    }
}
