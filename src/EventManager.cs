using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Aadev.JTF.Editor
{
    internal class EventManager
    {
        private readonly List<ChangedEvent> changedEvents;
        private readonly EventManager? parent;
        public EventManager(IIdentifiersManager identifiersManager, EventManager? parent)
        {
            Span<JtNode> array = identifiersManager.GetRegisteredNodes();

            changedEvents = new List<ChangedEvent>();
            for (int i = 0; i < array.Length; i++)
            {
                changedEvents.Add(new ChangedEvent(array[i].Id!));
            }
            identifiersManager.NodeRegistered += IdentifiersManager_NodeRegistered;
            identifiersManager.NodeUnregistered += IdentifiersManager_NodeUnregistered;
            this.parent = parent;
        }

        private void IdentifiersManager_NodeUnregistered(object? sender, NodeIdentifierEventArgs e)
        {
            Span<ChangedEvent> changedEventsSpan = CollectionsMarshal.AsSpan(changedEvents);
            for (int i = 0; i < changedEventsSpan.Length; i++)
            {
                if (changedEventsSpan[i].Id == e.Id)
                {
                    changedEvents.RemoveAt(i);
                }
            }
        }

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