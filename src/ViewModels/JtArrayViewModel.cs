using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Aadev.JTF.Nodes;
using Newtonsoft.Json.Linq;

namespace Aadev.JTF.Editor.ViewModels;
public sealed class JtArrayViewModel : JtContainerViewModel
{
    private List<JtNodeViewModel>? children;
    internal JtNode? SinglePrefab { get; set; }
    public new JtArrayNode Node => (JtArrayNode)base.Node;

    internal JtArrayViewModel(JtArrayNode node, JToken? value, EventManagerContext eventManagerContext, IJtNodeParentViewModel parent) : base(node, value, eventManagerContext, parent)
    {
        if (Node.SingleType && ValidValue?.Count > 0)
        {
            JTokenType? jtype = ValidValue[0]?.Type;
            SinglePrefab = Node.Prefabs.Nodes!.Where(x => x.JsonType == jtype).FirstOrDefault();
        }
    }

    public static bool CheckPrefab(JtNode prefab, JToken value)
    {
        if (prefab.JsonType != value.Type)
        {
            return false;
        }

        if (prefab is JtBlockNode b && b.JsonType is JTokenType.Object)
        {
            foreach (JProperty item in ((JObject)value).Properties())
            {
                if (!b.Children.Nodes!.Any(x => x.Name == item.Name))
                {
                    return false;
                }
            }
        }

        return true;

    }
    public List<JtNodeViewModel> GetChildren()
    {
        if (children is null)
            InitializeChildren();
        return children;
    }
    [MemberNotNull(nameof(children))]
    private void InitializeChildren()
    {
        children = new List<JtNodeViewModel>();

        if (Node.ContainerJsonType is JtContainerType.Array)
        {
            if (Value is not JArray jArray)
            {
                return;
            }

            for (int i = 0; i < jArray.Count; i++)
            {
                CreateArrayViewModel(Node.Prefabs.Nodes!.FirstOrDefault(x => CheckPrefab(x, jArray[i])) ?? Node.Prefabs.Nodes![0], jArray[i], i);
            }
        }
        else
        {
            if (Value is not JObject jObject)
            {
                return;
            }

            foreach (JProperty item in jObject.Properties())
            {
                CreateBlockViewModel(Node.Prefabs.Nodes!.FirstOrDefault(x => x.JsonType == item.Value.Type) ?? Node.Prefabs.Nodes![0], item);
            }
        }
    }

    internal void RemoveChild(JtNodeViewModel viewModel)
    {
        EnsureValue();
        JToken? oldValue;
        if (Node.MakeAsObject)
        {
            if (viewModel.DynamicName is not null)
            {
                oldValue = ((JObject)Value)[viewModel.DynamicName];
                ((JObject)Value)!.Remove(viewModel.DynamicName);
            }
            else
                oldValue = null;
        }
        else
        {
            oldValue = ((JArray)Value)[viewModel.ArrayIndex];
            ((JArray)Value)!.RemoveAt(viewModel.ArrayIndex);
        }

        if (ValidValue!.Count == 0)
            SinglePrefab = null;
        OnValueChanged(new JtfEditorAction(JtfEditorAction.JtEditorActionType.RemoveToken, oldValue, null, this));
    }

    public JtNodeViewModel AddNewItem(JtNode prefab)
    {
        EnsureValue();

        JtNodeViewModel vm;


        if (Node.ContainerJsonType is JtContainerType.Array)
        {
            vm = CreateArrayViewModel(prefab, null, ValidValue!.Count);
            ((JArray)Value).Add(vm.Value);
        }
        else
        {
            vm = CreateBlockViewModel(prefab, null);
        }

        OnValueChanged(new JtfEditorAction(JtfEditorAction.JtEditorActionType.AddToken, null, vm.Value, this));

        return vm;
    }

    public JtNodeViewModel CreateArrayViewModel(JtNode prefab, JToken? value, int index)
    {
        JtNodeViewModel vm = CreateCommonChildViewModel(prefab, value);

        vm.ArrayIndex = index;

        vm.ValueChanged += (vm, e) =>
        {
            if (Value is not JArray array)
                return;

            int ind = vm.ArrayIndex;

            JToken value = vm.Value;
            if (value.Type is JTokenType.Null)
                value = vm.Node.CreateDefaultValue();

            if (array.Count <= ind)
            {
                while (array.Count < ind)
                {
                    array.Add(JValue.CreateNull());
                }

                array.Add(value);
            }
            else
            {
                array[ind] = value;
            }

            OnValueChanged(e);
        };
        return vm;
    }

    public JtNodeViewModel CreateBlockViewModel(JtNode prefab, JProperty? property)
    {
        JtNodeViewModel vm = CreateCommonChildViewModel(prefab, property?.Value);
        string newDynamicName = string.Empty;

        if (property is null)
        {
            newDynamicName = $"new {Node.Name} item";
            if (children!.Any(x => x.DynamicName?.Equals(newDynamicName) is true))
            {
                int i = 1;
                while (children!.Any(x => x.DynamicName?.Equals($"new {Node.Name} item {i}") is true))
                {
                    i++;
                }

                newDynamicName = $"new {Node.Name} item {i}";
            }

            vm.DynamicName = newDynamicName;
        }
        else
        {
            vm.DynamicName = property.Name;
        }

        vm.ValueChanged += (vm, e) =>
        {
            if (Value is not JObject obj)
                return;

            if (e.Action?.Type is JtfEditorAction.JtEditorActionType.DynamicNameChanged && children!.Any(x => x.DynamicName == vm.DynamicName && x != vm))
            {
                vm.DynamicName = (string?)e.Action.OldValue; //TODO: OnError ??
                MessageBox.Show(string.Format(CultureInfo.CurrentCulture, Properties.Resources.ArrayObjectNameExist, vm.DynamicName), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (e.Action?.Type is JtfEditorAction.JtEditorActionType.DynamicNameChanged && e.Action.OldValue is JValue v && v.Value is string)
            {
                obj.Remove((string)e.Action.OldValue!);
            }

            JToken value = vm.Value;
            if (value.Type is JTokenType.Null)
                value = vm.Node.CreateDefaultValue();
            obj[vm.DynamicName!] = value;

            OnValueChanged(new ValueChangedEventArgs(e.Action, true));
        };

        vm.DynamicNamePreviewChange += s => OnValueChanged(new JtfEditorAction(JtfEditorAction.JtEditorActionType.None, null, null, this));


        JToken value = vm.Value;
        if (value.Type is JTokenType.Null)
            value = vm.Node.CreateDefaultValue();
        if (!JToken.DeepEquals(Value[vm.DynamicName], value))
        {
            JToken? oldValue = Value[vm.DynamicName];

            Value[vm.DynamicName] = value;
            OnValueChanged(new JtfEditorAction(JtfEditorAction.JtEditorActionType.ChangeValue, oldValue, Value[vm.DynamicName], this));
        }


        return vm;
    }

    private JtNodeViewModel CreateCommonChildViewModel(JtNode prefab, JToken? value)
    {
        if (children is null)
            InitializeChildren();
        JtNodeViewModel vm = Create(prefab, value, new EventManagerContext(eventManagerContext), this);

        children.Add(vm);

        return vm;
    }
}
