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

        public new JtValue Node => (JtValue)base.Node;
        private bool SuggestionEqualusJValue(IJtSuggestion suggestion, JValue value)
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

        private bool InvalidValue
        {
            get
            {
                if (value is not null && Node.ForecUsingSuggestions)
                {
                    if (Node.Suggestions.Count == 0)
                        return false;
                    foreach (IJtSuggestion item in Node.Suggestions)
                    {
                        if (SuggestionEqualusJValue(item, value))
                            return false;
                    }
                    return true;
                }
                return false;
            }
        }

        protected override bool IsFocused => base.IsFocused || valueBox?.Focused is true;

        private Rectangle discardInvalidValueButtonBounds = Rectangle.Empty;

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


        internal override bool IsSaveable => Node.Required || (Value.Type != JTokenType.Null && value.Value?.Equals(Node.GetDefault()) is false);
        internal ValueEditorItem(JtNode type, JToken? token, JsonJtfEditor jsonJtfEditor, EventManager? eventManager = null) : base(type, token, jsonJtfEditor, eventManager)
        {
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (IsInvalidValueType)
                return;
            if (InvalidValue)
            {
                string message = string.Format(Properties.Resources.InvalidValue, value.ToString());

                SizeF sf = e.Graphics.MeasureString(message, Font);
                e.Graphics.DrawString(message, Font, new SolidBrush(Color.Red), new PointF(xOffset + 10, 16 - sf.Height / 2));

                xOffset += (int)sf.Width + 10;



                string discardMessage = Properties.Resources.DiscardInvalidValue;


                SizeF dsf = e.Graphics.MeasureString(discardMessage, Font);

                discardInvalidValueButtonBounds = new Rectangle(xOffset, yOffset, (int)dsf.Width + 10, innerHeight);
                e.Graphics.FillRectangle(new SolidBrush(Color.Red), discardInvalidValueButtonBounds);
                e.Graphics.DrawString(discardMessage, Font, new SolidBrush(Color.White), xOffset + 5, 16 - dsf.Height / 2);



                xOffset += (int)sf.Width + 20;


                return;
            }




            textBoxBounds = new Rectangle(xOffset, yOffset, Width - xOffset - xRightOffset, innerHeight);
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(80, 80, 80)), textBoxBounds);

            if (valueBox is null)
            {
                if ((Node.Suggestions.Count > 0 || Node.Suggestions.CustomSourceId?.StartsWith('$') is true) && value.Value is not null)
                {
                    foreach (IJtSuggestion item in Node.Suggestions)
                    {
                        if (!SuggestionEqualusJValue(item, value))
                            continue;

                        SizeF s = e.Graphics.MeasureString(item.DisplayName, Font);

                        e.Graphics.DrawString(item.DisplayName, Font, new SolidBrush(ForeColor), new PointF(xOffset + 10, 16 - s.Height / 2));
                        return;
                    }
                }



                SizeF sf = e.Graphics.MeasureString(Value.ToString(), Font);

                e.Graphics.DrawString(Value.ToString(), Font, new SolidBrush(ForeColor), new PointF(xOffset + 10, 16 - sf.Height / 2));
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
                if (Node.Suggestions.Count > 0)
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
        private void CreateTextBox()
        {
            if (IsInvalidValueType)
                return;
            if (textBoxBounds == Rectangle.Empty)
                return;
            if (valueBox is not null)
            {
                return;
            }

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


                if (Node is JtString)
                {
                    textBox.TextChanged += (sender, eventArgs) => Value = textBox.Text;
                    textBox.LostFocus += (sender, eventArgs) =>
                    {
                        Value = textBox.Text;
                        Controls.Remove(textBox);
                        valueBox = null;
                        Invalidate();
                    };
                }
                else
                {
                    textBox.KeyPress += (s, e) => e.Handled = !char.IsDigit(e.KeyChar) && e.KeyChar != '-' && !char.IsControl(e.KeyChar) && (e.KeyChar != ',' || (Node.Type != JtNodeType.Float && Node.Type != JtNodeType.Double));

                    textBox.TextChanged += (s, e) =>
                    {

                        if (Node is JtByte jtByte)
                        {
                            if (BigInteger.TryParse(textBox?.Text, out BigInteger b))
                            {
                                Value = (byte)BigInteger.Min(jtByte.Max, BigInteger.Max(jtByte.Min, b));
                            }
                            else
                            {
                                textBox?.Undo();
                            }
                        }
                        else if (Node is JtShort jtShort)
                        {
                            if (BigInteger.TryParse(textBox?.Text, out BigInteger b))
                            {
                                Value = (short)BigInteger.Min(jtShort.Max, BigInteger.Max(jtShort.Min, b));
                            }
                            else
                            {
                                textBox?.Undo();
                            }
                        }
                        else if (Node is JtInt jtInt)
                        {
                            if (BigInteger.TryParse(textBox?.Text, out BigInteger b))
                            {
                                Value = (int)BigInteger.Min(jtInt.Max, BigInteger.Max(jtInt.Min, b));

                            }
                            else
                            {
                                textBox?.Undo();
                            }
                        }
                        else if (Node is JtLong jtLong)
                        {
                            if (BigInteger.TryParse(textBox?.Text, out BigInteger b))
                            {
                                Value = (long)BigInteger.Min(jtLong.Max, BigInteger.Max(jtLong.Min, b));
                            }
                            else
                            {
                                textBox?.Undo();
                            }
                        }
                        else if (Node is JtFloat jtFloat)
                        {

                            if (float.TryParse(textBox.Text, out float b))
                                Value = MathF.Min(jtFloat.Max, MathF.Max(jtFloat.Min, b));
                            else
                                textBox.Undo();
                        }
                        else if (Node is JtDouble jtDouble)
                        {
                            if (double.TryParse(textBox.Text, out double b))
                                Value = Math.Min(jtDouble.Max, Math.Max(jtDouble.Min, b));
                            else
                                textBox.Undo();
                        }



                    };
                    textBox.LostFocus += (sender, e) =>
                    {
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
    }
}
