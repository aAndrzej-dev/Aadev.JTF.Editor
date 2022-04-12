using System;

namespace Aadev.JTF.Editor
{

    internal class TwinChangedEventArgs : EventArgs
    {
        public JtToken? NewTwinType { get; set; }
    }

}