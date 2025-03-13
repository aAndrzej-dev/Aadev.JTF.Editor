using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Windows.Forms;
using Aadev.JTF.Nodes;
using Newtonsoft.Json.Linq;

namespace Aadev.JTF.Editor.ViewModels;
public sealed class JtValueViewModel : JtNodeViewModel
{
    private IntelligentSuggestionKey? iSuggestionKey = null;
    private bool isISuggestionKeyChecked;

    public new JtValueNode Node => (JtValueNode)base.Node;

    public override bool IsSavable => base.IsSavable || (Value.Type != JTokenType.Null && !IsEqualToDefaultValue());
    public override JToken Value
    {
        get => base.Value;
        set
        {
            if (JToken.DeepEquals(base.Value, value))
                return;

            CheckISuggestionKey();
            if (iSuggestionKey is not null)
            {
                for (int i = 0; i < iSuggestionKey.suggestions.Count; i++)
                {
                    if (iSuggestionKey.suggestions[i].source == Value)
                    {
                        iSuggestionKey.suggestions.RemoveAt(i);
                    }
                }


                if (value as JValue is null)
                    return;

                iSuggestionKey.suggestions.Add(new IntelligentSuggestion((JValue)value, iSuggestionKey));
            }

            base.Value = value;
        }
    }
    public bool InvalidValue
    {
        get
        {
            if (Value.Type == JTokenType.Null)
                return false;
            if (IsInvalidValueType)
                return false;
            if (IsEqualToDefaultValue())
                return false;

            if (Node.TryGetSuggestions() is null || Node.Suggestions.IsEmpty)
                return false;
            foreach (IJtSuggestion item in Node.Suggestions.GetSuggestions(GetDynamicSuggestions))
            {
                if (SuggestionEqualJValue(item, ValidValue))
                    return false;
            }

            return true;
        }
    }
    public JValue? ValidValue => Value as JValue;

    [MemberNotNullWhen(false, nameof(ValidValue))] public new bool IsInvalidValueType => base.IsInvalidValueType;

    public bool IsUsingSuggestionSelector(int suggestionsCount) => Root.IsReadOnly || Node.SuggestionsDisplayType is JtSuggestionsDisplayType.Window || (Node.SuggestionsDisplayType is JtSuggestionsDisplayType.Auto && suggestionsCount > Root.MaximumSuggestionCountForComboBox);

    public JtValueViewModel(JtValueNode node, JToken? value, EventManagerContext eventManagerContext, IJtNodeParentViewModel parent) : base(node, value, eventManagerContext, parent)
    {
        CheckISuggestionKey();
    }

    private void CheckISuggestionKey()
    {
        if (isISuggestionKeyChecked)
            return;
        isISuggestionKeyChecked = true;
        if (Node.TryGetSuggestions()?.DynamicSourceId.Identifier.Value?.StartsWith("jtf_auto:") is true)
        {
            ReadOnlySpan<char> key = Node.Suggestions.DynamicSourceId.Identifier.Value.AsSpan(9);
            iSuggestionKey = Root.IntelligentSuggestionsProvider.GetKeyOrCreate(key, Node.Suggestions.SuggestionType);
        }
    }
    public IEnumerable<IJtSuggestion> GetDynamicSuggestions(JtIdentifier id)
    {
        if (id.Value?.StartsWith("jtf:", StringComparison.OrdinalIgnoreCase) is true && Node is JtStringNode)
        {
            ReadOnlySpan<char> nodeId = id.Value.AsSpan(4);

            ChangedEvent? ce = EventManager.GetEvent(nodeId.ToString());
            if (ce?.Value is not JObject obj)
                return Enumerable.Empty<IJtSuggestion>();

            return CreateSuggestionsFromObject(obj);
        }

        CheckISuggestionKey();
        if (iSuggestionKey is not null)
        {
            return iSuggestionKey.suggestions?.DistinctBy(x => x.StringValue) ?? Enumerable.Empty<IJtSuggestion>();
        }
        else
        {
            return Root.DynamicSuggestionsSource?.Invoke(id) ?? Enumerable.Empty<IJtSuggestion>();
        }

        static IEnumerable<IJtSuggestion> CreateSuggestionsFromObject(JObject obj)
        {
            foreach (JProperty item in obj.Properties())
            {
                yield return new JtSuggestion<string>(item.Name);
            }
        }
    }
    public bool IsEqualToDefaultValue()
    {
        if (Value.Type != Node.JsonType)
            return false;



        return Node switch
        {
            JtByteNode jtByte => jtByte.Default.Equals((byte)Value),
            JtShortNode jtShort => jtShort.Default.Equals((short)Value),
            JtIntNode jtInt => jtInt.Default.Equals((int)Value),
            JtLongNode jtLong => jtLong.Default.Equals((long)Value),
            JtFloatNode jtFloat => jtFloat.Default.Equals((float)Value),
            JtDoubleNode jtDouble => jtDouble.Default.Equals((double)Value),
            JtStringNode jtString => jtString.Default.Equals((string?)Value, StringComparison.Ordinal),
            _ => throw new Exception(),
        };
    }
    public bool SuggestionEqualJValue(IJtSuggestion suggestion, JValue value)
    {
        if (value.Type != Node.JsonType)
            return false;
        return Node switch
        {
            JtByteNode _ => suggestion.GetValue<byte>().Equals((byte)value),
            JtShortNode _ => suggestion.GetValue<short>().Equals((short)value),
            JtIntNode _ => suggestion.GetValue<int>().Equals((int)value),
            JtLongNode _ => suggestion.GetValue<long>().Equals((long)value),
            JtFloatNode _ => suggestion.GetValue<float>().Equals((float)value),
            JtDoubleNode _ => suggestion.GetValue<double>().Equals((double)value),
            JtStringNode _ => suggestion.GetValue<string>().Equals((string?)value, StringComparison.Ordinal),
            _ => throw new Exception(),
        };
    }

