using Aadev.JTF.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Windows.Forms;

namespace Aadev.JTF.Editor.EditorItems
{
    internal sealed class ValueEditorItem : EditorItem
    {
        private Control? valueBox;
        private JValue value = JValue.CreateNull();
        private Rectangle textBoxBounds = Rectangle.Empty;
        private Rectangle discardInvalidValueButtonBounds = Rectangle.Empty;

        private new JtValue Node => (JtValue)base.Node;
        private bool InvalidValue
        {
            get
            {
                if (value is null || value.Type == JTokenType.Null)
                    return false;





                if (Node.Suggestions.Count == 0 && !(Node.Suggestions.CustomSourceId?.StartsWith("$") is true))
                    return false;
                foreach (IJtSuggestion item in Node.Suggestions)
                {
                    if (SuggestionEqualJValue(item, value))
                        return false;
                }
                if (Node.Suggestions.CustomSourceId?.StartsWith("$") is true)
                {
                    string id = Node.Suggestions.CustomSourceId.AsSpan(1).ToString();

                    if (RootEditor.GetDynamicSource?.Invoke(id) is IEnumerable<IJtSuggestion> enumerable)
                    {
                        bool empty = true;
                        foreach (IJtSuggestion item in enumerable)
                        {
                            if (item is null || item.ValueType != Node.ValueType)
                                continue;
                            empty = false;
                            if (SuggestionEqualJValue(item, value))
                                return false;
                        }
                        if (empty)
                            return false;
                    }
                    else
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
                        return Color.Red;
                    else
                        return Color.Yellow;
                }
                return base.BorderColor;
            }

        }
        public override JToken Value
        {
            get => value;
            set
            {
                if (value is not JValue jv)
                    throw new Exception();
                this.value = jv;
                Invalidate();
                OnValueChanged();
            }
        }

        internal ValueEditorItem(JtNode type, JToken? token, JsonJtfEditor jsonJtfEditor, EventManager? eventManager = null) : base(type, token, jsonJtfEditor, eventManager)
        {
            if (Node.Type.IsNumericType && token is null)
                value = (JValue)Node.CreateDefaultValue();

        }

