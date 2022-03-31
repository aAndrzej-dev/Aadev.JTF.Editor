using System;
using System.Collections.Generic;
using System.Linq;

namespace Aadev.JTF.Editor
{
    internal class EventManager
    {
        private readonly List<ChangedEvent> chnagedEvents = new();
        public bool RegistryEvent(string Id, object? Value)
        {
            if (chnagedEvents.Any(x => x.Id == Id))
            {
                return false;
            }

            chnagedEvents.Add(new ChangedEvent(Id, Value));
            return true;
        }

        public bool InvokeEvent(string Id, object? Value)
        {
            ChangedEvent? ce = chnagedEvents.FirstOrDefault(x => x.Id == Id);


            if (ce is null)
            {
                return false;
            }

            ce.Invoke(Value);
            return true;
        }
        public ChangedEvent? GetEvent(string Id) => chnagedEvents.FirstOrDefault(x => x.Id == Id);
    }
    internal class ChangedEvent
    {
        private object? value;
        public string Id;
        public object? Value { get => value; set { this.value = value; Event?.Invoke(this, new ChangedEventArgs(this.value)); } }
        public event ChangedValueEventHandler? Event;

        public ChangedEvent(string Id, object? Value)
        {
            this.Id = Id;
            this.Value = Value;
        }
        public void Invoke(object? Value) => this.Value = Value;
    }
    internal class ChangedEventArgs : EventArgs
    {
        public object? Value { get; }

        public ChangedEventArgs(object? value)
        {
            Value = value;
        }
    }

    internal delegate void ChangedValueEventHandler(object? sender, ChangedEventArgs e);
}