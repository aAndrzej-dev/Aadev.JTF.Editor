using Aadev.JTF.Editor.ViewModels;
using Newtonsoft.Json.Linq;

namespace Aadev.JTF.Editor;

public class JtfEditorAction
{
    private JtfEditorAction? reversedAction;
    public JtEditorActionType Type { get; }
    public JToken? OldValue { get; }
    public JToken? NewValue { get; }
    public JtNodeViewModel Invoker { get; }
    public JtfEditorAction(JtEditorActionType type, JToken? oldValue, JToken? newValue, JtNodeViewModel invoker)
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

    public enum JtEditorActionType
    {
        None,
        ChangeValue,
        ChangeTwinType,
        DynamicNameChanged,
        AddToken,
        RemoveToken,
    }
    public override string ToString() => $"Event: {Type} Node: {Invoker.Node.Name} OldValue: \"{OldValue}\" NewValue: \"{NewValue}\"";
}


