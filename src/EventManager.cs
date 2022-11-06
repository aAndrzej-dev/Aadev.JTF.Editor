using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Aadev.JTF.Editor
{
    internal class EventManager
    {
        private readonly List<ChangedEvent> changedEvents;
        private readonly EventManager? parent;
        public EventManager(IIdentifiersManager identifiersManager, EventManager? parent)
        {
            JtNode[] array = identifiersManager.GetRegisteredNodes();

            changedEvents = new List<ChangedEvent>();
            for (int i = 0; i < array.Length; i++)
            {
                changedEvents.Add(new ChangedEvent(array[i].Id!));
            }
            identifiersManager.NodeRegistered += IdentifiersManager_NodeRegistered;
            identifiersManager.NodeUnregistered += IdentifiersManager_NodeUnregistered;
            this.parent = parent;
        }

        private void IdentifiersManager_NodeUnregistered(object? sender, NodeIdentifierEventArgs e) => changedEvents.Remove(changedEvents.Where(x => x.Id == e.Id).First());
        private void IdentifiersManager_NodeRegistered(object? sender, NodeIdentifierEventArgs e) => changedEvents.Add(new ChangedEvent(e.Id));

        public ChangedEvent? GetEvent(JtIdentifier id)
        {
            foreach (ChangedEvent? item in changedEvents)
            {
                if (item.Id == id)
                    return item;
            }
            return parent?.GetEvent(id);
        }
    }
    internal class ChangedEvent
    {
        private JToken? value;
        public JtIdentifier Id { get; }
        public JToken? Value { get => value; set { this.value = value; Event?.Invoke(this, EventArgs.Empty); } }
        public event EventHandler? Event;

        public ChangedEvent(JtIdentifier id)
        {
            Id = id;
        }
        public void Invoke(JToken? value) => Value = value;
    }
}