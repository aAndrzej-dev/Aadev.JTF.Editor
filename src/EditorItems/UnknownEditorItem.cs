using Newtonsoft.Json.Linq;

namespace Aadev.JTF.Editor.EditorItems;

internal class UnknownEditorItem : EditorItem
{
    internal override bool IsInvalidValueType => false;
    public UnknownEditorItem(JtNode node, JToken? token, JsonJtfEditor rootEditor, EventManagerContext eventManagerContext) : base(node, token, rootEditor, eventManagerContext)
    {
        Value ??= node.CreateDefaultValue();
    }

    public override JToken Value { get; set; }
}
