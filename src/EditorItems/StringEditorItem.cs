using Aadev.JTF.Types;
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
        private Rectangle textboxBounds = Rectangle.Empty;


        private new JtString Type => (JtString)base.Type;


        private string? RawValue
        {
            get => _value?.Type == Type.JsonType ? ((string?)_value ?? Type.Default) : (_value?.Type is JTokenType.Null ? Type.Default : null); set => _value = new JValue(value);
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


        internal StringEditorItem(JtToken type, JToken? token, EventManager eventManager) : base(type, token, eventManager)
        {
            SetStyle(ControlStyles.Selectable, true);
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (IsInvalidValueType)
                return;



            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(80, 80, 80)), xOffset, 1, Width - xOffset - xRightOffset, 30);
            textboxBounds = new Rectangle(xOffset, 0, Width - xOffset - xRightOffset, 32);

            if (textBox is null)
            {

                SizeF sf = e.Graphics.MeasureString(RawValue, Font);

                e.Graphics.DrawString(RawValue, Font, new SolidBrush(ForeColor), new PointF(xOffset + 10, 16 - sf.Height / 2));
            }


        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (textboxBounds.Contains(e.Location))
            {
                CreateTextBox();
            }
        }
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            CreateTextBox();
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {

            if (textboxBounds.Contains(e.Location))
            {
                Cursor = Cursors.IBeam;
                return;
            }
            base.OnMouseMove(e);
        }
        protected override void CreateValue() => Value = Type.Default;
        private void CreateTextBox()
        {
            if (IsInvalidValueType)
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



            textBox.Location = new Point(textboxBounds.X + 10, 16 - textBox.Height / 2 + 2);
            textBox.Width = Width - textboxBounds.X - 20 - xRightOffset;
            textBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            textBox.TextChanged += (sender, eventArgs) => Value = textBox.Text;
            textBox.LostFocus += (sender, eventArgs) =>
            {
                Controls.Remove(textBox);
                textBox = null;
            };

            Controls.Add(textBox);
            textBox?.Focus();
            textBox?.SelectAll();
        }
    }
}