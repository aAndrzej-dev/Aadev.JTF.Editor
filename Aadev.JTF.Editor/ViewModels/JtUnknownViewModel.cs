using Newtonsoft.Json.Linq;

namespace Aadev.JTF.Editor.ViewModels;
public sealed class JtUnknownViewModel : JtNodeViewModel
{
    public JtUnknownViewModel(JtNode node, JToken? value, EventManagerContext eventManagerContext, IJtNodeParentViewModel parent) : base(node, value, eventManagerContext, parent)
    {
    }

    public override bool IsInvalidValueType => false;
}
