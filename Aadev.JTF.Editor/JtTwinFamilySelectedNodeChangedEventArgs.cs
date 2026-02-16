using System;
using Aadev.JTF.Editor.ViewModels;

namespace Aadev.JTF.Editor;

internal class JtTwinFamilySelectedNodeChangedEventArgs : EventArgs
{
    public JtTwinFamilySelectedNodeChangedEventArgs(JtNodeViewModel? oldNode, JtNodeViewModel? newNode)
    {
        OldNode = oldNode;
        NewNode = newNode;
    }

    public JtNodeViewModel? OldNode { get; }
    public JtNodeViewModel? NewNode { get; }
}