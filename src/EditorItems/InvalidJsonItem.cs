using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Aadev.JTF.Editor.EditorItems
{
    internal partial class InvalidJsonItem : UserControl, IJsonItem
    {
        private int xOffset;
        private int innerHeight;
        private int yOffset;
        private int xRightOffset;
        private readonly string? name;
        private Rectangle removeButtonBounds;
        private RectangleF viewValueButtonBounds;

        public JsonJtfEditor RootEditor { get; }
        public JToken Value { get; }

        public string Path => throw new NotImplementedException();

        public InvalidJsonItem(JToken value, JsonJtfEditor rootEditor)
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            AutoSize = false;
            Height = 32;
            AutoScaleMode = AutoScaleMode.None;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.Selectable, true);
            Value = value;
            RootEditor = rootEditor;

            JProperty? parent = value?.Parent as JProperty;
            if (parent is not null)
            {
                name = parent.Name;
            }

        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }
        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);


            Graphics g = e.Graphics;

            Color borderColor = RootEditor.ColorTable.InvalidBorderColor;

            if (Focused)
            {
                ControlPaint.DrawBorder(g, new Rectangle(0, 0, Width, Height), borderColor, 2, ButtonBorderStyle.Solid, borderColor, 2, ButtonBorderStyle.Solid, borderColor, 2, ButtonBorderStyle.Solid, borderColor, 2, ButtonBorderStyle.Solid);
                xOffset = 2;
                xRightOffset = 2;
                yOffset = 2;
                innerHeight = 28;
            }

            else
            {
                ControlPaint.DrawBorder(g, new Rectangle(0, 0, Width, Height), borderColor, ButtonBorderStyle.Solid);
                xOffset = 1;
                xRightOffset = 1;
                yOffset = 1;
                innerHeight = 30;
            }


            if (!string.IsNullOrEmpty(name))
            {
                int x = xOffset;
                xOffset += 20;



                SizeF nameSize = g.MeasureString(name, Font);


                g.DrawString(name, Font, RootEditor.ColorTable.InvalidElementForeBrush, new PointF(xOffset, 16 - nameSize.Height / 2));
                xOffset += (int)nameSize.Width;



                xOffset += 20;
            }
            removeButtonBounds = new Rectangle(Width - xRightOffset - 30, yOffset, 30, innerHeight);
            if(!RootEditor.ReadOnly)
            {

                g.FillRectangle(RootEditor.ColorTable.RemoveItemButtonBackBrush, removeButtonBounds);

                g.DrawLine(RootEditor.ColorTable.RemoveItemButtonForePen, Width - 20, 12, Width - 12, 20);
                g.DrawLine(RootEditor.ColorTable.RemoveItemButtonForePen, Width - 12, 12, Width - 20, 20);
            }

            SizeF viewValueSize = g.MeasureString(Properties.Resources.ViewValue, Font);

            viewValueButtonBounds = new RectangleF(xOffset, 0, viewValueSize.Width + 20, 32);

            g.FillRectangle(RootEditor.ColorTable.DiscardInvalidValueButtonBackBrush, viewValueButtonBounds);


            g.DrawString(Properties.Resources.ViewValue, Font, RootEditor.ColorTable.DiscardInvalidValueButtonForeBrush, new PointF(xOffset + 10, 16 - viewValueSize.Height / 2));
            xOffset += (int)viewValueSize.Width;


        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (viewValueButtonBounds.Contains(e.Location) || removeButtonBounds.Contains(e.Location))
                Cursor = Cursors.Hand;
            else
                Cursor = Cursors.Default;
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (viewValueButtonBounds.Contains(e.Location))
                MessageBox.Show(this, Value.ToString(Newtonsoft.Json.Formatting.Indented), "View Value", MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);
            else if (removeButtonBounds.Contains(e.Location))
            {
                if (Parent is not BlockEditorItem parent || RootEditor.ReadOnly)
                    return;
                if (name is not null)
                {
                    ((JObject?)parent.Value)?.Property(name, StringComparison.Ordinal)?.Remove();
                    parent.Controls.Remove(this);

                }
            }
        }
    }
}
