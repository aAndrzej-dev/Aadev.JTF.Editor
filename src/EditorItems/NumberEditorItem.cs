using Aadev.JTF.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Numerics;
using System.Windows.Forms;

namespace Aadev.JTF.Editor.EditorItems
{
    internal sealed class NumberEditorItem : EditorItem
    {
        private TextBox? textBox;
        private JToken _value = JValue.CreateNull();
        private Rectangle textBoxBounds = Rectangle.Empty;

        protected override bool IsFocused => Focused || textBox?.Focused is true;


        private int? RawValue
        {
            get => _value?.Type == Type.JsonType ? ((int?)_value ?? 0) : (_value?.Type is JTokenType.Null ? 0 : null);
            set => _value = new JValue(value);
        }
        public override JToken Value
        {
            get => _value;
            set
            {
                _value = value;
                Invalidate();
                OnValueChanged();
            }
        }

        internal override bool IsSaveable
        {
            get
            {
                if (Type.Required)
                    return true;
                if (Value.Type == JTokenType.Null)
                    return false;

                if (Type.Type == JtTokenType.Byte)
                    return ((JtByte)Type).Default != (byte?)Value;
                else if (Type.Type == JtTokenType.Short)
                    return ((JtShort)Type).Default != (short?)Value;
                else if (Type.Type == JtTokenType.Int)
                    return ((JtInt)Type).Default != (int?)Value;
                else if (Type.Type == JtTokenType.Long)
                    return ((JtLong)Type).Default != (long?)Value;
                else if (Type.Type == JtTokenType.Float)
                    return ((JtDouble)Type).Default != (float?)Value;
                else if (Type.Type == JtTokenType.Double)
                    return ((JtFloat)Type).Default != (double?)Value;

                throw new Exception();
            }
        }

        internal NumberEditorItem(JtToken type, JToken? token, EventManager eventManager) : base(type, token, eventManager) { }



        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (IsInvalidValueType)
            {
                return;
            }
            textBoxBounds = new Rectangle(xOffset, yOffset, Width - xOffset - xRightOffset, innerHeight);
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(80, 80, 80)), textBoxBounds);

            if (textBox == null)
            {

                SizeF sf = e.Graphics.MeasureString(RawValue.ToString(), Font);

                e.Graphics.DrawString(RawValue.ToString(), Font, new SolidBrush(ForeColor), new PointF(xOffset + 10, 16 - sf.Height / 2));

            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (textBoxBounds.Contains(e.Location))
                Cursor = Cursors.IBeam;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (textBoxBounds.Contains(e.Location))
            {
                CreateTextBox();
                Invalidate();
            }
        }
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            CreateTextBox();
        }
        protected override JToken CreateValue()
        {
            if (Type.Type == JtTokenType.Byte)
                return Value = ((JtByte)Type).Default;
            else if (Type.Type == JtTokenType.Short)
                return Value = ((JtShort)Type).Default;
            else if (Type.Type == JtTokenType.Int)
                return Value = ((JtInt)Type).Default;
            else if (Type.Type == JtTokenType.Long)
                return Value = ((JtLong)Type).Default;
            else if (Type.Type == JtTokenType.Float)
                return Value = ((JtDouble)Type).Default;
            else if (Type.Type == JtTokenType.Double)
                return Value = ((JtFloat)Type).Default;

            throw new Exception("Current element hasn't got numeric value");
        }

        private void CreateTextBox()
        {
            if (IsInvalidValueType)
            {
                return;
            }

            if (textBox is not null)
            {
                return;
            }

            Type t = Type.GetType();

            textBox = new TextBox
            {
                Font = Font,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = ForeColor,
                AutoSize = false
            };


            textBox.Location = new System.Drawing.Point(xOffset + 10, 16 - textBox.Height / 2 + 2);
            textBox.Width = Width - xOffset - 20 - xRightOffset;
            textBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBox.Text = RawValue.ToString();

            textBox.KeyPress += (s, e) => e.Handled = !char.IsDigit(e.KeyChar) && e.KeyChar != '-' && !char.IsControl(e.KeyChar) && (e.KeyChar != '.' || (Type.Type != JtTokenType.Float && Type.Type != JtTokenType.Double));

            textBox.TextChanged += (sender, eventArgs) =>
            {


            };
            Controls.Add(textBox);

            textBox.Focus();
            textBox.SelectAll();

            textBox.LostFocus += (s, e) =>
            {

                if (Type is JtByte jtByte)
                {
                    if (BigInteger.TryParse(textBox.Text, out BigInteger b))
                    {
                        Value = (byte)BigInteger.Min(jtByte.Max, BigInteger.Max(jtByte.Min, b));
                    }
                    else
                    {
                        textBox.Undo();
                    }
                }
                else if (Type is JtShort jtShort)
                {
                    if (BigInteger.TryParse(textBox.Text, out BigInteger b))
                    {
                        Value = (short)BigInteger.Min(jtShort.Max, BigInteger.Max(jtShort.Min, b));
                    }
                    else
                    {
                        textBox.Undo();
                    }
                }
                else if (Type is JtInt jtInt)
                {
                    if (BigInteger.TryParse(textBox.Text, out BigInteger b))
                    {
                        Value = (int)BigInteger.Min(jtInt.Max, BigInteger.Max(jtInt.Min, b));
                    }
                    else
                    {
                        textBox.Undo();
                    }
                }
                else if (Type is JtLong jtLong)
                {
                    if (BigInteger.TryParse(textBox.Text, out BigInteger b))
                    {
                        Value = (long)BigInteger.Min(jtLong.Max, BigInteger.Max(jtLong.Min, b));
                    }
                    else
                    {
                        textBox.Undo();
                    }
                }
                else if (Type is JtFloat jtFloat)
                {

                    if (float.TryParse(textBox.Text, out float b))
                        Value = MathF.Min(jtFloat.Max, MathF.Min(jtFloat.Min, b));
                    else textBox.Undo();
                }
                else if (Type is JtLong jtDouble)
                {
                    if (double.TryParse(textBox.Text, out double b))
                        Value = Math.Min(jtDouble.Max, Math.Min(jtDouble.Min, b));
                    else textBox.Undo();
                }


                Controls.Remove(textBox);
                textBox = null;
                Invalidate();
            };
        }
    }
}