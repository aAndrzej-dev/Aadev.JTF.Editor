using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Aadev.ConditionsInterpreter;
using Aadev.JTF.Nodes;
using Newtonsoft.Json.Linq;

namespace Aadev.JTF.Editor.ViewModels;
[DebuggerDisplay("\"{Node.Name, nq}\" : {Node.Type, nq}")]
public abstract class JtNodeViewModel
{
    internal EventManagerContext eventManagerContext;
    private EventManager? eventManager;
    private JToken value;
    private string? dynamicName;
    private int arrayIndex = -1;
    private bool isConditionMet;

    public event JtNodeViewModelEventHandler<ValueChangedEventArgs>? ValueChanged;
    internal event JtNodeViewModelEventHandler? DynamicNamePreviewChange;
    internal event JtNodeViewModelEventHandler? ConditionMetChanged;

    public JtRootViewModel Root { get; }
    public JtNode Node { get; }
    public virtual JToken Value
    {
        get => value;
        set
        {
            JToken oldValue = this.value;
            this.value = value;
            OnValueChanged(new ValueChangedEventArgs(new JtfEditorAction(JtfEditorAction.JtEditorActionType.ChangeValue, oldValue, value, this), true));
        }
    }
    public string ToolTipText { get; }
    public IJtNodeParentViewModel Parent { get; }


    internal EventManager EventManager => eventManager ??= eventManagerContext.GetOrCreate(Node.IdentifiersManager);
    public virtual bool IsInvalidValueType => Value.Type != Node.JsonType;

    public virtual bool IsSavable => Node.Required || Node.Parent?.Owner is { ContainerDisplayType: JtContainerType.Block, ContainerJsonType: JtContainerType.Array } || Node.IsRootChild || Node.IsArrayPrefab;


    public string? DynamicName { get => dynamicName; set { dynamicName = value; OnValueChanged(new ValueChangedEventArgs(new JtfEditorAction(JtfEditorAction.JtEditorActionType.DynamicNameChanged, null, dynamicName, this), true)); } }
    internal int ArrayIndex
    {
        get => arrayIndex; set
        {
            if (arrayIndex == value)
            {
                return;
            }

            arrayIndex = value;
            FriendlyDisplayName = CreateFriendlyName();
        }
    }

    public string FriendlyDisplayName { get; private set; }

    public bool IsConditionMet { get => isConditionMet; private set { if (value == isConditionMet) { return; } isConditionMet = value; TwinFamily?.UpdateSelectedNode(); ConditionMetChanged?.Invoke(this); } }

    public JtTwinFamilyViewModel? TwinFamily { get; private set; }


    public bool IsSelectedTwin => TwinFamily is null || TwinFamily.SelectedNode == this;

    private protected JtNodeViewModel(JtNode node, JToken? value, EventManagerContext eventManagerContext, IJtNodeParentViewModel parent)
    {
        Node = node;
        Parent = parent;
        Root = parent.Root;
        this.value = value is null || value.Type is JTokenType.Null ? Node.CreateDefaultValue() : value;
        this.eventManagerContext = eventManagerContext;

        ToolTipText = CreateToolTipText();
        FriendlyDisplayName = CreateFriendlyName();


        if (!Node.Id.IsEmpty)
        {
            EventManager.GetEvent(Node.Id)?.Invoke(Value);
        }



        if (Node.Condition is not null)
        {
            Dictionary<string, ChangedEvent> vars = new Dictionary<string, ChangedEvent>();


            ConditionInterpreter? interpreter = new ConditionInterpreter(x =>
            {
                string? id = x.ToLowerInvariant();
                if (vars.TryGetValue(id, out ChangedEvent? ce))
                {
                    return ce.Value ?? JValue.CreateNull();
                }

                ChangedEvent? e = EventManager.GetEvent(id);
                if (e is null)
                {
                    return JValue.CreateNull();
                }

                vars.Add(id, e);
                return e?.Value ?? JValue.CreateNull();
            }, Node.Condition);

            IsConditionMet = interpreter.ResolveCondition();
            foreach (KeyValuePair<string, ChangedEvent> ce in vars)
            {
                ce.Value.Event += (sender, e) =>
                {
                    IsConditionMet = interpreter.ResolveCondition();
                };
            }
        }
        else
        {
            IsConditionMet = true;
        }
    }


    
    private string CreateFriendlyName()
    {
        if (ArrayIndex != -1)
        {
            return ArrayIndex.ToString(CultureInfo.InvariantCulture);
        }
        else if (!string.IsNullOrEmpty(Node.DisplayName))
        {
            return ConvertToFriendlyName(Node.DisplayName);
        }

        return string.Empty;
    }

    protected virtual string CreateToolTipText()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"{Node.Name}");
        if (!Node.Id.IsEmpty)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"Id: {Node.Id}");
        }

        if (Node.Description is not null)
        {
            sb.AppendLine(Node.Description);
        }

        return sb.ToString();
    }
    public static JtNodeViewModel Create(JtNode node, JToken? value, EventManagerContext eventManagerContext, IJtNodeParentViewModel parent)
    {
        return node switch
        {
            JtArrayNode arrayNode => new JtArrayViewModel(arrayNode, value, eventManagerContext, parent),
            JtBlockNode blockNode => new JtBlockViewModel(blockNode, value, eventManagerContext, parent),
            JtBoolNode boolNode => new JtBoolViewModel(boolNode, value, eventManagerContext, parent),
            JtValueNode valueNode => new JtValueViewModel(valueNode, value, eventManagerContext, parent),
            _ => new JtUnknownViewModel(node, value, eventManagerContext, parent),
        };
    }

    internal void OnValueChanged(JtfEditorAction action) => OnValueChanged(new ValueChangedEventArgs(action, true));
    internal void OnValueChanged(ValueChangedEventArgs eventArgs)
    {
        ValueChanged?.Invoke(this, eventArgs); 
        if(!Node.Id.IsEmpty)
            EventManager.GetEvent(Node.Id)?.Invoke(Value);
    }
    

    public void EnsureValue()
    {
        if (IsInvalidValueType)
        {
            Value = Node.CreateDefaultValue();
        }
    }
    public void CreateValue() => Value = Node.CreateDefaultValue();

    private static string ConvertToFriendlyName(string name)
    {
        return string.Create(name.Length, name, new System.Buffers.SpanAction<char, string>((span, n) =>
        {
            span[0] = char.ToUpper(n[0], CultureInfo.CurrentCulture);
            for (int i = 1; i < name.Length; i++)
            {
                if (name[i] is '_')
                {
                    span[i] = ' ';
                    if (name.Length <= i + 1)
                    {
                        continue;
                    }

                    i++;
                    span[i] = char.ToUpper(name[i], CultureInfo.CurrentCulture);
                    continue;
                }

                span[i] = name[i];
            }
        }));
    }

    internal void SetTwinFamily(JtTwinFamilyViewModel? twinFamily) => TwinFamily = twinFamily;
    internal void OnDynamicNamePreviewChange()
    {
        DynamicNamePreviewChange?.Invoke(this);
    }
}
