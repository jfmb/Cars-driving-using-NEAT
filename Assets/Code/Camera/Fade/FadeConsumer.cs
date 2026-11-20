using Services.EventQueue;
using Services.EventQueue.Classes.EventData;
using Services.EventQueue.Events.ScriptableObjects;
using UnityEngine;
using UnityEngine.Assertions;

public class FadeConsumer : AbstractSecondaryInteraction
{
    [SerializeField] private EventId fadeEventId;
    [SerializeField] private float seconds = 1f;

    private void Awake()
    {
        Assert.IsNotNull(fadeEventId, "fadeEventId must be != null");
    }

    public override void ExecuteSecondaryInteraction()
    {
        SendFadeEvent();
    }

    public void SendFadeEvent()
    {
        var args = new FloatEventData(seconds);
        ServiceLocator.GetService<EventQueue>().EnqueueEvent(fadeEventId, args);
    }
}
