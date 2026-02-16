using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Aadev.JTF.Editor.ViewModels;
using Newtonsoft.Json.Linq;

namespace Aadev.JTF.Editor.EditorItems;

internal abstract class EditorItem : ContainerControl, IJsonItem
{
    private int oldHeight;
    private Rectangle removeButtonBounds = Rectangle.Empty;
    private Rectangle discardInvalidTypeButtonBounds = Rectangle.Empty;
    private Rectangle dynamicNameTextboxBounds = Rectangle.Empty;
    private Rectangle nameLabelBounds = Rectangle.Empty;
    private Rectangle twinFamilyButtonBounds = Rectangle.Empty;

    protected bool suspendCreatingDynamicTextBoxUntilNewFocus;
    protected TextBox? txtDynamicName;

    public JtNodeViewModel ViewModel { get; }
    protected JsonJtfEditor RootEditor { get; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool SuspendFocus { get; set; } //To prevent creating text boxes
    protected virtual bool IsFocused => Focused || txtDynamicName?.Focused is true;
    protected virtual Color BorderColor
    {
        get
        {
            if (ViewModel.IsInvalidValueType)
                return RootEditor.ColorTable.InvalidBorderColor;

            if (Parent is ArrayEditorItem aei && aei.ViewModel.SinglePrefab is not null && aei.ViewModel.SinglePrefab != Node)
                return RootEditor.ColorTable.WarningBorderColor;

            if (IsFocused)
                return RootEditor.ColorTable.ActiveBorderColor;

            return RootEditor.ColorTable.InactiveBorderColor;
        }
    }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal int ArrayIndex { get => ViewModel.ArrayIndex; set => ViewModel.ArrayIndex = value; }
    public JtNode Node => ViewModel.Node;
    JToken IJsonItem.Value => ViewModel.Value;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string? DynamicName { get => ViewModel.DynamicName; set => ViewModel.DynamicName = value; }

    internal event EditorItemEventHandler? HeightChanged;
    private protected EditorItem(JtNodeViewModel viewModel, JsonJtfEditor rootEditor)
    {
        SuspendLayout();
        ViewModel = viewModel;
        RootEditor = rootEditor;

        ViewModel.ValueChanged += (s, e) => Invalidate();

        oldHeight = Height;
        SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor, true);
        Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
        Margin = new Padding(0);
        Name = "EditorItem";
        Size = new Size(500, 1);
        AutoScaleMode = AutoScaleMode.None;
        Height = 32;
        TabStop = true;

        ResumeLayout(false);
    }

