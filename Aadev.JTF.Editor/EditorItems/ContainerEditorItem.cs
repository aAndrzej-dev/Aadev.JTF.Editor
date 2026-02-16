using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Aadev.JTF.Editor.ViewModels;

namespace Aadev.JTF.Editor.EditorItems;

internal abstract class ContainerEditorItem : EditorItem
{
    protected const int Indent = 10;
    private FocusableControl? focusPanel;
    private Rectangle expandButtonBounds = Rectangle.Empty;
    protected override bool IsFocused => base.IsFocused || focusPanel?.Focused is true;
    public bool CanDrawExpandButton => !ViewModel.IsInvalidValueType && !ViewModel.AlwaysExpanded && !(RootEditor.ViewModel.IsReadOnly && !ViewModel.IsSavable);
    public new JtContainerViewModel ViewModel => (JtContainerViewModel)base.ViewModel;

    public ContainerEditorItem(JtContainerViewModel viewModel, JsonJtfEditor rootEditor) : base(viewModel, rootEditor)
    {
        SetStyle(ControlStyles.ContainerControl, true);

        if (ViewModel is JtContainerViewModel cvm)
        {
            cvm.ExpandChanged += s =>
            {
                RootEditor.SuspendSrollingToControl = true;
                SuspendFocus = true;
                OnExpandChanged();
                RootEditor.SuspendSrollingToControl = false;
                SuspendFocus = false;
            };
        }
    }

    internal abstract void OnExpandChanged();

    /// <summary>
    /// Removes references to focus panel but not remove from Controls.
    /// </summary>
    protected void DestroyFocusPanel()
    {
        focusPanel = null;
    }

    protected void CreateFocusPanel()
    {
        focusPanel = new FocusableControl();

        focusPanel.GotFocus += (s, e) =>
        {
            if (Node.IsDynamicName)
            {
                if (txtDynamicName is null)
                    CreateDynamicNameTextBox();
                else
                    txtDynamicName.Focus();
            }
            else
            {
                Invalidate();
            }
        };
        focusPanel.LostFocus += (s, e) => Invalidate();
        Controls.Add(focusPanel);
        focusPanel?.Focus();
    }
    public void DeepExpand()
    {
        SuspendFocus = true;
        foreach (Control item in Controls)
        {
            if (item is not ContainerEditorItem cei || !(cei.ViewModel.ValidValue?.Count > 0))
                continue;

            cei.ViewModel.Expanded = true;
            cei.DeepExpand();
        }

        SuspendFocus = false;
    }
    protected override void DrawExpandButton(Graphics g, bool isLastInTwinFamily, ref DrawingBounds db)
    {
        if (!CanDrawExpandButton)
        {
            return;
        }

        expandButtonBounds = new Rectangle(db.xOffset, db.yOffset, 30, db.innerHeight);

        if (ViewModel.Expanded && !Node.IsDynamicName)
        {
            RectangleF bounds = new RectangleF(expandButtonBounds.Location, expandButtonBounds.Size);
            g.SmoothingMode = SmoothingMode.HighQuality;
            using GraphicsPath rectPath = new GraphicsPath();

            bounds.Offset(-0.5f, -0.5f);
            float w = bounds.X + bounds.Width;
            float h = bounds.Y + bounds.Height;
            rectPath.AddLine(bounds.X, bounds.Y, w, bounds.Y);
            rectPath.AddArc(w - 10, h - 10, 10, 10, 0, 90);
            if (isLastInTwinFamily)
            {
                rectPath.AddLine(bounds.X, h, bounds.X, bounds.Y);
            }
            else
            {
                rectPath.AddArc(bounds.X, h - 10, 10, 10, 90, 90);
            }

            g.FillPath(RootEditor.ColorTable.ExpandButtonBackBrush, rectPath);

            g.SmoothingMode = SmoothingMode.Default;

        }
        else
        {
            g.FillRectangle(RootEditor.ColorTable.ExpandButtonBackBrush, expandButtonBounds);
        }

        g.SmoothingMode = SmoothingMode.HighQuality;
        RectangleF innerRectBounds = new RectangleF(db.xOffset + 7, 8, 16, 16);
        using GraphicsPath innerRectPath = new GraphicsPath();

        float iw = innerRectBounds.X + innerRectBounds.Width;
        float ih = innerRectBounds.Y + innerRectBounds.Height;

        innerRectPath.AddArc(innerRectBounds.X, innerRectBounds.Y, 4, 4, 180, 90);
        innerRectPath.AddArc(iw - 4, innerRectBounds.Y, 4, 4, 270, 90);
        innerRectPath.AddArc(iw - 4, ih - 4, 4, 4, 0, 90);

        innerRectPath.AddArc(innerRectBounds.X, ih - 4, 4, 4, 90, 90);
        innerRectPath.CloseFigure();
        g.DrawPath(RootEditor.ColorTable.ExpandButtonForePen, innerRectPath);
        g.SmoothingMode = SmoothingMode.Default;


        if (ViewModel.Expanded)
        {
            g.DrawLine(RootEditor.ColorTable.ExpandButtonForePen, db.xOffset + 12, 16, db.xOffset + 18, 16);
        }
        else
        {
            g.DrawLine(RootEditor.ColorTable.ExpandButtonForePen, db.xOffset + 12, 16, db.xOffset + 18, 16);
            g.DrawLine(RootEditor.ColorTable.ExpandButtonForePen, db.xOffset + 15, 12, db.xOffset + 15, 20);
        }

        db.xOffset += 30;
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (ViewModel.Expanded)
            focusPanel?.Focus();
    }
    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (expandButtonBounds.Contains(e.Location))
        {
            if (ViewModel is not JtContainerViewModel cvm)
            {
                return;
            }

            cvm.Expanded = !cvm.Expanded;
            if (ModifierKeys is Keys.Shift && cvm.Expanded)
            {
                DeepExpand();
            }

            return;
        }
    }
    protected override void OnDraw(Graphics g, ref DrawingBounds db)
    {
        if (ViewModel is JtContainerViewModel cvm && cvm.AlwaysExpanded)
        {
            cvm.Expanded = true;
            if (cvm.Node.IsRootChild && cvm.Node.ContainerDisplayType is JtContainerType.Block && !ViewModel.IsInvalidValueType && Node.Template.Roots.Count == 1)
            {   // Hide borders of top level block item
                db.xOffset = 0;
                db.xRightOffset = 0;
                db.yOffset = 0;
                db.innerHeight = 32;
                return;
            }
        }
        base.OnDraw(g, ref db);
    }
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        int w = Width - 20;

        foreach (Control item in Controls)
        {
            if (item.Width != w)
                item.Width = w;
        }
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (expandButtonBounds.Contains(e.Location))
        {
            Cursor = Cursors.Hand;
        }
    }
    protected override void OnGotFocus(EventArgs e)
    {
        if (expandButtonBounds.Contains(PointToClient(MousePosition)) && MouseButtons == MouseButtons.Left)
            suspendCreatingDynamicTextBoxUntilNewFocus = true;
        base.OnGotFocus(e);
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (ViewModel.IsInvalidValueType)
        {
            return;
        }

        if (e.KeyCode == Keys.Space)
        {
            ViewModel.Expanded = !ViewModel.Expanded;
        }
    }
    protected class FocusableControl : Control
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, nint wParam, nint lParam);
        public FocusableControl()
        {
            SetStyle(ControlStyles.Selectable, true);
            Height = 0;
            Width = 0;
            Top = 0;
            Left = 0;
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (Parent is null)
                return;
            SendMessage(Parent.Handle, 0x0100, (nint)e.KeyCode, 0);
        }
    }
}