    public IJtSuggestion? GetValueAsSuggestion(IJtSuggestion[] suggestions)
    {
        if (ValidValue is null)
            return null;
        IJtSuggestion? currentSuggestion = suggestions.Where(x => SuggestionEqualJValue(x, ValidValue)).FirstOrDefault();
        if (currentSuggestion is null && !Node.ForceUsingSuggestions)
        {
            switch (Node)
            {
                case JtByteNode:
                    currentSuggestion = new DynamicSuggestion<byte>((byte)ValidValue);
                    break;
                case JtShortNode:
                    currentSuggestion = new DynamicSuggestion<short>((short)ValidValue);
                    break;
                case JtIntNode:
                    currentSuggestion = new DynamicSuggestion<int>((int)ValidValue);
                    break;
                case JtLongNode:
                    currentSuggestion = new DynamicSuggestion<long>((long)ValidValue);
                    break;
                case JtFloatNode:
                    currentSuggestion = new DynamicSuggestion<float>((float)ValidValue);
                    break;
                case JtDoubleNode:
                    currentSuggestion = new DynamicSuggestion<double>((double)ValidValue);
                    break;
                case JtStringNode:
                        currentSuggestion = new DynamicSuggestion<string>((string?)ValidValue ?? string.Empty);
                    break;
                default:
                    break;
            }
        }

        return currentSuggestion;
    }
    public void ShowSuggestionSelector(IJtSuggestion[] suggestions)
    {
        Root.EnsureSuggestionSelector();
        DialogResult dr = Root.SuggestionSelector.Show(suggestions, Node.ForceUsingSuggestions || Root.IsReadOnly, GetValueAsSuggestion(suggestions));

        if (dr == DialogResult.OK && !Root.IsReadOnly)
        {
            Value = new JValue(Root.SuggestionSelector.SelectedSuggestion!.GetValue());
        }
    }

    public void ParseValue(string value)
    {
        switch (Node)
        {
            case JtByteNode jtByte:
                {
                    if (BigInteger.TryParse(value, out BigInteger b))
                    {
                        SetValue((byte)BigInteger.Min(jtByte.Max, BigInteger.Max(jtByte.Min, b)));
                    }

                    return;
                }
            case JtShortNode jtShort:
                {
                    if (BigInteger.TryParse(value, out BigInteger b))
                    {
                        SetValue((short)BigInteger.Min(jtShort.Max, BigInteger.Max(jtShort.Min, b)));
                    }

                    return;
                }
            case JtIntNode jtInt:
                {
                    if (BigInteger.TryParse(value, out BigInteger b))
                    {
                        SetValue((int)BigInteger.Min(jtInt.Max, BigInteger.Max(jtInt.Min, b)));
                    }

                    return;
                }
            case JtLongNode jtLong:
                {
                    if (BigInteger.TryParse(value, out BigInteger b))
                    {
                        SetValue((long)BigInteger.Min(jtLong.Max, BigInteger.Max(jtLong.Min, b)));
                    }

                    return;
                }
            case JtFloatNode jtFloat:
                {
                    if (float.TryParse(value, out float b))
                        SetValue(MathF.Min(jtFloat.Max, MathF.Max(jtFloat.Min, b)));
                    return;
                }
            case JtDoubleNode jtDouble:
                {
                    if (double.TryParse(value, out double b))
                        SetValue(Math.Min(jtDouble.Max, Math.Max(jtDouble.Min, b)));
                    return;
                }
            case JtStringNode:
                SetValue(value);
                return;
            default:
                throw new Exception();
        }
    }

