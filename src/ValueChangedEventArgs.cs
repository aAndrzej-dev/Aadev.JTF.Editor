using System;

namespace Aadev.JTF.Editor;
public class ValueChangedEventArgs : EventArgs
{
    public JtfEditorAction Action { get; }
    public bool ReplaceValue { get; }

    public ValueChangedEventArgs(JtfEditorAction action, bool replaceValue)
    {
        Action = action;
        ReplaceValue = replaceValue;
    }
}