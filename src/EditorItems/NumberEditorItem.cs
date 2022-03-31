using Aadev.JTF.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Aadev.JTF.Editor.EditorItems
{
    internal sealed class NumberEditorItem : EditorItem
    {
        private TextBox? textBox;
        private JToken _value = JValue.CreateNull();



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

        internal NumberEditorItem(JtToken type, JToken? token) : base(type, token) { }


        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (IsInvalidValueType)
            {
                return;
            }

            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(80, 80, 80)), xOffset, 1, Width - xOffset - xRightOffset, 30);

            if (textBox == null)
            {

                SizeF sf = e.Graphics.MeasureString(RawValue.ToString(), Font);

                e.Graphics.DrawString(RawValue.ToString(), Font, new SolidBrush(ForeColor), new PointF(xOffset + 10, 16 - sf.Height / 2));

            }
        }
        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            CreateNumeric();
        }
        protected override void CreateValue()
        {
            if (Type.Type == JtTokenType.Byte)
                Value = ((JtByte)Type).Default;
            else if (Type.Type == JtTokenType.Short)
                Value = ((JtShort)Type).Default;
            else if (Type.Type == JtTokenType.Int)
                Value = ((JtInt)Type).Default;
            else if (Type.Type == JtTokenType.Long)
                Value = ((JtLong)Type).Default;
            else if (Type.Type == JtTokenType.Float)
                Value = ((JtDouble)Type).Default;
            else if (Type.Type == JtTokenType.Double)
                Value = ((JtFloat)Type).Default;
        }

        public void CreateNumeric()
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

            textBox.KeyPress += (s, e) => e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar) && (e.KeyChar != '.' || (Type.Type != JtTokenType.Float && Type.Type != JtTokenType.Double));

            textBox.TextChanged += (sender, eventArgs) =>
            {

                if (Type.Type == JtTokenType.Byte)
                    Value = byte.Parse(textBox.Text!);
                else if (Type.Type == JtTokenType.Short)
                    Value = short.Parse(textBox.Text!);
                else if (Type.Type == JtTokenType.Int)
                    Value = int.Parse(textBox.Text!);
                else if (Type.Type == JtTokenType.Long)
                    Value = long.Parse(textBox.Text!);
                else if (Type.Type == JtTokenType.Float)
                    Value = float.Parse(textBox.Text!);
                else if (Type.Type == JtTokenType.Double)
                    Value = double.Parse(textBox.Text!);


            };
            Controls.Add(textBox);

            textBox.Focus();
            textBox.SelectAll();

            textBox.LostFocus += (s, e) =>
            {
                Controls.Remove(textBox);
                textBox = null;
            };
        }
    }
}