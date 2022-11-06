using Newtonsoft.Json.Linq;

namespace Aadev.JTF.Editor.EditorItems
{
    internal interface IJsonItem
    {
        public JToken Value { get; }

        public string Path { get; }
    }
}
