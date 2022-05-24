using Aadev.JTF.Editor.EditorItems;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Aadev.JTF.Editor
{
    internal class EventManager
    {
        private readonly List<ChangedEvent> chnagedEvents = new();
        public bool RegistryEvent(EditorItem editorItem, object? value)
        {
            if (chnagedEvents.Any(x => x.Id == editorItem.Node.Id))
            {
                return false;
            }

            chnagedEvents.Add(new ChangedEvent(editorItem, value));
            return true;
        }

        public bool InvokeEvent(string id, object? value)
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
        private object? value;
        public string Id => EditorItem.Node.Id!;
        public EditorItem EditorItem { get; }
        public object? Value { get => value; set { this.value = value; Event?.Invoke(this, new ChangedEventArgs(this.value)); } }
        public event ChangedValueEventHandler? Event;

        public ChangedEvent(EditorItem editorItem, object? value)
        {
            EditorItem = editorItem;
            if (EditorItem.Node.Id is null)
                throw new Exception("Editor item must have id in events");
            Value = value;
        }
        public void Invoke(object? value) => Value = value;
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