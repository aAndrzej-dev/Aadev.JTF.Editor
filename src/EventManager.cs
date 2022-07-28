using Newtonsoft.Json.Linq;
using System;

namespace Aadev.JTF.Editor
{
    internal class EventManager
    {
        private readonly ChangedEvent[] changedEvents;
        public EventManager(IIdentifiersManager identifiersManager)
        {
            JtNode[] array = identifiersManager.GetRegisteredNodes();
            changedEvents = new ChangedEvent[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                JtNode? item = array[i];
                changedEvents[i] = new ChangedEvent(item.Id!);
            }
        }
        public ChangedEvent? GetEvent(string id)
        {
            foreach (ChangedEvent? item in changedEvents)
            {
                if (item.Id == id)
                    return item;
            }
            return null;
        }
    }
    internal class ChangedEvent
    {
        private JToken? value;
        public string Id { get; }
        public JToken? Value { get => value; set { this.value = value; Event?.Invoke(this, EventArgs.Empty); } }
        public event EventHandler? Event;

        public ChangedEvent(string id)
        {
            Id = id;
        }
        public void Invoke(JToken? value) => Value = value;
    }
}