using Aadev.JTF.Editor.EditorItems;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Aadev.JTF.Editor
{
    internal class EventManager
    {
        private readonly List<ChangedEvent> chnagedEvents = new();
        public static EventManager CreateEventManager(IIdentifiersManager identifiersManager)
        {
            return new EventManager(identifiersManager);
        }
        private EventManager(IIdentifiersManager identifiersManager)
        {
            foreach (JtNode? item in identifiersManager.GetRegisteredNodes())
            {
                chnagedEvents.Add(new ChangedEvent(item.Id!));
            }
        }

        public bool InvokeEvent(string id, JToken? value)
        {
            if (chnagedEvents.FirstOrDefault(x => x.Id == id) is not ChangedEvent ce)
            {
                return false;
            }

            ce.Invoke(value);
            return true;
        }
        public ChangedEvent? GetEvent(string id) => chnagedEvents.FirstOrDefault(x => x.Id == id);
    }
    internal class ChangedEvent
    {
        private JToken? value;
        public string Id { get; }
        public JToken? Value { get => value; set { this.value = value; Event?.Invoke(this, new ChangedEventArgs(this.value)); } }
        public event ChangedValueEventHandler? Event;

        public ChangedEvent(string id)
        {
            Id = id;
        }
        public void Invoke(JToken? value) => Value = value;
    }
    internal class ChangedEventArgs : EventArgs
    {
        public JToken? Value { get; }

        public ChangedEventArgs(JToken? value)
        {
            Value = value;
        }
    }

    internal delegate void ChangedValueEventHandler(object? sender, ChangedEventArgs e);
}