        private bool SuggestionEqualJValue(IJtSuggestion suggestion, JValue value)
        {
            return Node switch
            {
                JtByte _ => suggestion.GetValue<byte>().Equals((byte)value),
                JtShort _ => suggestion.GetValue<short>().Equals((short)value),
                JtInt _ => suggestion.GetValue<int>().Equals((int)value),
                JtLong _ => suggestion.GetValue<long>().Equals((long)value),
                JtFloat _ => suggestion.GetValue<float>().Equals((float)value),
                JtDouble _ => suggestion.GetValue<double>().Equals((double)value),
                JtString _ => suggestion.GetValue<string>().Equals((string?)value),
                _ => throw new Exception(),
            };
        }
        private bool IsEqualToDefaultValue()
        {
            return Node switch
            {
                JtByte jtByte => jtByte.Default.Equals((byte)value),
                JtShort jtShort => jtShort.Default.Equals((short)value),
                JtInt jtInt => jtInt.Default.Equals((int)value),
                JtLong jtLong => jtLong.Default.Equals((long)value),
                JtFloat jtFloat => jtFloat.Default.Equals((float)value),
                JtDouble jtDouble => jtDouble.Default.Equals((double)value),
                JtString jtString => jtString.Default.Equals((string?)value),
                _ => throw new Exception(),
            };
        }
        private void CreateTextBox()
        {
            if (IsInvalidValueType)
                return;
            if (textBoxBounds == Rectangle.Empty)
                return;
            if (valueBox is not null)
                return;
            if (Parent is EditorItem parent && parent.SuspendFocus)
                return;

            if (Node.Suggestions.Count > 0 || Node.Suggestions.CustomSourceId?.StartsWith('$') is true)
            {
                ComboBox comboBox = new ComboBox
                {
                    Font = Font,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(80, 80, 80),
                    ForeColor = ForeColor,
                    AutoSize = false,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    Width = Width - xOffset - 12 - xRightOffset,
                    Text = Value.ToString()
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

                comboBox.Focus();
                comboBox.DroppedDown = true;

                if (Node.Suggestions.CustomSourceId?.StartsWith("$") is true)
                {
                    string id = Node.Suggestions.CustomSourceId.AsSpan(1).ToString();

                    if (RootEditor.GetDynamicSource?.Invoke(id) is IEnumerable<IJtSuggestion> enumerable)
                    {
                        foreach (IJtSuggestion item in enumerable)
                        {
                            if (item is null || item.ValueType != Node.ValueType)
                                continue;
                            comboBox.Items.Add(item);
                        }
                    }
                }
                else
                {

                    foreach (IJtSuggestion item in Node.Suggestions)
                    {
                        if (item is null)
                            continue;
                        comboBox.Items.Add(item);
                    }
                }






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
            else
            {
                TextBox textBox = new TextBox
                {
                    Font = Font,
                    BorderStyle = BorderStyle.None,
                    BackColor = Color.FromArgb(80, 80, 80),
                    ForeColor = ForeColor,
                    AutoSize = false,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    Width = Width - textBoxBounds.X - 20 - xRightOffset,

                    Text = Value.ToString()
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
                        else if (e.KeyChar is '-' && !textBox.Text.Contains('-'))
                            e.Handled = false;
                        else if (e.KeyChar == ',' && (Node is JtFloat or JtDouble) && !textBox.Text.Contains(','))
                            e.Handled = false;
                        else if (e.KeyChar == 'e' && (Node is JtFloat or JtDouble) && !textBox.Text.Contains('e'))
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
                            textBox.Text = Node.GetDefault().ToString();
                            textBox.SelectAll();
                            value.Value = Node.GetDefault();
                            Invalidate();
                            OnValueChanged();
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
                string message = string.Format(Properties.Resources.InvalidValue, value.ToString());

                SizeF sf = e.Graphics.MeasureString(message, Font);
                e.Graphics.DrawString(message, Font, redBrush, new PointF(xOffset + 10, 16 - sf.Height / 2));

                xOffset += (int)sf.Width + 10;



                string discardMessage = Properties.Resources.DiscardInvalidValue;


                SizeF dsf = e.Graphics.MeasureString(discardMessage, Font);

                discardInvalidValueButtonBounds = new Rectangle(xOffset, yOffset, (int)dsf.Width + 10, innerHeight);
                e.Graphics.FillRectangle(redBrush, discardInvalidValueButtonBounds);
                e.Graphics.DrawString(discardMessage, Font, whiteBrush, xOffset + 5, 16 - dsf.Height / 2);



                xOffset += (int)sf.Width + 20;


                return;
            }




            textBoxBounds = new Rectangle(xOffset, yOffset, Width - xOffset - xRightOffset, innerHeight);
            e.Graphics.FillRectangle(grayBrush, textBoxBounds);

            if (valueBox is null)
            {
                if ((Node.Suggestions.Count > 0 || Node.Suggestions.CustomSourceId?.StartsWith('$') is true) && value.Value is not null)
                {
                    foreach (IJtSuggestion item in Node.Suggestions)
                    {
                        if (!SuggestionEqualJValue(item, value))
                            continue;

                        SizeF s = e.Graphics.MeasureString(item.DisplayName, Font);

                        SolidBrush brush2;

                        if (InvalidValue)
                        {
                            if (Node.ForecUsingSuggestions)
                                brush2 = redBrush;
                            else
                                brush2 = yellowBrush;
                        }
                        else
                            brush2 = ForeColorBrush;

                        e.Graphics.DrawString(item.DisplayName, Font, brush2, new PointF(xOffset + 10, 16 - s.Height / 2));
                        return;
                    }
                }
                SolidBrush brush;

                if (InvalidValue)
                {
                    if (Node.ForecUsingSuggestions)
                        brush = redBrush;
                    else
                        brush = yellowBrush;
                }
                else
                    brush = ForeColorBrush;


                SizeF sf = e.Graphics.MeasureString(Value.ToString(), Font);

                e.Graphics.DrawString(Value.ToString(), Font, brush, new PointF(xOffset + 10, 16 - sf.Height / 2));
            }
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (textBoxBounds.Contains(e.Location))
            {
                CreateTextBox();
                Invalidate();
                return;
            }
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

            if (textBoxBounds.Contains(e.Location))
            {
                if (Node.Suggestions.Count > 0 || Node.Suggestions.CustomSourceId?.StartsWith('$') is true)
                    Cursor = Cursors.Hand;
                else
                    Cursor = Cursors.IBeam;
                return;
            }
            if (discardInvalidValueButtonBounds.Contains(e.Location))
            {
                Cursor = Cursors.Hand;
                return;
            }
            base.OnMouseMove(e);
        }

    }
}
