using System.Diagnostics.CodeAnalysis;
using Aadev.JTF.Nodes;
using Newtonsoft.Json.Linq;

namespace Aadev.JTF.Editor.ViewModels;
public abstract class JtContainerViewModel : JtNodeViewModel, IJtNodeParentViewModel
{
    private bool expanded;
    internal event JtNodeViewModelEventHandler? ExpandChanged;


    public bool Expanded
    {
        get => expanded;
        set
        {
            if (expanded == value)
                return;
            if (AlwaysExpanded)
            {
                if (expanded)
                    return;
                expanded = true;
            }
            else
            {
                expanded = value;
            }

            ExpandChanged?.Invoke(this);
        }
    }
    public bool AlwaysExpanded => Node.DisableCollapse || Node.IsRootChild;

    public new JtContainerNode Node => (JtContainerNode)base.Node;
    public JContainer? ValidValue
    {
        get
        {
            if (Value.Type != Node.JsonType)
                return null;
            return Value as JContainer;
        }
    }
    [MemberNotNullWhen(false, nameof(ValidValue))] public new bool IsInvalidValueType => base.IsInvalidValueType;
    public override bool IsSavable => base.IsSavable || (!IsInvalidValueType && ValidValue.Count > 0);
    private protected JtContainerViewModel(JtContainerNode node, JToken? value, EventManagerContext eventManagerContext, IJtNodeParentViewModel parent) : base(node, value, eventManagerContext, parent) { }
}
