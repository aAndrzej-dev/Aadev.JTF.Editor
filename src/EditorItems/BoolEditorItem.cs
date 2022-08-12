using Aadev.JTF.Types;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Windows.Forms;

namespace Aadev.JTF.Editor.EditorItems
{
    internal sealed class BoolEditorItem : EditorItem
    {
        private JToken value = JValue.CreateNull();
        private Rectangle falsePanelRect = Rectangle.Empty;
        private Rectangle truePanelRect = Rectangle.Empty;

        internal override bool IsSaveable => base.IsSaveable || (Value.Type != JTokenType.Null && (bool?)Value != Node.Default);
        private bool? RawValue
        {
            get => value.Type == Node.JsonType ? ((bool?)value ?? Node.Default) : (value.Type is JTokenType.Null ? Node.Default : null);
            set => this.value = new JValue(value);
        }
        public override JToken Value
        {
            get => value;
            set
            {
                this.value = value;
                Invalidate();
                OnValueChanged();
            }
        }

        private new JtBool Node => (JtBool)base.Node;

        internal BoolEditorItem(JtNode type, JToken? token, JsonJtfEditor jsonJtfEditor, EventManager? eventManager = null) : base(type, token, jsonJtfEditor, eventManager) { }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (IsInvalidValueType)
                return;
            int width = Width - xOffset - xRightOffset;
            int halfWidth = (int)(width * 0.5f);


            Graphics g = e.Graphics;

            falsePanelRect = new Rectangle(xOffset, yOffset, halfWidth, innerHeight);
            truePanelRect = new Rectangle(xOffset + halfWidth, yOffset, halfWidth, innerHeight);
            if (RawValue ?? Node.Default)
                g.FillRectangle(greenBrush, truePanelRect);
            else
                g.FillRectangle(redBrush, falsePanelRect);


            SizeF falseLabelSize = g.MeasureString("False", Font);

            g.DrawString("False", Font, RawValue ?? Node.Default ? ForeColorBrush : whiteBrush, new PointF(xOffset + width / 4 - falseLabelSize.Width / 2, 16 - falseLabelSize.Height / 2));

            SizeF trueLabelSize = g.MeasureString("True", Font);

            g.DrawString("True", Font, RawValue ?? Node.Default ? whiteBrush : ForeColorBrush, new PointF(xOffset + halfWidth + width / 4 - trueLabelSize.Width / 2, 16 - trueLabelSize.Height / 2));

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