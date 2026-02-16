using Aadev.JTF.Nodes;
using Newtonsoft.Json.Linq;

namespace Aadev.JTF.Editor.ViewModels;
public sealed class JtBoolViewModel : JtNodeViewModel
{
    public JValue? ValidValue => Value as JValue;
    public override JToken Value
    {
        get => base.Value;
        set
        {
            if (!JToken.DeepEquals(base.Value, value))
            {
                base.Value = value;
            }
        }
    }
    public override bool IsSavable => base.IsSavable || (Value.Type != JTokenType.Null && (bool?)ValidValue != Node.Default);
    public bool? RawValue
    {
        get => Value.Type == Node.JsonType ? ((bool?)Value ?? Node.Default) : (Value.Type is JTokenType.Null ? Node.Default : null);
        set 
            {
            if (ValidValue is null)
            {
                Value = new JValue(value);
            }
            else if (!value.Equals(ValidValue.Value))
            {
                ValidValue.Value = value;
                OnValueChanged(new ValueChangedEventArgs(new JtfEditorAction(JtfEditorAction.JtEditorActionType.ChangeValue, null, ValidValue, this), false));
            }
        }
    }

    private new JtBoolNode Node => (JtBoolNode)base.Node;
    internal JtBoolViewModel(JtBoolNode node, JToken? value, EventManagerContext eventManagerContext, IJtNodeParentViewModel parent) : base(node, value, eventManagerContext, parent) { }
}
