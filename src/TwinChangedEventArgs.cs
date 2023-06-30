using System;

namespace Aadev.JTF.Editor;

internal class TwinChangedEventArgs : EventArgs
{
    public JtNode NewTwinNode { get; set; }

    public TwinChangedEventArgs(JtNode newTwinNode)
    {
        NewTwinNode = newTwinNode;
    }
}