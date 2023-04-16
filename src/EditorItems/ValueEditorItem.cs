using Aadev.JTF.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Windows.Forms;

namespace Aadev.JTF.Editor.EditorItems
{
    internal sealed class ValueEditorItem : EditorItem
    {
        private Control? valueBox;
        private JToken value;
        private Rectangle textBoxBounds = Rectangle.Empty;
        private Rectangle discardInvalidValueButtonBounds = Rectangle.Empty;
        private Rectangle restoreDefaultValueButtonBounds = Rectangle.Empty;

        private new JtValueNode Node => (JtValueNode)base.Node;
        private bool InvalidValue
        {
            get
            {
                if (value.Type == JTokenType.Null)
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

        protected override bool IsFocused => base.IsFocused || valueBox?.Focused is true;
        internal override bool IsSavable => base.IsSavable || (Value.Type != JTokenType.Null && !IsEqualToDefaultValue());
        protected override Color BorderColor
        {
            get
            {
                if (InvalidValue)
                {
                    if (Node.ForceUsingSuggestions)
                        return RootEditor.ColorTable.InvalidBorderColor;
                    else
                        return RootEditor.ColorTable.WarningBorderColor;
                }
                return base.BorderColor;
            }

        }

        public override JToken Value
        {
            get => value;
            set
            {
                if (!JToken.DeepEquals(this.value, value))
                {
                    JToken oldValue = this.value;
                    this.value = value;
                    Invalidate();
                    OnValueChanged(new JtfEditorAction(JtfEditorAction.JtEditorActionType.ChangeValue, oldValue, value, this));
                }
            }
        }
        public JValue? ValidValue => Value as JValue;

        [MemberNotNullWhen(false, nameof(ValidValue))] public new bool IsInvalidValueType => base.IsInvalidValueType;
        internal ValueEditorItem(JtValueNode type, JToken? token, JsonJtfEditor jsonJtfEditor, EventManager eventManager) : base(type, token, jsonJtfEditor, eventManager)
        {
            value ??= Node.CreateDefaultValue();

            StringBuilder sb = new StringBuilder(toolTipText);

            switch (Node)
            {
                case JtByteNode n:
                    if (RootEditor.ShowAdvancedToolTip || n.Min != byte.MinValue)
                        sb.AppendLine($"Min: {n.Min}");
                    if (RootEditor.ShowAdvancedToolTip || n.Max != byte.MaxValue)
                        sb.AppendLine($"Max: {n.Max}");
                    if (RootEditor.ShowAdvancedToolTip || n.Default != 0)
                        sb.AppendLine($"Default: {n.Default}");
                    break;
                case JtShortNode n:
                    if (RootEditor.ShowAdvancedToolTip || n.Min != short.MinValue)
                        sb.AppendLine($"Min: {n.Min}");
                    if (RootEditor.ShowAdvancedToolTip || n.Max != short.MaxValue)
                        sb.AppendLine($"Max: {n.Max}");
                    if (RootEditor.ShowAdvancedToolTip || n.Default != 0)
                        sb.AppendLine($"Default: {n.Default}");
                    break;
                case JtIntNode n:
                    if (RootEditor.ShowAdvancedToolTip || n.Min != int.MinValue)
                        sb.AppendLine($"Min: {n.Min}");
                    if (RootEditor.ShowAdvancedToolTip || n.Max != int.MaxValue)
                        sb.AppendLine($"Max: {n.Max}");
                    if (RootEditor.ShowAdvancedToolTip || n.Default != 0)
                        sb.AppendLine($"Default: {n.Default}");
                    break;
                case JtLongNode n:
                    if (RootEditor.ShowAdvancedToolTip || n.Min != long.MinValue)
                        sb.AppendLine($"Min: {n.Min}");
                    if (RootEditor.ShowAdvancedToolTip || n.Max != long.MaxValue)
                        sb.AppendLine($"Max: {n.Max}");
                    if (RootEditor.ShowAdvancedToolTip || n.Default != 0)
                        sb.AppendLine($"Default: {n.Default}");
                    break;
                case JtFloatNode n:
                    if (RootEditor.ShowAdvancedToolTip || n.Min != float.MinValue)
                        sb.AppendLine($"Min: {n.Min}");
                    if (RootEditor.ShowAdvancedToolTip || n.Max != float.MaxValue)
                        sb.AppendLine($"Max: {n.Max}");
                    if (RootEditor.ShowAdvancedToolTip || n.Default != 0)
                        sb.AppendLine($"Default: {n.Default}");
                    break;
                case JtDoubleNode n:
                    if (RootEditor.ShowAdvancedToolTip || n.Min != double.MinValue)
                        sb.AppendLine($"Min: {n.Min}");
                    if (RootEditor.ShowAdvancedToolTip || n.Max != double.MaxValue)
                        sb.AppendLine($"Max: {n.Max}");
                    if (RootEditor.ShowAdvancedToolTip || n.Default != 0)
                        sb.AppendLine($"Default: {n.Default}");
                    break;
                case JtStringNode n:
                    if(RootEditor.ShowAdvancedToolTip || n.MaxLength != -1)
                        sb.AppendLine($"Max Length: {n.MaxLength}");
                    if(RootEditor.ShowAdvancedToolTip || n.MinLength != 0)
                        sb.AppendLine($"Min Length: {n.MinLength}");
                    break;
                default: 
                    throw new Exception();
            }

            toolTipText =sb.ToString();
        }

        private bool SuggestionEqualJValue(IJtSuggestion suggestion, JValue value)
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
        private bool IsEqualToDefaultValue()
        {
            if (value.Type != Node.JsonType)
                return false;
            return Node switch
            {
                JtByteNode jtByte => jtByte.Default.Equals((byte)value),
                JtShortNode jtShort => jtShort.Default.Equals((short)value),
                JtIntNode jtInt => jtInt.Default.Equals((int)value),
                JtLongNode jtLong => jtLong.Default.Equals((long)value),
                JtFloatNode jtFloat => jtFloat.Default.Equals((float)value),
                JtDoubleNode jtDouble => jtDouble.Default.Equals((double)value),
                JtStringNode jtString => jtString.Default.Equals((string?)value, StringComparison.Ordinal),
                _ => throw new Exception(),
            };
        }
        private void CreateTextBox(bool doubleClick = false)
        {
            if (IsInvalidValueType)
                return;
            if (textBoxBounds == Rectangle.Empty)
                return;
            if (valueBox is not null)
                return;
            if (Parent is EditorItem parent && parent.SuspendFocus)
                return;

            if (Node.TryGetSuggestions() is null || Node.Suggestions.IsEmpty)
            {
                TextBox textBox = new TextBox
                {
                    Font = Font,
                    BorderStyle = BorderStyle.None,
                    BackColor = RootEditor.ColorTable.TextBoxBackColor,
                    ForeColor = RootEditor.ColorTable.TextBoxForeColor,
                    AutoSize = false,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    Width = Width - textBoxBounds.X - 20 - xRightOffset - 30,

                    Text = Value.ToString(),
                    ReadOnly = RootEditor.ReadOnly
                };
                textBox.Location = new Point(textBoxBounds.X + 10, 16 - textBox.Height / 2);


                if (Node is JtStringNode strNode)
                {
                    if (strNode.MaxLength > 0)
                        textBox.MaxLength = strNode.MaxLength;
                    textBox.TextChanged += (sender, eventArgs) =>
                    {
                        Value = textBox.Text;
                    };
                    textBox.LostFocus += (sender, eventArgs) =>
                    {
                        Controls.Remove(textBox);
                        valueBox = null;
                        Invalidate();
                    };
                }
                else
                {
                    textBox.KeyPress += (sender, e) =>
                    {
                        if (char.IsDigit(e.KeyChar) || char.IsControl(e.KeyChar))
                            e.Handled = false;
                        else if (e.KeyChar is '-' && !textBox.Text.Contains('-', StringComparison.Ordinal))
                            e.Handled = false;
                        else if (e.KeyChar == ',' && (Node is JtFloatNode or JtDoubleNode) && !textBox.Text.Contains(',', StringComparison.Ordinal))
                            e.Handled = false;
                        else if (e.KeyChar == 'e' && (Node is JtFloatNode or JtDoubleNode) && !textBox.Text.Contains('e', StringComparison.OrdinalIgnoreCase))
                            e.Handled = false;
                        else
                            e.Handled = true;
                    };
                    textBox.TextChanged += (sender, e) =>
                    {
                        if (string.IsNullOrEmpty(textBox.Text))
                        {
                            JToken oldValue = Value;
                            textBox.Text = Node.GetDefaultValue().ToString();
                            textBox.SelectAll();
                            ValidValue.Value = Node.GetDefaultValue();
                            Invalidate();
                            OnValueChanged(new JtfEditorAction(JtfEditorAction.JtEditorActionType.ChangeValue, oldValue, Value, this));
                            return;
                        }

                        if (Node is JtByteNode jtByte)
                        {
                            if (BigInteger.TryParse(textBox.Text, out BigInteger b))
                            {
                                Value = (byte)BigInteger.Min(jtByte.Max, BigInteger.Max(jtByte.Min, b));
                            }
                        }
                        else if (Node is JtShortNode jtShort)
                        {
                            if (BigInteger.TryParse(textBox.Text, out BigInteger b))
                            {
                                Value = (short)BigInteger.Min(jtShort.Max, BigInteger.Max(jtShort.Min, b));
                            }
                        }
                        else if (Node is JtIntNode jtInt)
                        {
                            if (BigInteger.TryParse(textBox.Text, out BigInteger b))
                            {
                                Value = (int)BigInteger.Min(jtInt.Max, BigInteger.Max(jtInt.Min, b));
                            }
                        }
                        else if (Node is JtLongNode jtLong)
                        {
                            if (BigInteger.TryParse(textBox.Text, out BigInteger b))
                            {
                                Value = (long)BigInteger.Min(jtLong.Max, BigInteger.Max(jtLong.Min, b));
                            }
                        }
                        else if (Node is JtFloatNode jtFloat)
                        {

                            if (float.TryParse(textBox.Text, out float b))
                                Value = MathF.Min(jtFloat.Max, MathF.Max(jtFloat.Min, b));
                        }
                        else if (Node is JtDoubleNode jtDouble)
                        {
                            if (double.TryParse(textBox.Text, out double b))
                                Value = Math.Min(jtDouble.Max, Math.Max(jtDouble.Min, b));
                        }
                    };
                    textBox.LostFocus += (sender, e) =>
                    {
                        if (textBox is null)
                            return;
                        Controls.Remove(textBox);
                        valueBox = null;
                        Invalidate();
                    };
                }




                Controls.Add(textBox);
                textBox?.Focus();
                textBox?.SelectAll();
                valueBox = textBox;

            }
            else
            {
                IJtSuggestion[] suggestions = Node.Suggestions.GetSuggestions(GetDynamicSuggestions).ToArray();

                if (RootEditor.ReadOnly || Node.SuggestionsDisplayType is JtSuggestionsDisplayType.Window || (Node.SuggestionsDisplayType is JtSuggestionsDisplayType.Auto && suggestions.Length > RootEditor.MaximumSuggestionCountForComboBox))
                {
                    if (!doubleClick)
                        return;

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
                    DialogResult dr = RootEditor.SuggestionSelector.Show(suggestions, Node.ForceUsingSuggestions || RootEditor.ReadOnly, currentSuggestion);

                    if (dr == DialogResult.OK && !RootEditor.ReadOnly)
                    {
                        Value = new JValue(RootEditor.SuggestionSelector.SelectedSuggestion!.GetValue());
                    }
                }
                else
                {
                    ComboBox comboBox = new ComboBox
                    {
                        Font = Font,
                        FlatStyle = FlatStyle.Flat,
                        BackColor = RootEditor.ColorTable.TextBoxBackColor,
                        ForeColor = RootEditor.ColorTable.TextBoxForeColor,
                        AutoSize = false,
                        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                        Width = Width - xOffset - 12 - xRightOffset,
                        Text = Value.ToString(),

                    };


                    comboBox.Location = new Point(xOffset + 10, 16 - comboBox.Height / 2 - 4);
                    if (Node.ForceUsingSuggestions)
                    {
                        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                    }
                    else
                    {
                        comboBox.DropDownStyle = ComboBoxStyle.DropDown;
                        comboBox.AutoCompleteMode = AutoCompleteMode.Suggest;
                        comboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
                    }


                    Controls.Add(comboBox);


                    comboBox.Items.AddRange(suggestions);

                    comboBox.SelectedItem = ValidValue.Value;

                    comboBox.Focus();
                    comboBox.DroppedDown = true;




                    comboBox.SelectedIndexChanged += (sender, eventArgs) =>
                    {
                        if (comboBox.SelectedItem is null)
                            return;
                        Value = new JValue(((IJtSuggestion)comboBox.SelectedItem).GetValue());
                    };


                    if (!Node.ForceUsingSuggestions)
                    {
                        comboBox.TextChanged += (sender, eventArgs) => Value = comboBox?.Text;
                    }

                    comboBox.LostFocus += (s, e) =>
                    {
                        Controls.Remove(comboBox);
                        valueBox = null;
                        Invalidate();
                    };
                    valueBox = comboBox;
                }
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (IsInvalidValueType)
                return;

            Graphics g = e.Graphics;

            if (InvalidValue && Node.ForceUsingSuggestions)
            {
                string message = string.Format(CultureInfo.CurrentCulture, Properties.Resources.InvalidValue, value.ToString());

                SizeF sf = g.MeasureString(message, Font);
                g.DrawString(message, Font, RootEditor.ColorTable.InvalidValueBrush, new PointF(xOffset + 10, 16 - sf.Height / 2));

                xOffset += (int)sf.Width + 10;



                string discardMessage = Properties.Resources.DiscardInvalidValue;


                SizeF dsf = g.MeasureString(discardMessage, Font);

                discardInvalidValueButtonBounds = new Rectangle(xOffset, yOffset, (int)dsf.Width + 10, innerHeight);
                g.FillRectangle(RootEditor.ColorTable.DiscardInvalidValueButtonBackBrush, discardInvalidValueButtonBounds);
                g.DrawString(discardMessage, Font, RootEditor.ColorTable.DiscardInvalidValueButtonForeBrush, xOffset + 5, 16 - dsf.Height / 2);



                xOffset += (int)sf.Width + 20;


                return;
            }

            if (!IsEqualToDefaultValue() && !Node.IsArrayPrefab)
            {
                int width = IsFocused ? 29 : 30;
                restoreDefaultValueButtonBounds = new Rectangle(Width - xRightOffset - width, yOffset, width, innerHeight);

                g.FillRectangle(RootEditor.ColorTable.RestoreDefaultValueButtonBackBrush, restoreDefaultValueButtonBounds);

                g.DrawLine(RootEditor.ColorTable.RestoreDefaultValueButtonForePen, Width - xRightOffset - width + 8, 16, Width - xRightOffset - 8, 16);


                xRightOffset += width;
            }


            textBoxBounds = new Rectangle(xOffset, yOffset, Width - xOffset - xRightOffset, innerHeight);
            g.FillRectangle(RootEditor.ColorTable.TextBoxBackBrush, textBoxBounds);

            if (valueBox is null)
            {
                if (!(Node.TryGetSuggestions() is null || Node.Suggestions.IsEmpty) && ValidValue.Value is not null)
                {
                    foreach (IJtSuggestion item in Node.Suggestions.GetSuggestions(RootEditor.DynamicSuggestionsSource!))
                    {
                        if (!SuggestionEqualJValue(item, ValidValue))
                            continue;

                        SizeF s = g.MeasureString(item.DisplayName, Font);

                        SolidBrush brush2;

                        if (InvalidValue)
                        {
                            if (Node.ForceUsingSuggestions)
                                brush2 = RootEditor.ColorTable.InvalidValueBrush;
                            else
                                brush2 = RootEditor.ColorTable.WarningValueBrush;
                        }
                        else
                            brush2 = RootEditor.ColorTable.TextBoxForeBrush;

                        g.DrawString(item.DisplayName, Font, brush2, new PointF(xOffset + 10, 16 - s.Height / 2));
                        return;
                    }
                }
                SolidBrush brush;

                if (InvalidValue)
                {
                    if (Node.ForceUsingSuggestions)
                        brush = RootEditor.ColorTable.InvalidValueBrush;
                    else
                        brush = RootEditor.ColorTable.WarningValueBrush;
                }
                else
                    brush = RootEditor.ColorTable.TextBoxForeBrush;

                string? displayValue = Node.GetDisplayString(Value);

                SizeF sf = g.MeasureString(displayValue, Font);

                g.DrawString(displayValue, Font, brush, new PointF(xOffset + 10, 16 - sf.Height / 2));
            }
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (textBoxBounds.Contains(e.Location))
            {
                CreateTextBox(true);
                Invalidate();
                return;
            }
            if (RootEditor.ReadOnly)
                return;
            if (discardInvalidValueButtonBounds.Contains(e.Location))
            {
                CreateValue();
                Invalidate();
                return;
            }
            if (restoreDefaultValueButtonBounds.Contains(e.Location))
            {
                if(valueBox is not null)
                {
                    Parent?.Focus();
                    Controls.Remove(valueBox);
                    valueBox = null;
                    Focus();

                }
                CreateValue();

                Invalidate();
                return;
            }
        }
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);

            if (txtDynamicName is not null)
                return;


            CreateTextBox();
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (IsInvalidValueType)
                return;
            if (textBoxBounds.Contains(e.Location))
            {
                if (Node.TryGetSuggestions() is null || Node.Suggestions.IsEmpty)
                    Cursor = Cursors.IBeam;
                else
                    Cursor = Cursors.Hand;
                return;
            }
            if ((discardInvalidValueButtonBounds.Contains(e.Location) || restoreDefaultValueButtonBounds.Contains(e.Location)) && !RootEditor.ReadOnly)
            {
                Cursor = Cursors.Hand;
                return;
            }
        }


        private IEnumerable<IJtSuggestion> GetDynamicSuggestions(JtIdentifier id)
        {
            if (id.Value?.StartsWith("jtf:", StringComparison.OrdinalIgnoreCase) is true && Node is JtStringNode)
            {
                ReadOnlySpan<char> nodeId = id.Value.AsSpan(4);

                ChangedEvent? ce = eventManager.GetEvent(nodeId.ToString());
                if (ce is null)
                    return Enumerable.Empty<IJtSuggestion>();
                JToken? value = ce.Value;
                if (value is not JObject obj)
                    return Enumerable.Empty<IJtSuggestion>();

                return CreateSuggestionsFromObject(obj);
            }
            else
            {
                return RootEditor.DynamicSuggestionsSource?.Invoke(id) ?? Enumerable.Empty<IJtSuggestion>();
            }

            static IEnumerable<IJtSuggestion> CreateSuggestionsFromObject(JObject obj)
            {
                foreach (JProperty item in obj.Properties())
                {
                    yield return new JtSuggestion<string>(item.Name);
                }
            }
        }
    }
}
