using Aadev.JTF.Types;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Windows.Forms;

namespace Aadev.JTF.Editor.EditorItems
{
    internal sealed class BoolEditorItem : EditorItem
    {
        private JToken _value = JValue.CreateNull();
        private Rectangle falsePanelRect = Rectangle.Empty;
        private Rectangle truePanelRect = Rectangle.Empty;


        private bool? RawValue
        {
            get => _value.Type == Type.JsonType ? ((bool?)_value ?? Type.Default) : (_value.Type is JTokenType.Null ? Type.Default : null); set => _value = new JValue(value);
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

        private new JtBool Type => (JtBool)base.Type;

        internal BoolEditorItem(JtToken type, JToken? token) : base(type, token) { }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (IsInvalidValueType)
                return;
            int width = Width - xOffset - xRightOffset;
            int halfWidth = (int)(width * 0.5f);


            Graphics g = e.Graphics;

            g.FillRectangle(new SolidBrush(RawValue ?? Type.Default ? BackColor : Color.Red), xOffset, 1, halfWidth, Height - 2);
            falsePanelRect = new Rectangle(xOffset, 1, halfWidth, Height - 2);


            g.FillRectangle(new SolidBrush(RawValue ?? Type.Default ? Color.Green : BackColor), xOffset + halfWidth, 1, halfWidth, Height - 2);
            truePanelRect = new Rectangle(xOffset + halfWidth, 1, halfWidth, Height - 2);

            SizeF falseLabelSize = g.MeasureString("False", Font);

            g.DrawString("False", Font, new SolidBrush(RawValue ?? Type.Default ? ForeColor : Color.White), new PointF(xOffset + width / 4 - falseLabelSize.Width / 2, Height / 2 - falseLabelSize.Height / 2));

            SizeF trueLabelSize = g.MeasureString("True", Font);

            g.DrawString("True", Font, new SolidBrush(RawValue ?? Type.Default ? Color.White : ForeColor), new PointF(xOffset + halfWidth + width / 4 - trueLabelSize.Width / 2, Height / 2 - trueLabelSize.Height / 2));

        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (IsInvalidValueType)
            {
                return;
            }

            if (falsePanelRect.Contains(e.Location))
            {
                Value = false;
                return;
            }
            if (truePanelRect.Contains(e.Location))
            {
                Value = true;
                return;
            }
        }
        protected override void CreateValue() => Value = Type.Default;
    }
}