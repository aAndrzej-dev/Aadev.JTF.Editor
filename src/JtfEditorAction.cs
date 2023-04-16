using Aadev.JTF.Editor.EditorItems;
using Newtonsoft.Json.Linq;
using System;

namespace Aadev.JTF.Editor
{
    internal class JtfEditorAction
    {
        private JtfEditorAction? reversedAction;
        public JtEditorActionType Type {get;}
        public JToken? OldValue { get; }
        public JToken? NewValue { get; }
        public EditorItem Invoker { get; }
        public JtfEditorAction(JtEditorActionType type, JToken? oldValue, JToken? newValue, EditorItem invoker)
        {
            Type = type;
            OldValue = oldValue;
            NewValue = newValue;
            Invoker = invoker;
        }
        private JtfEditorAction(JtfEditorAction reversed)
        {
            reversedAction = reversed;
            OldValue = reversed.NewValue;
            NewValue = reversed.OldValue;
            Type = reversed.Type switch
            {
                JtEditorActionType.AddToken => JtEditorActionType.RemoveToken,
                JtEditorActionType.RemoveToken => JtEditorActionType.AddToken,
                _ => reversed.Type,
            };
            Invoker = reversed.Invoker;
        }
        public JtfEditorAction Reverse()
        {
            if (reversedAction is not null)
                return reversedAction;
            return reversedAction = new JtfEditorAction(this);
        }

        internal enum JtEditorActionType
        {
            None,
            ChangeValue,
            ChangeTwinType,
            DynamicNameChanged,
            AddToken,
            RemoveToken,
        }
    }

    internal class ValueChangedEventArgs : EventArgs
    {
        public JtfEditorAction Action { get; }

        public ValueChangedEventArgs(JtfEditorAction action)
        {
            Action = action;
        }
    }
}
