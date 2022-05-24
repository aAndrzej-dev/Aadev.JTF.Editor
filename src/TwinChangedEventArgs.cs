using System;

namespace Aadev.JTF.Editor
{
    internal class TwinChangedEventArgs : EventArgs
    {
        public JtNode? NewTwinNode { get; set; }
    }
}