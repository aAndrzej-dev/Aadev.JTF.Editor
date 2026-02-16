using System;
using System.Drawing;
using System.Windows.Forms;
using Aadev.JTF.Editor.ViewModels;
using Aadev.JTF.Nodes;

namespace Aadev.JTF.Editor.EditorItems;

internal sealed class BoolEditorItem : EditorItem
{
    private Rectangle falsePanelRect = Rectangle.Empty;
    private Rectangle truePanelRect = Rectangle.Empty;

    public new JtBoolViewModel ViewModel => (JtBoolViewModel)base.ViewModel;
    private new JtBoolNode Node => (JtBoolNode)base.Node;

    internal BoolEditorItem(JtBoolViewModel node, JsonJtfEditor rootEditor) : base(node, rootEditor) { }

    protected override void PostDraw(Graphics g, ref DrawingBounds db)
    {
        if (ViewModel.IsInvalidValueType)
            return;
        if (IsFocused)
            db.xOffset++;
        int width = Width - db.xOffset - db.xRightOffset;


        if (Node.Constant)
        {
            if (Node.Default)
            {
                truePanelRect = new Rectangle(db.xOffset, db.yOffset, width, db.innerHeight);
                falsePanelRect = Rectangle.Empty;
            }
            else
            {
                truePanelRect = Rectangle.Empty;
                falsePanelRect = new Rectangle(db.xOffset, db.yOffset, width, db.innerHeight);
            }
        }
        else
        {
            double halfWidth = width * 0.5;
            falsePanelRect = new Rectangle(db.xOffset, db.yOffset, (int)halfWidth, db.innerHeight);
            truePanelRect = new Rectangle(db.xOffset + (int)halfWidth, db.yOffset, (int)Math.Ceiling(halfWidth), db.innerHeight);
        }


        if (ViewModel.RawValue ?? Node.Default)
            g.FillRectangle(RootEditor.ColorTable.TrueValueBackBrush, truePanelRect);
        else
            g.FillRectangle(RootEditor.ColorTable.FalseValueBackBrush, falsePanelRect);

        if (!falsePanelRect.IsEmpty)
        {
            SizeF falseLabelSize = g.MeasureString(Properties.Resources.FalseValue, Font);

            g.DrawString(Properties.Resources.FalseValue, Font, (ViewModel.RawValue ?? Node.Default) ? RootEditor.ColorTable.NameForeBrush : RootEditor.ColorTable.FalseValueForeBrush, falsePanelRect.X + (falsePanelRect.Width / 2) - (falseLabelSize.Width / 2), falsePanelRect.Y + (falsePanelRect.Height / 2) - (falseLabelSize.Height / 2));
        }

        if (!truePanelRect.IsEmpty)
        {
            SizeF trueLabelSize = g.MeasureString(Properties.Resources.TrueValue, Font);

            g.DrawString(Properties.Resources.TrueValue, Font, (ViewModel.RawValue ?? Node.Default) ? RootEditor.ColorTable.TrueValueForeBrush : RootEditor.ColorTable.NameForeBrush, truePanelRect.X + (truePanelRect.Width / 2) - (trueLabelSize.Width / 2), truePanelRect.Y + (truePanelRect.Height / 2) - (trueLabelSize.Height / 2));
        }
    }
    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);

        if (ViewModel.IsInvalidValueType || ViewModel.Root.IsReadOnly)
            return;


        if (falsePanelRect.Contains(e.Location))
        {
            ViewModel.RawValue = false;
            Focus();
            return;
        }

        if (truePanelRect.Contains(e.Location))
        {
            ViewModel.RawValue = true;
            Focus();
            return;
        }
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (ViewModel.IsInvalidValueType || ViewModel.Root.IsReadOnly)
            return;
        if (e.KeyCode == Keys.Space)
        {
            ViewModel.RawValue  = (bool?)ViewModel.Value is false;
        }
    }
}