using System;

namespace Services.EventQueue.Classes.EventData
{
    public class StringAndFloatEventData: EventArgs
    {
        public string Text { get; }
        public float Seconds { get; }

        public StringAndFloatEventData(string text, float seconds)
        {
            Text = text;
            Seconds = seconds;
        }
    }
}