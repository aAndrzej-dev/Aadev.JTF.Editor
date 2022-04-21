﻿using Aadev.JTF.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Aadev.JTF.Editor.EditorItems
{
    internal sealed class StringEditorItem : EditorItem
    {
        private TextBox? textBox;
        private JToken _value = JValue.CreateNull();
        private Rectangle textBoxBounds = Rectangle.Empty;

        protected override bool IsFocused => Focused || textBox?.Focused is true;

        private new JtString Type => (JtString)base.Type;


        private string? RawValue
        {
            get => _value.Type == Type.JsonType ? ((string?)_value ?? Type.Default) : (_value.Type is JTokenType.Null ? Type.Default : null);
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

        internal override bool IsSaveable => Type.Required || (Value.Type != JTokenType.Null && (string?)Value != Type.Default);
        internal StringEditorItem(JtToken type, JToken? token, EventManager eventManager) : base(type, token, eventManager) { }


        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (IsInvalidValueType)
                return;
            bool createTextBox = false;
            if (Focused && textBoxBounds == Rectangle.Empty)
            {
                createTextBox = true;
            }


            textBoxBounds = new Rectangle(xOffset, yOffset, Width - xOffset - xRightOffset, innerHeight);
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(80, 80, 80)), textBoxBounds);

            if (textBox is null)
            {

                SizeF sf = e.Graphics.MeasureString(RawValue, Font);

                e.Graphics.DrawString(RawValue, Font, new SolidBrush(ForeColor), new PointF(xOffset + 10, 16 - sf.Height / 2));
            }
            if (createTextBox)
            {
                CreateTextBox();
            }

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
        protected override void OnMouseMove(MouseEventArgs e)
        {

            if (textBoxBounds.Contains(e.Location))
            {
                Cursor = Cursors.IBeam;
                return;
            }
            base.OnMouseMove(e);
        }
        protected override JToken CreateValue() => Value = Type.CreateDefaultToken();
        private void CreateTextBox()
        {
            if (IsInvalidValueType)
                return;
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
                AutoSize = false,

                Text = RawValue
            };



            textBox.Location = new Point(textBoxBounds.X + 10, 16 - textBox.Height / 2 + 2);
            textBox.Width = Width - textBoxBounds.X - 20 - xRightOffset;
            textBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            textBox.TextChanged += (sender, eventArgs) => Value = textBox.Text;
            textBox.LostFocus += (sender, eventArgs) =>
            {
                Controls.Remove(textBox);
                textBox = null;
                Invalidate();
            };

            Controls.Add(textBox);
            textBox?.Focus();
            textBox?.SelectAll();
        }
    }
}