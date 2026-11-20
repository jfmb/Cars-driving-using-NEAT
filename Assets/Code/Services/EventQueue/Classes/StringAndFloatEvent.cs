using System;
using Services.EventQueue.Classes.EventData;
using Services.EventQueue.Events.Interfaces;

namespace Services.EventQueue.Classes
{
    public class StringAndFloatEvent: IEventSender
    {
        public delegate void StringAndFloatEventHandler(object source, StringAndFloatEventData args);
        public event StringAndFloatEventHandler StringAndSecondsEventSender;
        
        public void SendEvent(EventArgs args)
        {
            StringAndSecondsEventSender?.Invoke(this, (StringAndFloatEventData) args);
        }
    }
}