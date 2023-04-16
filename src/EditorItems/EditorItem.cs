using Aadev.ConditionsInterpreter;
using Aadev.JTF.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Aadev.JTF.Editor.EditorItems
{
    internal abstract partial class EditorItem : UserControl, IJsonItem
    {
        private readonly JtNode[] twinsFamily;
        protected string toolTipText;
        private string? dynamicName;
        private int oldHeight;
        private bool expanded;
        private Rectangle expandButtonBounds = Rectangle.Empty;
        private Rectangle removeButtonBounds = Rectangle.Empty;
        private Rectangle discardInvalidTypeButtonBounds = Rectangle.Empty;
        private Rectangle dynamicNameTextboxBounds = Rectangle.Empty;
        private Rectangle nameLabelBounds = Rectangle.Empty;
        private Rectangle twinFamilyButtonBounds;


        protected readonly EventManager eventManager;
        protected TextBox? txtDynamicName;
        protected int xOffset;
        protected int xRightOffset;
        protected int yOffset;
        protected int innerHeight;


        internal virtual bool IsInvalidValueType => Value.Type != Node.JsonType;
        protected virtual bool IsFocused => Focused || txtDynamicName?.Focused is true;
        protected JsonJtfEditor RootEditor { get; }
        protected SolidBrush ForeColorBrush { get; private set; }
        protected bool CanCollapse => Node is JtContainerNode c && !c.DisableCollapse;
        protected bool Expanded { get => expanded; set { if (expanded == value) return; expanded = !CanCollapse || value; RootEditor.DisableScrollingToControl = true; SuspendFocus = true; OnExpandChanged(); SuspendFocus = false; RootEditor.DisableScrollingToControl = false; } }
        protected virtual Color BorderColor
        {
            get
            {
                if (IsInvalidValueType)
                    return RootEditor.ColorTable.InvalidBorderColor;
                else if (Parent is ArrayEditorItem aei && aei.SinglePrefab is not null && aei.SinglePrefab != Node)
                    return RootEditor.ColorTable.WarningBorderColor;
                else if (IsFocused)
                    return RootEditor.ColorTable.ActiveBorderColor;
                else
                    return RootEditor.ColorTable.InactiveBorderColor;
            }
        }

        internal int ArrayIndex { get; set; } = -1;
        internal virtual bool IsSavable => Node.Required || Node.Parent?.Owner is { ContainerDisplayType: JtContainerType.Block, ContainerJsonType: JtContainerType.Array } || Node.IsRoot || Node.IsArrayPrefab;
        internal bool SuspendFocus { get; private set; }

        public JtNode Node { get; }
        public abstract JToken Value { get; set; }
        public string? DynamicName { get => dynamicName; set { dynamicName = value; Invalidate(); } }

        public event EventHandler<ValueChangedEventArgs>? ValueChanged;
        public event EventHandler<DynamicNamePreviewChangeEventArgs>? DynamicNamePreviewChange;
        internal event EventHandler<TwinChangedEventArgs>? TwinTypeChanged;
        internal event EventHandler? HeightChanged;

        private protected EditorItem(JtNode node, JToken? token, JsonJtfEditor rootEditor, EventManager eventManager)
        {
            Node = node;
            RootEditor = rootEditor;
            Value = token is null || token.Type is JTokenType.Null ? Node.CreateDefaultValue() : token;
            this.eventManager = eventManager;
            twinsFamily = RootEditor.NormalizeTwinNodeOrder ? Node.GetTwinFamily().OrderBy(x => x.Type.Id).ToArray() : Node.GetTwinFamily().ToArray();

            InitializeComponent();
            ForeColorBrush = new SolidBrush(ForeColor);
            oldHeight = Height;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.Selectable, true);

            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;

            AutoScaleMode = AutoScaleMode.None;

            twinFamilyButtonBounds = new Rectangle(1, 1, twinsFamily.Length * 30, 30);

            if (!Node.Id.IsEmpty)
            {
                ValueChanged += (s, ev) => eventManager.GetEvent(Node.Id)?.Invoke(Value);
                eventManager.GetEvent(Node.Id)?.Invoke(Value);

            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(CultureInfo.InvariantCulture, $"{Node.Name}");
            if (!Node.Id.IsEmpty)
                sb.AppendLine(CultureInfo.InvariantCulture, $"Id: {Node.Id}");
            if (Node.Description is not null)
                sb.AppendLine(Node.Description);
            toolTipText = sb.ToString();


            if (Node.Condition is not null)
            {
                Dictionary<string, ChangedEvent> vars = new Dictionary<string, ChangedEvent>();


                ConditionInterpreter? interpreter = new ConditionInterpreter(x =>
                {
                    string? id = x.ToLowerInvariant();
                    if (vars.TryGetValue(id, out ChangedEvent? ce))
                        return ce.Value ?? JValue.CreateNull();
                    ChangedEvent? e = eventManager.GetEvent(id);
                    if (e is null)
                        return JValue.CreateNull();
                    vars.Add(id, e);
                    return e?.Value ?? JValue.CreateNull();
                }, Node.Condition);



                SetDisplayState(interpreter.ResolveCondition());
                foreach (KeyValuePair<string, ChangedEvent> ce in vars)
                {
                    ce.Value.Event += (sender, e) =>
                    {
                        if (IsDisposed)
                            return;
                        SetDisplayState(interpreter.ResolveCondition());

                    };
                }
            }
            else
            {
                SetDisplayState(true);
            }
        }

        protected JToken CreateValue() => Value = Node.CreateDefaultValue();
        protected void OnValueChanged(JtfEditorAction action) => ValueChanged?.Invoke(this, new ValueChangedEventArgs(action));
        protected void OnValueChanged(ValueChangedEventArgs eventArgs) => ValueChanged?.Invoke(this, eventArgs);
        protected virtual void OnExpandChanged()
        {
            Focus();
            Invalidate();
        }
        private void SetDisplayState(bool state)
        {
            if (state)
            {
                Expanded = false;
                Height = 32;
                TabStop = true;
            }
            else
            {
                Expanded = false;
                Height = 0;
                TabStop = false;
                Value = Node.CreateDefaultValue();
            }
        }
        protected void CreateDynamicNameTextBox()
        {

            if (txtDynamicName is not null)
                return;

            if (SuspendFocus)
                return;

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
                ReadOnly = RootEditor.ReadOnly
            };

            txtDynamicName.Location = new Point(dynamicNameTextboxBounds.X + 10, 16 - txtDynamicName.Height / 2);
            txtDynamicName.Width = dynamicNameTextboxBounds.Width - 20;
            txtDynamicName.TextChanged += (sender, eventArgs) =>
            {
                DynamicNamePreviewChange?.Invoke(this, new DynamicNamePreviewChangeEventArgs(txtDynamicName.Name, DynamicName));
            };
            txtDynamicName.LostFocus += (sender, eventArgs) =>
            {
                if (txtDynamicName is null)
                    return;
                if (DynamicName != txtDynamicName.Text && !RootEditor.ReadOnly)
                {
                    string? oldDynamicName = DynamicName;
                    DynamicName = txtDynamicName.Text;
                    OnValueChanged(new JtfEditorAction(JtfEditorAction.JtEditorActionType.DynamicNameChanged, oldDynamicName, DynamicName, this));
                }
                else
                    Invalidate();
                Controls.Remove(txtDynamicName);
                txtDynamicName = null;
            };
            txtDynamicName.GotFocus += (sender, e) => Invalidate();
          
            Controls.Add(txtDynamicName);
            txtDynamicName?.Focus();
            txtDynamicName?.SelectAll();
        }


        private void InitDraw(Graphics g)
        {
            Color borderColor = BorderColor;

            if (IsFocused)
            {
                ControlPaint.DrawBorder(g, new Rectangle(0, 0, Width, Height), borderColor, 2, ButtonBorderStyle.Solid, borderColor, 2, ButtonBorderStyle.Solid, borderColor, 2, ButtonBorderStyle.Solid, borderColor, 2, ButtonBorderStyle.Solid);
                xOffset = 2;
                xRightOffset = 2;
                yOffset = 2;
                if (Expanded && Node is JtContainerNode)
                    innerHeight = 29;
                else
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
        }
        private void DrawInvalidValueMessage(Graphics g)
        {
            if (!IsInvalidValueType)
                return;


            string message = string.Format(CultureInfo.CurrentCulture, Properties.Resources.InvalidValueType, Value.Type, Node.JsonType);

            SizeF sf = g.MeasureString(message, Font);
            g.DrawString(message, Font, RootEditor.ColorTable.InvalidValueBrush, new PointF(xOffset + 10, 16 - sf.Height / 2));

            xOffset += (int)sf.Width + 20;

            string discardMessage = Properties.Resources.DiscardInvalidType;

            SizeF dsf = g.MeasureString(discardMessage, Font);

            discardInvalidTypeButtonBounds = new Rectangle(xOffset, yOffset, (int)dsf.Width + 10, innerHeight);
            g.FillRectangle(RootEditor.ColorTable.DiscardInvalidValueButtonBackBrush, discardInvalidTypeButtonBounds);
            g.DrawString(discardMessage, Font, RootEditor.ColorTable.DiscardInvalidValueButtonForeBrush, xOffset + 5, 16 - dsf.Height / 2);

            xOffset += (int)sf.Width + 20;
        }
        private void DrawDynamicName(Graphics g)
        {
            if (!Node.IsDynamicName)
                return;


            if (Node is JtContainerNode)
            {
                dynamicNameTextboxBounds = new Rectangle(xOffset, yOffset, Width - xOffset - xRightOffset, innerHeight);
                g.FillRectangle(RootEditor.ColorTable.TextBoxBackBrush, dynamicNameTextboxBounds);

                if (txtDynamicName is not null)
                    return;


                SizeF sf = g.MeasureString(DynamicName, Font);

                g.DrawString(DynamicName, Font, RootEditor.ColorTable.TextBoxForeBrush, new PointF(xOffset + 10, 16 - sf.Height / 2));
            }
            else
            {
                SizeF s = g.MeasureString(":", Font);
                int size = (Width - xOffset - (int)s.Width - 10 - xRightOffset) / 2;

                dynamicNameTextboxBounds = new Rectangle(xOffset, yOffset, size, innerHeight);
                g.FillRectangle(RootEditor.ColorTable.TextBoxBackBrush, dynamicNameTextboxBounds);
                if (txtDynamicName is null)
                {

                    SizeF sf = g.MeasureString(DynamicName, Font);

                    g.DrawString(DynamicName, Font, RootEditor.ColorTable.TextBoxForeBrush, new PointF(xOffset + 10, 16 - sf.Height / 2));
                }
                xOffset += size;


                g.DrawString(":", Font, ForeColorBrush, new PointF(xOffset + 5, 16 - s.Height / 2));

                xOffset += (int)s.Width + 10;

            }
        }
        private void DrawRemoveButton(Graphics g)
        {
            if (!Node.IsArrayPrefab || RootEditor.ReadOnly)
                return;

            int width = IsFocused ? 29 : 30;
            removeButtonBounds = new Rectangle(Width - xRightOffset - width, yOffset, width, innerHeight);
            if (Expanded && !Node.IsDynamicName && Node is not JtArrayNode)
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
                g.FillRectangle(RootEditor.ColorTable.RemoveItemButtonBackBrush, removeButtonBounds);

            g.DrawLine(RootEditor.ColorTable.RemoveItemButtonForePen, Width - 20, 12, Width - 12, 20);
            g.DrawLine(RootEditor.ColorTable.RemoveItemButtonForePen, Width - 12, 12, Width - 20, 20);

            xRightOffset += width;
        }
        private void DrawName(Graphics g)
        {
            if (!string.IsNullOrEmpty(Node.DisplayName))
            {
                int x = xOffset;
                xOffset += ArrayIndex != -1 ? 10 : 20;

                string dn;
                if(ArrayIndex != -1)
                    dn = ConvertToFriendlyName($"{ArrayIndex} ({Node.DisplayName})");
                else
                    dn = ConvertToFriendlyName(Node.DisplayName);
                SizeF nameSize = g.MeasureString(dn, Font);

                g.DrawString(dn, Font, IsSavable ? ForeColorBrush : RootEditor.ColorTable.DefaultElementForeBrush, new PointF(xOffset, 16 - nameSize.Height / 2));
                xOffset += (int)nameSize.Width;



                if (Node.Required)
                {
                    g.DrawString("*", Font, RootEditor.ColorTable.RequiredStarBrush, new PointF(xOffset, 16 - nameSize.Height / 2));
                }





                xOffset += ArrayIndex != -1 ? 10 : 20;
                nameLabelBounds = new Rectangle(x, 1, xOffset - x, 30);
            }
            else if (ArrayIndex != -1)
            {
                int x = xOffset;
                xOffset += 10;

                string index = ArrayIndex.ToString(CultureInfo.CurrentCulture);

                SizeF nameSize = g.MeasureString(index, Font);

                g.DrawString(index, Font, ForeColorBrush, new PointF(xOffset, 32 / 2 - nameSize.Height / 2));

                xOffset += (int)nameSize.Width;
                xOffset += 10;

                nameLabelBounds = new Rectangle(x, 1, xOffset - x, 30);
            }
        }

        private bool CanDrawExpandButton => Node is JtContainerNode && !IsInvalidValueType && CanCollapse && !(RootEditor.ReadOnly && !IsSavable) && !Node.IsRoot;

        public string Path => Parent is IJsonItem ji ? ji.Path + GetCurrentPathName() : GetCurrentPathName();
        private string GetCurrentPathName()
        {
            if (Node.IsDynamicName)
            {
                return $"[{DynamicName}]";
            }
            else if (Node.IsArrayPrefab)
            {
                return $"[{ArrayIndex}]";
            }
            else
            {
                return $"\\{Node.Name!}";
            }
        }

        private void DrawExpandButton(Graphics g)
        {
            if (!CanDrawExpandButton)
                return;

            expandButtonBounds = new Rectangle(xOffset, yOffset, 30, innerHeight);

            if (Expanded && !Node.IsDynamicName)
            {
                RectangleF bounds = new RectangleF(expandButtonBounds.Location, expandButtonBounds.Size);
                g.SmoothingMode = SmoothingMode.HighQuality;
                using GraphicsPath rectPath = new GraphicsPath();

                bounds.Offset(-0.5f, -0.5f);
                float w = bounds.X + bounds.Width;
                float h = bounds.Y + bounds.Height;
                rectPath.AddLine(bounds.X, bounds.Y, w, bounds.Y);
                rectPath.AddArc(w - 10, h - 10, 10, 10, 0, 90);
                if (twinsFamily[^1] == Node)
                    rectPath.AddLine(bounds.X, h, bounds.X, bounds.Y);
                else
                    rectPath.AddArc(bounds.X, h - 10, 10, 10, 90, 90);

                g.FillPath(RootEditor.ColorTable.ExpandButtonBackBrush, rectPath);

                g.SmoothingMode = SmoothingMode.Default;

            }
            else
                g.FillRectangle(RootEditor.ColorTable.ExpandButtonBackBrush, expandButtonBounds);


            g.SmoothingMode = SmoothingMode.HighQuality;
            RectangleF innerRectBounds = new RectangleF(xOffset + 7, 8, 16, 16);
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


            if (Expanded)
            {
                g.DrawLine(RootEditor.ColorTable.ExpandButtonForePen, xOffset + 12, 16, xOffset + 18, 16);
            }
            else
            {
                g.DrawLine(RootEditor.ColorTable.ExpandButtonForePen, xOffset + 12, 16, xOffset + 18, 16);
                g.DrawLine(RootEditor.ColorTable.ExpandButtonForePen, xOffset + 15, 12, xOffset + 15, 20);
            }
            xOffset += 30;
        }
        private void DrawTypeIcons(Graphics g)
        {
            bool rounded = Node is JtContainerNode && Expanded && (!CanCollapse || twinsFamily[^1] != Node || twinsFamily[0] != Node);


            Span<JtNode> twinFamilySpan = twinsFamily;
            bool isLastNode = twinFamilySpan[^1] == Node;
            bool isFirstNode = twinFamilySpan[0] == Node;
            for (int i = 0; i < twinFamilySpan.Length; i++)
            {
                JtNode item = twinFamilySpan[i];
                if (Node.IsArrayPrefab && Node != item)
                {
                    continue;
                }

                if (Properties.Resources.ResourceManager.GetObject(item.Type.Name, CultureInfo.InvariantCulture) is not Bitmap bmp)
                    continue;

                int width = i == 0 && IsFocused ? 29 : 30;
                if (Node == item)
                {
                    if (rounded)
                    {
                        RectangleF bounds = new RectangleF(xOffset, yOffset, width, innerHeight);
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        using GraphicsPath rectPath = new GraphicsPath();

                        bounds.Offset(-0.5f, -0.5f);
                        float w = bounds.X + bounds.Width;
                        float h = bounds.Y + bounds.Height;
                        rectPath.AddLine(bounds.X, bounds.Y, w, bounds.Y);

                        
                        if (!isLastNode || !CanDrawExpandButton)
                            rectPath.AddArc(w - 10, h - 10, 10, 10, 0, 90);
                        else
                            rectPath.AddLine(w, bounds.Y, w, h);
                        if (isFirstNode)
                            rectPath.AddLine(bounds.X, h, bounds.X, bounds.Y);
                        else
                            rectPath.AddArc(bounds.X, h - 10, 10, 10, 90, 90);
                        g.FillPath(RootEditor.ColorTable.SelectedNodeTypeBackBrush, rectPath);

                        g.SmoothingMode = SmoothingMode.Default;
                    }
                    else
                        g.FillRectangle(RootEditor.ColorTable.SelectedNodeTypeBackBrush, xOffset, yOffset, width, innerHeight);

                }

                g.DrawImage(bmp, xOffset + (i == 0 && IsFocused ? 7 : 8), 8, 16, 16);

                xOffset += width;
            }
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            if (Node.IsDynamicName)
            {
                if (txtDynamicName is null)
                    CreateDynamicNameTextBox();
                else
                    txtDynamicName.Focus();
            }
            Invalidate();
        }
        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }
        protected override void OnForeColorChanged(EventArgs e)
        {
            if (ForeColorBrush.Color != ForeColor)
                ForeColorBrush = new SolidBrush(ForeColor);
            base.OnForeColorChanged(e);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            if (Node.IsRoot)
            {
                Expanded = true;
                if (Node is JtContainerNode { ContainerDisplayType: JtContainerType.Block } && !IsInvalidValueType && Node.Template.Roots.Count == 1)
                {
                    xOffset = 0;
                    xRightOffset = 0;
                    yOffset = 0;
                    innerHeight = 32;
                    return;
                }
            }
            if (Node is JtContainerNode c && c.DisableCollapse)
                Expanded = true;


            InitDraw(g);
            if (ArrayIndex != -1)
            {
                TabIndex = ArrayIndex;
            }


            DrawTypeIcons(g);
            DrawExpandButton(g);
            DrawName(g);
            DrawInvalidValueMessage(g);
            DrawRemoveButton(g);
            DrawDynamicName(g);
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (e.Button != MouseButtons.Left)
            {
                Focus();
                return;
            }
            if (expandButtonBounds.Contains(e.Location))
            {
                Expanded = !Expanded;
                if (Control.ModifierKeys is Keys.Shift && Expanded)
                {
                    DeepExpand();
                }
                return;
            }
            if (removeButtonBounds.Contains(e.Location))
            {
                if (txtDynamicName is not null)
                {
                    if (DynamicName != txtDynamicName.Text && !RootEditor.ReadOnly)
                    {
                        string? oldDynamicName = DynamicName;
                        DynamicName = txtDynamicName.Text;
                        OnValueChanged(new JtfEditorAction(JtfEditorAction.JtEditorActionType.DynamicNameChanged, oldDynamicName, DynamicName, this));
                    }
                    else
                        Invalidate();
                    Controls.Remove(txtDynamicName);
                    txtDynamicName = null;
                }

                if (Parent is not ArrayEditorItem parent)
                    throw new Exception();

                parent.RemoveChild(this);


                return;
            }
            if (discardInvalidTypeButtonBounds.Contains(e.Location))
            {
                CreateValue();
                if (Node.IsRoot)
                    Expanded = true;
                if (!CanCollapse || Node.IsRoot)
                    OnExpandChanged();
                else
                    Invalidate();

                return;
            }
            if (dynamicNameTextboxBounds.Contains(e.Location))
            {
                CreateDynamicNameTextBox();
                return;
            }



            if (new Rectangle(0, 1, twinsFamily.Length * 30, 30).Contains(e.Location))
            {
                JtNode newType = twinsFamily[(e.Location.X - 1) / 30];

                if (newType == Node)
                    return;
                TwinTypeChanged?.Invoke(this, new TwinChangedEventArgs(newType));

                return;

            }


        }

        private void DeepExpand()
        {
            if (Node is not JtContainerNode)
                return;
            SuspendFocus = true;
            foreach (object? item in Controls)
            {
                if (item is BlockEditorItem bei)
                {
                    if (bei.ValidValue?.Count > 0)
                    {
                        bei.Expanded = true;
                        bei.DeepExpand();
                    }
                }
                if (item is ArrayEditorItem aei)
                {
                    if (aei.ValidValue?.Count > 0)
                    {
                        aei.Expanded = true;
                        aei.DeepExpand();
                    }
                }
            }
            SuspendFocus = false;
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (oldHeight == Height)
                return;
            oldHeight = Height;
            HeightChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);



            if (nameLabelBounds.Contains(e.Location))
            {
                Cursor = Cursors.Help;
                if (RootEditor.ToolTip.Active)
                    return;
                RootEditor.ToolTip.Active = true;
                RootEditor.ToolTip.Show(toolTipText, this);
                return;
            }
            else
            {
                if (RootEditor.ToolTip.Active)
                {

                    RootEditor.ToolTip.Active = false;
                    RootEditor.ToolTip.Hide(this);
                }
            }

            if (expandButtonBounds.Contains(e.Location))
                Cursor = Cursors.Hand;
            else if (!RootEditor.ReadOnly && (removeButtonBounds.Contains(e.Location) || discardInvalidTypeButtonBounds.Contains(e.Location) || twinFamilyButtonBounds.Contains(e.Location)))
                Cursor = Cursors.Hand;
            else if (dynamicNameTextboxBounds.Contains(e.Location) && !RootEditor.ReadOnly)
                Cursor = Cursors.IBeam;

            else
                Cursor = Cursors.Default;

        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (IsInvalidValueType)
                return;

            if (Node is JtContainerNode && e.KeyCode == Keys.Space)
            {
                Expanded = !Expanded;
            }

        }
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);

            if (Parent is null)
            {
                Dispose();
            }
        }

        protected static string ConvertToFriendlyName(string name)
        {
            return string.Create(name.Length, name, new System.Buffers.SpanAction<char, string>((span, n) =>
            {
                span[0] = char.ToUpper(n[0], CultureInfo.CurrentCulture);
                for (int i = 1; i < name.Length; i++)
                {
                    if (name[i] is '_')
                    {
                        span[i] = ' ';
                        if (name.Length <= i + 1)
                        {
                            continue;
                        }
                        i++;
                        span[i] = char.ToUpper(name[i], CultureInfo.CurrentCulture);
                        continue;
                    }
                    span[i] = name[i];
                }

            }));
        }
        public static EditorItem Create(JtNode node, JToken? token, JsonJtfEditor rootEditor, EventManager eventManager)
        {
            if (node is JtBoolNode boolNode)
                return new BoolEditorItem(boolNode, token, rootEditor, eventManager);
            if (node is JtValueNode valueNode)
                return new ValueEditorItem(valueNode, token, rootEditor, eventManager);
            if (node is JtBlockNode blockNode)
                return new BlockEditorItem(blockNode, token, rootEditor, eventManager);
            if (node is JtArrayNode arrayNode)
                return new ArrayEditorItem(arrayNode, token, rootEditor, eventManager);
            return new UnknownEditorItem(node, token, rootEditor, eventManager);
        }



        protected class FocusableControl : UserControl
        {
            public FocusableControl()
            {
                SetStyle(ControlStyles.Selectable, true);
            }
        }
    }
    internal class DynamicNamePreviewChangeEventArgs : EventArgs
    {
        public DynamicNamePreviewChangeEventArgs(string? newDynamicName, string? oldDynamicName)
        {
            NewDynamicName = newDynamicName;
            OldDynamicName = oldDynamicName;
        }

        public string? NewDynamicName { get; }
        public string? OldDynamicName { get; }
    }
}