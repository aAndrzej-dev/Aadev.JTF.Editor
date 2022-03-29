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
            get => _value?.Type == Type.JsonType ? ((bool?)_value ?? Type.Default) : (_value?.Type is JTokenType.Null ? Type.Default : null); set => _value = new JValue(value);
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
            if (InvalidValueType)
                return;
            int width = Width - xOffset - xRightOffset;

            Graphics g = e.Graphics;

            g.FillRectangle(new SolidBrush(RawValue ?? Type.Default ? BackColor : Color.Red), xOffset, 1, width / 2, Height - 2);
            falsePanelRect = new Rectangle(xOffset, 1, width / 2, Height - 2);


            g.FillRectangle(new SolidBrush(RawValue ?? Type.Default ? Color.Green : BackColor), xOffset + width / 2, 1, width / 2, Height - 2);
            truePanelRect = new Rectangle(xOffset + width / 2, 1, width / 2, Height - 2);

            SizeF falseLabelSize = g.MeasureString("False", Font);

            g.DrawString("False", Font, new SolidBrush(RawValue ?? Type.Default ? ForeColor : Color.White), new PointF(xOffset + width / 4 - falseLabelSize.Width / 2, Height / 2 - falseLabelSize.Height / 2));

            SizeF trueLabelSize = g.MeasureString("True", Font);

            g.DrawString("True", Font, new SolidBrush(RawValue ?? Type.Default ? Color.White : ForeColor), new PointF(xOffset + width / 2 + width / 4 - trueLabelSize.Width / 2, Height / 2 - trueLabelSize.Height / 2));

        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (InvalidValueType)
            {
                return;
            }

            if (falsePanelRect.Contains(e.Location))
            {
                Value = false;
            }
            else if (truePanelRect.Contains(e.Location))
            {
                Value = true;
            }
        }
        protected override void CreateValue() => Value = Type.Default;
    }
}