    protected void CreateDynamicNameTextBox()
    {
        if (txtDynamicName is not null)
            return;

        if (SuspendFocus)
        {
            return;
        }

        txtDynamicName = new TextBox
        {
            Font = Font,
            BorderStyle = BorderStyle.None,
            BackColor = RootEditor.ColorTable.TextBoxBackColor,
            ForeColor = RootEditor.ColorTable.TextBoxForeColor,
            AutoSize = false,
            TabIndex = 0,

            Text = DynamicName,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            ReadOnly = ViewModel.Root.IsReadOnly
        };

        txtDynamicName.Location = new Point(dynamicNameTextboxBounds.X + 10, 16 - (txtDynamicName.Height / 2));
        txtDynamicName.Width = dynamicNameTextboxBounds.Width - 20;
        txtDynamicName.TextChanged += (sender, eventArgs) => ViewModel.OnDynamicNamePreviewChange();
        txtDynamicName.LostFocus += (sender, eventArgs) =>
        {
            if (txtDynamicName is null)
            {
                return;
            }

            if (DynamicName != txtDynamicName.Text && !ViewModel.Root.IsReadOnly)
            {
                string? oldDynamicName = DynamicName;
                DynamicName = txtDynamicName.Text;
                ViewModel.OnValueChanged(new JtfEditorAction(JtfEditorAction.JtEditorActionType.DynamicNameChanged, oldDynamicName, DynamicName, ViewModel));
            }
            else
            {
                Invalidate();
            }

            Controls.Remove(txtDynamicName);
            txtDynamicName = null;
        };
        txtDynamicName.GotFocus += (sender, e) => Invalidate();

        txtDynamicName.KeyDown += (sender, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                suspendCreatingDynamicTextBoxUntilNewFocus = true;
                Controls.Remove(txtDynamicName);
                e.Handled = true;
            }
        };
        Controls.Add(txtDynamicName);
        txtDynamicName?.Focus();
        txtDynamicName?.SelectAll();
    }

    protected virtual void InitDraw(Graphics g, ref DrawingBounds db)
    {
        Color borderColor = BorderColor;

        if (IsFocused)
        {
            ControlPaint.DrawBorder(g, new Rectangle(0, 0, Width, Height), borderColor, 2, ButtonBorderStyle.Solid, borderColor, 2, ButtonBorderStyle.Solid, borderColor, 2, ButtonBorderStyle.Solid, borderColor, 2, ButtonBorderStyle.Solid);
            db.xOffset = 2;
            db.xRightOffset = 2;
            db.yOffset = 2;
            if (ViewModel is JtContainerViewModel cvm && cvm.Expanded)
                db.innerHeight = 29;
            else
                db.innerHeight = 28;
        }
        else
        {
            ControlPaint.DrawBorder(g, new Rectangle(0, 0, Width, Height), borderColor, ButtonBorderStyle.Solid);
            db.xOffset = 1;
            db.xRightOffset = 1;
            db.yOffset = 1;
            db.innerHeight = 30;
        }
    }
    private void DrawInvalidValueMessage(Graphics g, ref DrawingBounds db)
    {
        if (!ViewModel.IsInvalidValueType)
        {
            return;
        }

        string message = string.Format(CultureInfo.CurrentCulture, Properties.Resources.InvalidValueType, ViewModel.Value.Type, Node.JsonType);

        SizeF sf = g.MeasureString(message, Font);
        g.DrawString(message, Font, RootEditor.ColorTable.InvalidValueBrush, new PointF(db.xOffset + 10, 16 - (sf.Height / 2)));

        db.xOffset += (int)sf.Width + 20;

        string discardMessage = Properties.Resources.DiscardInvalidType;

        SizeF dsf = g.MeasureString(discardMessage, Font);

        discardInvalidTypeButtonBounds = new Rectangle(db.xOffset, db.yOffset, (int)dsf.Width + 10, db.innerHeight);
        g.FillRectangle(RootEditor.ColorTable.DiscardInvalidValueButtonBackBrush, discardInvalidTypeButtonBounds);
        g.DrawString(discardMessage, Font, RootEditor.ColorTable.DiscardInvalidValueButtonForeBrush, db.xOffset + 5, 16 - (dsf.Height / 2));

        db.xOffset += (int)sf.Width + 20;
    }
    private void DrawDynamicName(Graphics g, ref DrawingBounds db)
    {
        if (!Node.IsDynamicName)
        {
            return;
        }

        if (ViewModel is JtContainerViewModel)
        {
            dynamicNameTextboxBounds = new Rectangle(db.xOffset, db.yOffset, Width - db.xOffset - db.xRightOffset, db.innerHeight);
            g.FillRectangle(RootEditor.ColorTable.TextBoxBackBrush, dynamicNameTextboxBounds);

            if (txtDynamicName is not null)
            {
                return;
            }

            SizeF sf = g.MeasureString(DynamicName, Font);

            g.DrawString(DynamicName, Font, RootEditor.ColorTable.TextBoxForeBrush, new PointF(db.xOffset + 10, 16 - (sf.Height / 2)));
        }
        else
        {
            SizeF s = g.MeasureString(":", Font);
            int size = (Width - db.xOffset - (int)s.Width - 10 - db.xRightOffset) / 2;

            dynamicNameTextboxBounds = new Rectangle(db.xOffset, db.yOffset, size, db.innerHeight);
            g.FillRectangle(RootEditor.ColorTable.TextBoxBackBrush, dynamicNameTextboxBounds);
            if (txtDynamicName is null)
            {

                SizeF sf = g.MeasureString(DynamicName, Font);

                g.DrawString(DynamicName, Font, RootEditor.ColorTable.TextBoxForeBrush, new PointF(db.xOffset + 10, 16 - (sf.Height / 2)));
            }

            db.xOffset += size;


            g.DrawString(":", Font, RootEditor.ColorTable.NameForeBrush, new PointF(db.xOffset + 5, 16 - (s.Height / 2)));

            db.xOffset += (int)s.Width + 10;

        }
    }
    private void DrawRemoveButton(Graphics g, ref DrawingBounds db)
    {
        if (!Node.IsArrayPrefab || RootEditor.ViewModel.IsReadOnly)
        {
            return;
        }

        int width = IsFocused ? 29 : 30;
        removeButtonBounds = new Rectangle(Width - db.xRightOffset - width, db.yOffset, width, db.innerHeight);
        if (ViewModel is JtBlockViewModel bvm && bvm.Expanded && !Node.IsDynamicName)
        {
            RectangleF bounds = new RectangleF(removeButtonBounds.Location, removeButtonBounds.Size);
            g.SmoothingMode = SmoothingMode.HighQuality;
            using GraphicsPath rectPath = new GraphicsPath();

            bounds.Offset(-0.5f, -0.5f);
            float w = bounds.X + bounds.Width;
            float h = bounds.Y + bounds.Height;
            rectPath.AddLine(bounds.X, bounds.Y, w, bounds.Y);
            rectPath.AddLine(w, bounds.Y, w, h);
            rectPath.AddArc(bounds.X, h - 10, 10, 10, 90, 90);
            g.FillPath(RootEditor.ColorTable.RemoveItemButtonBackBrush, rectPath);

            g.SmoothingMode = SmoothingMode.Default;
        }
        else
        {
            g.FillRectangle(RootEditor.ColorTable.RemoveItemButtonBackBrush, removeButtonBounds);
        }

        g.DrawLine(RootEditor.ColorTable.RemoveItemButtonForePen, Width - 20, 12, Width - 12, 20);
        g.DrawLine(RootEditor.ColorTable.RemoveItemButtonForePen, Width - 12, 12, Width - 20, 20);

        db.xRightOffset += width;
    }
    private void DrawName(Graphics g, ref DrawingBounds db)
    {
        if (string.IsNullOrEmpty(Node.DisplayName) && ArrayIndex == -1)
            return;

        int x = db.xOffset;
        db.xOffset += ArrayIndex != -1 ? 10 : 20;

        SizeF nameSize = g.MeasureString(ViewModel.FriendlyDisplayName, Font);

        g.DrawString(ViewModel.FriendlyDisplayName, Font, ViewModel.IsSavable ? RootEditor.ColorTable.NameForeBrush : RootEditor.ColorTable.DefaultElementForeBrush, new PointF(db.xOffset, 16 - (nameSize.Height / 2)));

        db.xOffset += (int)nameSize.Width;

        if (Node.Required)
        {
            g.DrawString("*", Font, RootEditor.ColorTable.RequiredStarBrush, new PointF(db.xOffset, 16 - (nameSize.Height / 2)));
        }

        db.xOffset += ArrayIndex != -1 ? 10 : 20;
        nameLabelBounds = new Rectangle(x, 1, db.xOffset - x, 30);
    }
    protected virtual void DrawExpandButton(Graphics g, bool isLastInTwinFamily, ref DrawingBounds db) { }
    protected virtual void PostDraw(Graphics g, ref DrawingBounds db) { }
    private bool DrawTypeIcons(Graphics g, ref DrawingBounds db)
    {
        int startX = db.xOffset;
        bool drawRounded = ViewModel is JtContainerViewModel { Expanded: true };

        void DrawSingleType(JtNodeViewModel item, int i, bool isLastNode, bool isFirstNode, ref DrawingBounds db)
        {
            if (Properties.Resources.ResourceManager.GetObject(item.Node.Type.Name, CultureInfo.InvariantCulture) is not Bitmap bmp)
            {
                return;
            }

            int width = i == 0 && IsFocused ? 29 : 30;
            if (ViewModel == item)
            {
                if (drawRounded)
                {
                    RectangleF bounds = new RectangleF(db.xOffset, db.yOffset, width, db.innerHeight);
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    using GraphicsPath rectPath = new GraphicsPath();

                    bounds.Offset(-0.5f, -0.5f);
                    float w = bounds.X + bounds.Width;
                    float h = bounds.Y + bounds.Height;
                    rectPath.AddLine(bounds.X, bounds.Y, w, bounds.Y);


                    if (!isLastNode)
                    {
                        rectPath.AddArc(w - 10, h - 10, 10, 10, 0, 90);
                    }
                    else
                    {
                        rectPath.AddLine(w, bounds.Y, w, h);
                    }

                    if (isFirstNode)
                    {
                        rectPath.AddLine(bounds.X, h, bounds.X, bounds.Y);
                    }
                    else
                    {
                        rectPath.AddArc(bounds.X, h - 10, 10, 10, 90, 90);
                    }

                    g.FillPath(RootEditor.ColorTable.SelectedNodeTypeBackBrush, rectPath);

                    g.SmoothingMode = SmoothingMode.Default;
                }
                else
                {
                    g.FillRectangle(RootEditor.ColorTable.SelectedNodeTypeBackBrush, db.xOffset, db.yOffset, width, db.innerHeight);
                }
            }

            g.DrawImage(bmp, db.xOffset + (i == 0 && IsFocused ? 7 : 8), 8, 16, 16);

            db.xOffset += width;
        }


        ReadOnlySpan<JtNodeViewModel> twinFamilySpan = CollectionsMarshal.AsSpan(ViewModel.TwinFamily?.members);
        if (twinFamilySpan.Length == 0)
        {
            DrawSingleType(ViewModel, 0, true, true, ref db);
            twinFamilyButtonBounds = new Rectangle(startX, db.yOffset, db.xOffset - startX, db.innerHeight);
            return true;
        }



        bool isLastNode = false;

        for (int i = twinFamilySpan.Length - 1; i >= 0; i--)
        {
            if (twinFamilySpan[i] == ViewModel)
            {
                isLastNode = true;
                break;
            }

            if (twinFamilySpan[i].IsConditionMet)
                break;
        }

        bool isFirstNode = false;
        bool isFirstNodeSet = false;
        for (int i = 0; i < twinFamilySpan.Length; i++)
        {
            if (!isFirstNodeSet && twinFamilySpan[i] == ViewModel)
            {
                isFirstNode = true;
                isFirstNodeSet = true;
            }

            if (twinFamilySpan[i].IsConditionMet)
            {
                isFirstNodeSet = true;
            }
            else
                continue;

            JtNodeViewModel item = twinFamilySpan[i];
            if (Node.IsArrayPrefab && ViewModel != item)
            {
                continue;
            }

            DrawSingleType(item, i, isLastNode, isFirstNode, ref db);
        }

        twinFamilyButtonBounds = new Rectangle(startX, db.yOffset, db.xOffset - startX, db.innerHeight);

        return isLastNode;
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);

        if (Node.IsDynamicName)
        {
            if (txtDynamicName is null)
            {
                if (suspendCreatingDynamicTextBoxUntilNewFocus)
                {
                    suspendCreatingDynamicTextBoxUntilNewFocus = false;
                    return;
                }

                CreateDynamicNameTextBox();
            }
            else
            {
                txtDynamicName.Focus();
            }
        }

        Invalidate();
    }
    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;

        if (ArrayIndex != -1)
        {
            TabIndex = ArrayIndex;
        }
        DrawingBounds drawingBounds = new DrawingBounds();
        OnDraw(g, ref drawingBounds);
    }
    protected virtual void OnDraw(Graphics g, ref DrawingBounds db)
    {
        InitDraw(g, ref db);
        bool isLast = DrawTypeIcons(g, ref db);
        DrawExpandButton(g, isLast, ref db);
        DrawName(g, ref db);
        DrawInvalidValueMessage(g, ref db);
        DrawRemoveButton(g, ref db);
        DrawDynamicName(g, ref db);
        PostDraw(g, ref db);
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
    }
    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);

        if (e.Button != MouseButtons.Left)
        {
            Focus();
            return;
        }



        if (removeButtonBounds.Contains(e.Location))
        {
            if (txtDynamicName is not null)
            {
                if (DynamicName != txtDynamicName.Text && !ViewModel.Root.IsReadOnly)
                {
                    string? oldDynamicName = DynamicName;
                    DynamicName = txtDynamicName.Text;
                    ViewModel.OnValueChanged(new JtfEditorAction(JtfEditorAction.JtEditorActionType.DynamicNameChanged, oldDynamicName, DynamicName, ViewModel));
                }
                else
                {
                    Invalidate();
                }

                Controls.Remove(txtDynamicName);
                txtDynamicName = null;
            }

            if (Parent is not ArrayEditorItem parent)
            {
                throw new Exception();
            }

            parent.RemoveChild(this);


            return;
        }

        if (discardInvalidTypeButtonBounds.Contains(e.Location))
        {
            ViewModel.CreateValue();

            if (ViewModel is JtContainerViewModel cvm && cvm.AlwaysExpanded)
            {
                if (this is ContainerEditorItem cei)
                {
                    cei.OnExpandChanged();
                    return;
                }
            }

            return;
        }

        if (dynamicNameTextboxBounds.Contains(e.Location))
        {
            CreateDynamicNameTextBox();
            return;
        }

        if (ViewModel.TwinFamily is not null && twinFamilyButtonBounds.Contains(e.Location))
        {
            JtNodeViewModel newType = ViewModel.TwinFamily[e.Location.X / 30];
            ViewModel.TwinFamily?.RequestChange(newType);
            return;
        }
    }
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (oldHeight == Height)
        {
            return;
        }

        oldHeight = Height;
        HeightChanged?.Invoke(this);
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (nameLabelBounds.Contains(e.Location))
        {
            Cursor = Cursors.Help;
            if (RootEditor.ToolTip.Active)
            {
                return;
            }

            RootEditor.ToolTip.Active = true;
            RootEditor.ToolTip.Show(ViewModel.ToolTipText, this);
            return;
        }
        else if (RootEditor.ToolTip.Active)
        {
            RootEditor.ToolTip.Active = false;
            RootEditor.ToolTip.Hide(this);
        }

        if (!ViewModel.Root.IsReadOnly && (removeButtonBounds.Contains(e.Location) || discardInvalidTypeButtonBounds.Contains(e.Location) || twinFamilyButtonBounds.Contains(e.Location)))
        {
            Cursor = Cursors.Hand;
        }
        else if (dynamicNameTextboxBounds.Contains(e.Location) && !ViewModel.Root.IsReadOnly)
        {
            Cursor = Cursors.IBeam;
        }
        else
        {
            Cursor = Cursors.Default;
        }
    }


    public static EditorItem Create(JtNodeViewModel viewModel, JsonJtfEditor rootEditor)
    {
        return viewModel switch
        {
            JtBoolViewModel boolNode => new BoolEditorItem(boolNode, rootEditor),
            JtValueViewModel valueNode => new ValueEditorItem(valueNode, rootEditor),
            JtBlockViewModel blockNode => new BlockEditorItem(blockNode, rootEditor),
            JtArrayViewModel arrayNode => new ArrayEditorItem(arrayNode, rootEditor),
            _ => new UnknownEditorItem(viewModel, rootEditor)
        };
    }
}