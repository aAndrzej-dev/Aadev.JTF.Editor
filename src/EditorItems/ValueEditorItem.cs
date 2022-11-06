using Aadev.JTF.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Windows.Forms;

namespace Aadev.JTF.Editor.EditorItems
{
    internal sealed class ValueEditorItem : EditorItem
    {
        private Control? valueBox;
        private JToken value;
        private Rectangle textBoxBounds = Rectangle.Empty;
        private Rectangle discardInvalidValueButtonBounds = Rectangle.Empty;

        private new JtValue Node => (JtValue)base.Node;
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

                if (Node.Suggestions.IsEmpty)
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
        internal override bool IsSaveable => base.IsSaveable || (Value.Type != JTokenType.Null && !IsEqualToDefaultValue());
        protected override Color BorderColor
        {
            get
            {
                if (InvalidValue)
                {
                    if (Node.ForecUsingSuggestions)
                        return RootEditor.InvalidBorderColor;
                    else
                        return RootEditor.WarningBorderColor;
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

        [MemberNotNullWhen(false, "ValidValue")] public new bool IsInvalidValueType => base.IsInvalidValueType;
        internal ValueEditorItem(JtValue type, JToken? token, JsonJtfEditor jsonJtfEditor, EventManager eventManager) : base(type, token, jsonJtfEditor, eventManager)
        {
            value ??= Node.CreateDefaultValue();

        }

        private bool SuggestionEqualJValue(IJtSuggestion suggestion, JValue value)
        {
            if (value.Type != Node.JsonType)
                return false;
            return Node switch
            {
                JtByte _ => suggestion.GetValue<byte>().Equals((byte)value),
                JtShort _ => suggestion.GetValue<short>().Equals((short)value),
                JtInt _ => suggestion.GetValue<int>().Equals((int)value),
                JtLong _ => suggestion.GetValue<long>().Equals((long)value),
                JtFloat _ => suggestion.GetValue<float>().Equals((float)value),
                JtDouble _ => suggestion.GetValue<double>().Equals((double)value),
                JtString _ => suggestion.GetValue<string>().Equals((string?)value, StringComparison.Ordinal),
                _ => throw new Exception(),
            };
        }
        private bool IsEqualToDefaultValue()
        {
            if (value.Type != Node.JsonType)
                return false;
            return Node switch
            {
                JtByte jtByte => jtByte.Default.Equals((byte)value),
                JtShort jtShort => jtShort.Default.Equals((short)value),
                JtInt jtInt => jtInt.Default.Equals((int)value),
                JtLong jtLong => jtLong.Default.Equals((long)value),
                JtFloat jtFloat => jtFloat.Default.Equals((float)value),
                JtDouble jtDouble => jtDouble.Default.Equals((double)value),
                JtString jtString => jtString.Default.Equals((string?)value, StringComparison.Ordinal),
                _ => throw new Exception(),
            };
        }
        private void CreateTextBox(bool doubleclick = false)
        {
            if (IsInvalidValueType)
                return;
            if (textBoxBounds == Rectangle.Empty)
                return;
            if (valueBox is not null)
                return;
            if (Parent is EditorItem parent && parent.SuspendFocus)
                return;

            if (!Node.Suggestions.IsEmpty)
            {
                IJtSuggestion[] suggestions = Node.Suggestions.GetSuggestions(GetDynamicSuggestions).ToArray();

                if (suggestions.Length > RootEditor.MaximumSuggestionCountForComboBox || RootEditor.ReadOnly)
                {
                    if (!doubleclick)
                        return;

                    IJtSuggestion? currentSuggestion = suggestions.Where(x => SuggestionEqualJValue(x, ValidValue)).FirstOrDefault();
                    if (currentSuggestion is null && !Node.ForecUsingSuggestions)
                    {
                        switch (Node)
                        {
                            case JtByte:
                                currentSuggestion = new DynamicSuggestion<byte>((byte)ValidValue);
                                break;
                            case JtShort:
                                currentSuggestion = new DynamicSuggestion<short>((short)ValidValue);
                                break;
                            case JtInt:
                                currentSuggestion = new DynamicSuggestion<int>((int)ValidValue);
                                break;
                            case JtLong:
                                currentSuggestion = new DynamicSuggestion<long>((long)ValidValue);
                                break;
                            case JtFloat:
                                currentSuggestion = new DynamicSuggestion<float>((float)ValidValue);
                                break;
                            case JtDouble:
                                currentSuggestion = new DynamicSuggestion<double>((double)ValidValue);
                                break;
                            case JtString:
                                currentSuggestion = new DynamicSuggestion<string>((string?)ValidValue ?? string.Empty);
                                break;
                            default:
                                break;
                        }
                    }
                    DialogResult dr = RootEditor.SuggestionSelector.Show(suggestions, Node.ForecUsingSuggestions || RootEditor.ReadOnly, currentSuggestion);

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
                        BackColor = RootEditor.TextBoxBackColor,
                        ForeColor = RootEditor.TextBoxForeColor,
                        AutoSize = false,
                        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                        Width = Width - xOffset - 12 - xRightOffset,
                        Text = Value.ToString(),

                    };


                    comboBox.Location = new Point(xOffset + 10, 16 - comboBox.Height / 2 - 4);
                    if (Node.ForecUsingSuggestions)
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


                    if (!Node.ForecUsingSuggestions)
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
            else
            {
                TextBox textBox = new TextBox
                {
                    Font = Font,
                    BorderStyle = BorderStyle.None,
                    BackColor = RootEditor.TextBoxBackColor,
                    ForeColor = RootEditor.TextBoxForeColor,
                    AutoSize = false,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    Width = Width - textBoxBounds.X - 20 - xRightOffset,

                    Text = Value.ToString(),
                    ReadOnly = RootEditor.ReadOnly
                };
                textBox.Location = new Point(textBoxBounds.X + 10, 16 - textBox.Height / 2);


                if (Node is JtString strNode)
                {
                    if (strNode.MaxLength > 0)
                        textBox.MaxLength = strNode.MaxLength;
                    textBox.TextChanged += (sender, eventArgs) => Value = textBox.Text;
                    textBox.LostFocus += (sender, eventArgs) =>
                    {
                        Controls.Remove(textBox);
                        valueBox = null;
                        Invalidate();
                    };
                }
                else
                {
                    textBox.KeyPress += (s, e) =>
                    {
                        if (char.IsDigit(e.KeyChar) || char.IsControl(e.KeyChar))
                            e.Handled = false;
                        else if (e.KeyChar is '-' && !textBox.Text.Contains('-', StringComparison.Ordinal))
                            e.Handled = false;
                        else if (e.KeyChar == ',' && (Node is JtFloat or JtDouble) && !textBox.Text.Contains(',',StringComparison.Ordinal))
                            e.Handled = false;
                        else if (e.KeyChar == 'e' && (Node is JtFloat or JtDouble) && !textBox.Text.Contains('e', StringComparison.OrdinalIgnoreCase))
                            e.Handled = false;
                        else
                            e.Handled = true;
                    };
                    textBox.TextChanged += (s, e) =>
                    {
                        if (textBox is null)
                            return;
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
                        if (Node is JtByte jtByte)
                        {
                            if (BigInteger.TryParse(textBox.Text, out BigInteger b))
                            {
                                Value = (byte)BigInteger.Min(jtByte.Max, BigInteger.Max(jtByte.Min, b));
                            }
                        }
                        else if (Node is JtShort jtShort)
                        {
                            if (BigInteger.TryParse(textBox.Text, out BigInteger b))
                            {
                                Value = (short)BigInteger.Min(jtShort.Max, BigInteger.Max(jtShort.Min, b));
                            }
                        }
                        else if (Node is JtInt jtInt)
                        {
                            if (BigInteger.TryParse(textBox.Text, out BigInteger b))
                            {
                                Value = (int)BigInteger.Min(jtInt.Max, BigInteger.Max(jtInt.Min, b));
                            }
                        }
                        else if (Node is JtLong jtLong)
                        {
                            if (BigInteger.TryParse(textBox.Text, out BigInteger b))
                            {
                                Value = (long)BigInteger.Min(jtLong.Max, BigInteger.Max(jtLong.Min, b));
                            }
                        }
                        else if (Node is JtFloat jtFloat)
                        {

                            if (float.TryParse(textBox.Text, out float b))
                                Value = MathF.Min(jtFloat.Max, MathF.Max(jtFloat.Min, b));
                        }
                        else if (Node is JtDouble jtDouble)
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
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (IsInvalidValueType)
                return;
            if (InvalidValue && Node.ForecUsingSuggestions)
            {
                string message = string.Format(CultureInfo.CurrentCulture, Properties.Resources.InvalidValue, value.ToString());

                SizeF sf = e.Graphics.MeasureString(message, Font);
                e.Graphics.DrawString(message, Font, RootEditor.InvalidValueBrush, new PointF(xOffset + 10, 16 - sf.Height / 2));

                xOffset += (int)sf.Width + 10;



                string discardMessage = Properties.Resources.DiscardInvalidValue;


                SizeF dsf = e.Graphics.MeasureString(discardMessage, Font);

                discardInvalidValueButtonBounds = new Rectangle(xOffset, yOffset, (int)dsf.Width + 10, innerHeight);
                e.Graphics.FillRectangle(RootEditor.DiscardInvalidValueButtonBackBrush, discardInvalidValueButtonBounds);
                e.Graphics.DrawString(discardMessage, Font, RootEditor.DiscardInvalidValueButtonForeBrush, xOffset + 5, 16 - dsf.Height / 2);



                xOffset += (int)sf.Width + 20;


                return;
            }




            textBoxBounds = new Rectangle(xOffset, yOffset, Width - xOffset - xRightOffset, innerHeight);
            e.Graphics.FillRectangle(RootEditor.TextBoxBackBrush, textBoxBounds);

            if (valueBox is null)
            {
                if ((!Node.Suggestions.IsEmpty) && ValidValue.Value is not null)
                {
                    foreach (IJtSuggestion item in Node.Suggestions.GetSuggestions(RootEditor.GetDynamicSource!))
                    {
                        if (!SuggestionEqualJValue(item, ValidValue))
                            continue;

                        SizeF s = e.Graphics.MeasureString(item.DisplayName, Font);

                        SolidBrush brush2;

                        if (InvalidValue)
                        {
                            if (Node.ForecUsingSuggestions)
                                brush2 = RootEditor.InvalidValueBrush;
                            else
                                brush2 = RootEditor.WarinigValueBrush;
                        }
                        else
                            brush2 = RootEditor.TextBoxForeBrush;

                        e.Graphics.DrawString(item.DisplayName, Font, brush2, new PointF(xOffset + 10, 16 - s.Height / 2));
                        return;
                    }
                }
                SolidBrush brush;

                if (InvalidValue)
                {
                    if (Node.ForecUsingSuggestions)
                        brush = RootEditor.InvalidValueBrush;
                    else
                        brush = RootEditor.WarinigValueBrush;
                }
                else
                    brush = RootEditor.TextBoxForeBrush;


                SizeF sf = e.Graphics.MeasureString(Value.ToString(), Font);

                e.Graphics.DrawString(Value.ToString(), Font, brush, new PointF(xOffset + 10, 16 - sf.Height / 2));
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
                if (Node.Suggestions.IsEmpty)
                    Cursor = Cursors.IBeam;
                else
                    Cursor = Cursors.Hand;
                return;
            }
            if (discardInvalidValueButtonBounds.Contains(e.Location) && !RootEditor.ReadOnly)
            {
                Cursor = Cursors.Hand;
                return;
            }
        }

        
        private IEnumerable<IJtSuggestion> GetDynamicSuggestions(JtIdentifier id)
        {
            if (id.Identifier?.StartsWith("jtf:", StringComparison.OrdinalIgnoreCase) is true && Node is JtString)
            {
                ReadOnlySpan<char> nodeId = id.Identifier.AsSpan(4);

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
                return RootEditor.GetDynamicSource?.Invoke(id) ?? Enumerable.Empty<IJtSuggestion>();
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
