using Newtonsoft.Json.Linq;

namespace Aadev.JTF.Editor.EditorItems;

internal interface IJsonItem
{
    JToken Value { get; }
}
