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

        internal override bool IsSaveable => Node.Required || (Value.Type != JTokenType.Null && (bool?)Value != Node.Default);
        private bool? RawValue
        {
            get => _value.Type == Node.JsonType ? ((bool?)_value ?? Node.Default) : (_value.Type is JTokenType.Null ? Node.Default : null);
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

        private new JtBool Node => (JtBool)base.Node;

        internal BoolEditorItem(JtNode type, JToken? token, EventManager eventManager, JsonJtfEditor jsonJtfEditor) : base(type, token, eventManager, jsonJtfEditor) { }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (IsInvalidValueType)
                return;
            int width = Width - xOffset - xRightOffset;
            int halfWidth = (int)(width * 0.5f);


            Graphics g = e.Graphics;

            falsePanelRect = new Rectangle(xOffset, yOffset, halfWidth, innerHeight);
            g.FillRectangle(new SolidBrush(RawValue ?? Node.Default ? BackColor : Color.Red), falsePanelRect);


            truePanelRect = new Rectangle(xOffset + halfWidth, yOffset, halfWidth, innerHeight);
            g.FillRectangle(new SolidBrush(RawValue ?? Node.Default ? Color.Green : BackColor), truePanelRect);

            SizeF falseLabelSize = g.MeasureString("False", Font);

            g.DrawString("False", Font, new SolidBrush(RawValue ?? Node.Default ? ForeColor : Color.White), new PointF(xOffset + width / 4 - falseLabelSize.Width / 2, 16 - falseLabelSize.Height / 2));

            SizeF trueLabelSize = g.MeasureString("True", Font);

            g.DrawString("True", Font, new SolidBrush(RawValue ?? Node.Default ? Color.White : ForeColor), new PointF(xOffset + halfWidth + width / 4 - trueLabelSize.Width / 2, 16 - trueLabelSize.Height / 2));

        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (IsInvalidValueType)
                return;


            if (falsePanelRect.Contains(e.Location))
            {
                Value = false;
                Focus();
                return;
            }
            if (truePanelRect.Contains(e.Location))
            {
                Value = true;
                Focus();
                return;
            }


        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (IsInvalidValueType)
                return;

            if (e.KeyCode == Keys.Space)
            {
                Value = (bool?)Value is false;
            }
        }
    }
}