    public void SetValue(object? newValue)
    {
        if (ValidValue is null)
        {
            Value = new JValue(newValue);
        }
        else
        {
            CheckISuggestionKey();
            if (iSuggestionKey is not null)
            {
                for (int i = 0; i < iSuggestionKey.suggestions.Count; i++)
                {
                    if (iSuggestionKey.suggestions[i].source == Value)
                    {
                        iSuggestionKey.suggestions.RemoveAt(i);
                    }
                }
            }

            ValidValue.Value = newValue;
            if (iSuggestionKey is not null)
            {
                if (ValidValue is not null)
                    iSuggestionKey.suggestions.Add(new IntelligentSuggestion(ValidValue, iSuggestionKey));
            }

            OnValueChanged(new ValueChangedEventArgs(new JtfEditorAction(JtfEditorAction.JtEditorActionType.ChangeValue, null, ValidValue, this), false));
        }
    }


    protected override string CreateToolTipText()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine(CultureInfo.InvariantCulture, $"{Node.Name}");
        if (!Node.Id.IsEmpty)
            sb.AppendLine(CultureInfo.InvariantCulture, $"Id: {Node.Id}");
        if (Node.Description is not null)
            sb.AppendLine(Node.Description);

        switch (Node)
        {
            case JtByteNode n:
                if (Root.ShowAdvancedToolTip || n.Min != byte.MinValue)
                    sb.AppendLine($"Min: {n.Min}");
                if (Root.ShowAdvancedToolTip || n.Max != byte.MaxValue)
                    sb.AppendLine($"Max: {n.Max}");
                if (Root.ShowAdvancedToolTip || n.Default != 0)
                    sb.AppendLine($"Default: {n.Default}");
                break;
            case JtShortNode n:
                if (Root.ShowAdvancedToolTip || n.Min != short.MinValue)
                    sb.AppendLine($"Min: {n.Min}");
                if (Root.ShowAdvancedToolTip || n.Max != short.MaxValue)
                    sb.AppendLine($"Max: {n.Max}");
                if (Root.ShowAdvancedToolTip || n.Default != 0)
                    sb.AppendLine($"Default: {n.Default}");
                break;
            case JtIntNode n:
                if (Root.ShowAdvancedToolTip || n.Min != int.MinValue)
                    sb.AppendLine($"Min: {n.Min}");
                if (Root.ShowAdvancedToolTip || n.Max != int.MaxValue)
                    sb.AppendLine($"Max: {n.Max}");
                if (Root.ShowAdvancedToolTip || n.Default != 0)
                    sb.AppendLine($"Default: {n.Default}");
                break;
            case JtLongNode n:
                if (Root.ShowAdvancedToolTip || n.Min != long.MinValue)
                    sb.AppendLine($"Min: {n.Min}");
                if (Root.ShowAdvancedToolTip || n.Max != long.MaxValue)
                    sb.AppendLine($"Max: {n.Max}");
                if (Root.ShowAdvancedToolTip || n.Default != 0)
                    sb.AppendLine($"Default: {n.Default}");
                break;
            case JtFloatNode n:
                if (Root.ShowAdvancedToolTip || n.Min != float.MinValue)
                    sb.AppendLine($"Min: {n.Min}");
                if (Root.ShowAdvancedToolTip || n.Max != float.MaxValue)
                    sb.AppendLine($"Max: {n.Max}");
                if (Root.ShowAdvancedToolTip || n.Default != 0)
                    sb.AppendLine($"Default: {n.Default}");
                break;
            case JtDoubleNode n:
                if (Root.ShowAdvancedToolTip || n.Min != double.MinValue)
                    sb.AppendLine($"Min: {n.Min}");
                if (Root.ShowAdvancedToolTip || n.Max != double.MaxValue)
                    sb.AppendLine($"Max: {n.Max}");
                if (Root.ShowAdvancedToolTip || n.Default != 0)
                    sb.AppendLine($"Default: {n.Default}");
                break;
            case JtStringNode n:
                if (Root.ShowAdvancedToolTip || n.MaxLength != -1)
                    sb.AppendLine($"Max Length: {n.MaxLength}");
                if (Root.ShowAdvancedToolTip || n.MinLength != 0)
                    sb.AppendLine($"Min Length: {n.MinLength}");
                break;
            default:
                throw new Exception();
        }

        return sb.ToString();
    }
}
