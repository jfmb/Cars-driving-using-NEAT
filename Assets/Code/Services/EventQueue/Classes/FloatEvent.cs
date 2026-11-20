using System;
using Services.EventQueue.Classes.EventData;
using Services.EventQueue.Events.Interfaces;

namespace Services.EventQueue.Classes
{
    public class FloatEvent : IEventSender
    {
        public delegate void FloatEventHandler(object source, FloatEventData args);
        public event FloatEventHandler FloatEventSender;

        public void SendEvent(EventArgs args)
        {
            FloatEventSender?.Invoke(this, (FloatEventData)args);
        }
    }
}
