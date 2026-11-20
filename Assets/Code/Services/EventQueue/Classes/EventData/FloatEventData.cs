using System;

namespace Services.EventQueue.Classes.EventData
{
    public class FloatEventData : EventArgs
    {
        public float Value { get; }

        public FloatEventData(float value)
        {
            Value = value;
        }
    }
}
