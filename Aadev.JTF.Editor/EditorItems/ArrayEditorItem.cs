using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Aadev.JTF.Editor.ViewModels;
using Aadev.JTF.Nodes;

namespace Aadev.JTF.Editor.EditorItems;

internal partial class ArrayEditorItem : ContainerEditorItem
{
    private Rectangle addNewButtonBounds = Rectangle.Empty;
    private ContextMenuStrip? cmsPrefabSelect;

    private new JtArrayNode Node => (JtArrayNode)base.Node;
    public new JtArrayViewModel ViewModel => (JtArrayViewModel)base.ViewModel;
    internal ArrayEditorItem(JtArrayViewModel node, JsonJtfEditor rootEditor) : base(node, rootEditor) { }
    private void OnPrefabSelect_Click(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem control || control.Tag is not JtNode prefab || ViewModel.IsInvalidValueType)
            return;
        if (Node.SingleType)
            ViewModel.SinglePrefab = prefab;

        AddNewItem(prefab);
    }
    private void AddNewItem(JtNode prefab)
    {
        ViewModel.Expanded = true;

        JtNodeViewModel vm = ViewModel.AddNewItem(prefab);
        int y = Height - 5;
        (y, _) = CreateChildItem(vm, true, y);

        Height = y + 5;
    }
    private void RequestAddNewItem()
    {
        if (ViewModel.Root.IsReadOnly || ViewModel.IsInvalidValueType)
            return;
        if (Node.Prefabs.Nodes!.Count == 0 || (Node.MaxSize >= 0 && Node.MaxSize <= ViewModel.ValidValue.Count))
            return;

        if (Node.Prefabs.Nodes!.Count > 1)
        {
            if (Node.SingleType && ViewModel.SinglePrefab is not null)
            {
                AddNewItem(ViewModel.SinglePrefab);
                return;
            }

            InitContextMenu();
            cmsPrefabSelect!.Show(MousePosition);
            return;
        }

        AddNewItem(Node.Prefabs.Nodes![0]);
    }
    private void UpdateLayout(EditorItem bei)
    {
        SuspendLayout();
        int oy = bei.Top;
        int index = 0;
        if (Node.ContainerJsonType is JtContainerType.Array)
        {
            index = bei.ArrayIndex;
        }

        foreach (Control control in Controls)
        {
            if (control is not EditorItem ei || control.Top <= bei.Top)
                continue;
            if (control.Top != oy)
                control.Top = oy;
            oy += control.Height;
            oy += 5;
            if (Node.ContainerJsonType is JtContainerType.Array)
            {
                ei.ArrayIndex = index;
                index++;
            }
        }

        Height = oy + 5;
        ResumeLayout();
    }

    private (int, EditorItem) CreateChildItem(JtNodeViewModel vm, bool focus, int y)
    {
        EditorItem bei = Create(vm, RootEditor);

        bei.Location = new Point(Indent, y);
        bei.Width = Width - (2 * Indent);


        Controls.Add(bei);

        bei.HeightChanged += (bei) =>
        {
            SuspendLayout();
            int oy = bei.Top + bei.Height + 5;
            foreach (Control control in Controls)
            {
                if (control is not EditorItem || control.Top < bei.Top || control == bei)
                    continue;
                control.Top = oy;
                oy += control.Height;
                oy += 5;
            }

            Height = oy + 5;
            ResumeLayout();
        };

        if (focus)
            bei.Focus();

        if (bei.Height != 0)
        {
            y += bei.Height + 5;
        }

        return (y, bei);
    }

    internal void RemoveChild(EditorItem editorItem)
    {
        Focus();
        ViewModel.RemoveChild(editorItem.ViewModel);
        Controls.Remove(editorItem);
        UpdateLayout(editorItem);
    }
    protected override Color BorderColor
    {
        get
        {
            if (ViewModel.IsInvalidValueType)
                return Color.Red;
            if (Node.MaxSize >= 0 && Node.MaxSize < ViewModel.ValidValue.Count)
                return Color.Yellow;
            return base.BorderColor;
        }
    }
    protected override void PostDraw(Graphics g, ref DrawingBounds db)
    {
        if (ViewModel.IsInvalidValueType)
            return;

        int addWidth = (IsFocused && !Node.IsArrayPrefab) ? 29 : 30;
        addNewButtonBounds = new Rectangle(Width - db.xRightOffset - addWidth, db.yOffset, addWidth, db.innerHeight);
        if (!ViewModel.Root.IsReadOnly)
        {
            if (ViewModel.Expanded && !Node.IsDynamicName)
            {
                RectangleF bounds = new RectangleF(addNewButtonBounds.Location, addNewButtonBounds.Size);
                g.SmoothingMode = SmoothingMode.HighQuality;
                using GraphicsPath rectPath = new GraphicsPath();

                bounds.Offset(-0.5f, -0.5f);
                float w = bounds.X + bounds.Width;
                float h = bounds.Y + bounds.Height;
                rectPath.AddLine(bounds.X, bounds.Y, w, bounds.Y);
                rectPath.AddLine(w, bounds.Y, w, h);
                rectPath.AddArc(bounds.X, h - 10, 10, 10, 90, 90);
                g.FillPath(RootEditor.ColorTable.AddItemButtonBackBrush, rectPath);

                g.SmoothingMode = SmoothingMode.Default;
            }
            else
                g.FillRectangle(RootEditor.ColorTable.AddItemButtonBackBrush, addNewButtonBounds);
            g.DrawLine(RootEditor.ColorTable.RemoveItemButtonForePen, Width - addWidth - db.xRightOffset + 15, 8, Width - addWidth - db.xRightOffset + 15, 24);
            g.DrawLine(RootEditor.ColorTable.RemoveItemButtonForePen, Width - addWidth - db.xRightOffset + 7, 16, Width - addWidth - db.xRightOffset + 23, 16);
            db.xRightOffset += addWidth;
        }

        string msg;
        if (ViewModel.SinglePrefab is null)
            msg = string.Format(CultureInfo.CurrentCulture, Properties.Resources.ArrayElementsCount, Node.MaxSize >= 0 ? $"{ViewModel.ValidValue.Count}/{Node.MaxSize}" : ViewModel.ValidValue.Count.ToString(CultureInfo.CurrentCulture));
        else
            msg = string.Format(CultureInfo.CurrentCulture, Properties.Resources.ArrayElementsCountOfType, Node.MaxSize >= 0 ? $"{ViewModel.ValidValue.Count}/{Node.MaxSize}" : ViewModel.ValidValue.Count.ToString(CultureInfo.CurrentCulture), ViewModel.SinglePrefab.Type.DisplayName);
        SizeF msgSize = g.MeasureString(msg, Font);

        g.DrawString(msg, Font, RootEditor.ColorTable.NameForeBrush, new PointF(Width - db.xRightOffset - 10 - msgSize.Width, 16 - (msgSize.Height / 2)));

        db.xRightOffset += (int)msgSize.Width;
    }
    internal override void OnExpandChanged()
    {
        Focus(); // To unfocus dynamic name textbox of child
        if (ViewModel.IsInvalidValueType)
            return;
        SuspendLayout();
        if (!ViewModel.Expanded)
        {
            DestroyFocusPanel();
            Controls.Clear();

            Height = 32;
            ResumeLayout();
            return;
        }

        CreateFocusPanel();

        int y = 38;
        Span<JtNodeViewModel> childrenSpan = CollectionsMarshal.AsSpan(ViewModel.GetChildren());
        for (int i = 0; i < childrenSpan.Length; i++)
        {
            (y, _) = CreateChildItem(childrenSpan[i], false, y);
        }

        Height = y + 5;

        ResumeLayout();
    }
    private void InitContextMenu()
    {
        if (cmsPrefabSelect is not null || Node.Prefabs.Nodes!.Count <= 1)
            return;
        cmsPrefabSelect = new ContextMenuStrip();
        Span<JtNode> collectionSpan = CollectionsMarshal.AsSpan(Node.Prefabs.Nodes!);
        for (int i = 0; i < collectionSpan.Length; i++)
        {
            JtNode item = collectionSpan[i];
            ToolStripMenuItem? tsmi = new ToolStripMenuItem() { Tag = item };

            if (item.Name is null)
                tsmi.Text = item.Type.DisplayName;
            else
                tsmi.Text = $"{item.Name} ({item.Type.DisplayName})";

            if (Properties.Resources.ResourceManager.GetObject(item.Type.Name, CultureInfo.InvariantCulture) is Bitmap bmp)
                tsmi.Image = bmp;

            tsmi.BackColor = Color.FromArgb(80, 80, 80);
            tsmi.ForeColor = Color.White;
            tsmi.ImageTransparentColor = Color.FromArgb(80, 80, 80);
            tsmi.ImageScaling = ToolStripItemImageScaling.SizeToFit;
            tsmi.Click += OnPrefabSelect_Click;
            cmsPrefabSelect.Items.Add(tsmi);
        }

        cmsPrefabSelect.BackColor = Color.FromArgb(80, 80, 80);
        cmsPrefabSelect.ForeColor = Color.White;
        cmsPrefabSelect.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);

        if (ViewModel.IsInvalidValueType)
            return;

        if (e.Button != MouseButtons.Left)
            return;

        if (addNewButtonBounds.Contains(e.Location))
        {
            RequestAddNewItem();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.N && e.Control)
        {
            RequestAddNewItem();
        }
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (ViewModel.IsInvalidValueType || ViewModel.Root.IsReadOnly)
            return;
        if (addNewButtonBounds.Contains(e.Location))
        {
            if (Node.Prefabs.Nodes!.Count == 0 || (Node.MaxSize >= 0 && Node.MaxSize <= ViewModel.ValidValue.Count))
                Cursor = Cursors.No;
            else
                Cursor = Cursors.Hand;
            return;
        }
    }
}