﻿using Aadev.JTF.Types;
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
        private JValue _value = JValue.CreateNull();
        private Rectangle textBoxBounds = Rectangle.Empty;

        protected override bool IsFocused => Focused || textBox?.Focused is true;


        private JValue? RawValue
        {
            get => _value.Type == Type.JsonType ? _value : (_value.Type is JTokenType.Null ? new JValue(0) : null);
            set => _value = value ?? JValue.CreateNull();
        }
        public override JToken Value
        {
            get => _value;
            set
            {
                _value = (JValue)value;
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

                if (Type is JtByte jtByte)
                    return jtByte.Default != (byte?)RawValue;
                if (Type is JtShort jtShort)
                    return jtShort.Default != (short?)RawValue;
                if (Type is JtInt jtInt)
                    return jtInt.Default != (int?)RawValue;
                if (Type is JtLong jtLong)
                    return jtLong.Default != (long?)RawValue;
                if (Type is JtFloat jtFloat)
                    return jtFloat.Default != (float?)RawValue;
                if (Type is JtDouble jtDouble)
                    return jtDouble.Default != (double?)RawValue;

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
            bool createTextBox = false;
            if (Focused && textBoxBounds == Rectangle.Empty)
            {
                createTextBox = true;
            }


            textBoxBounds = new Rectangle(xOffset, yOffset, Width - xOffset - xRightOffset, innerHeight);
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(80, 80, 80)), textBoxBounds);

            if (textBox == null)
            {

                SizeF sf = e.Graphics.MeasureString(RawValue!.ToString(), Font);

                e.Graphics.DrawString(RawValue.ToString(), Font, new SolidBrush(ForeColor), new PointF(xOffset + 10, 16 - sf.Height / 2));

            }
            if (createTextBox)
            {
                CreateTextBox();
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
                return Value = ((JtFloat)Type).Default;
            else if (Type.Type == JtTokenType.Double)
                return Value = ((JtDouble)Type).Default;

            throw new Exception("Current element hasn't got numeric value");
        }

        private void CreateTextBox()
        {
            if (IsInvalidValueType)
            {
                return;
            }
            if (textBoxBounds == Rectangle.Empty)
                return;
            if (textBox is not null)
            {
                return;
            }


            textBox = new TextBox
            {
                Font = Font,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = ForeColor,
                AutoSize = false
            };


            textBox.Location = new Point(xOffset + 10, 16 - textBox.Height / 2 + 2);
            textBox.Width = Width - xOffset - 20 - xRightOffset;
            textBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBox.Text = RawValue!.ToString();

            textBox.KeyPress += (s, e) => e.Handled = !char.IsDigit(e.KeyChar) && e.KeyChar != '-' && !char.IsControl(e.KeyChar) && (e.KeyChar != ',' || (Type.Type != JtTokenType.Float && Type.Type != JtTokenType.Double));

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
                        Value = MathF.Min(jtFloat.Max, MathF.Max(jtFloat.Min, b));
                    else textBox.Undo();
                }
                else if (Type is JtDouble jtDouble)
                {
                    if (double.TryParse(textBox.Text, out double b))
                        Value = Math.Min(jtDouble.Max, Math.Max(jtDouble.Min, b));
                    else textBox.Undo();
                }


                Controls.Remove(textBox);
                textBox = null;
                Invalidate();
            };

        }
    }
}