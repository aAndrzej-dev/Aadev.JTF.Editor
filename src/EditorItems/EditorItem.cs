using Aadev.ConditionsInterpreter;
using Aadev.JTF.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Aadev.JTF.Editor.EditorItems
{
    internal abstract partial class EditorItem : UserControl, IJsonItem
    {
        internal static readonly Pen whitePen = new Pen(Color.White);
        internal static readonly SolidBrush yellowBrush = new SolidBrush(Color.Yellow);
        internal static readonly SolidBrush redBrush = new SolidBrush(Color.Red);
        internal static readonly SolidBrush whiteBrush = new SolidBrush(Color.White);
        internal static readonly SolidBrush lightGrayBrush = new SolidBrush(Color.LightGray);
        internal static readonly SolidBrush goldBrush = new SolidBrush(Color.Gold);
        internal static readonly SolidBrush greenBrush = new SolidBrush(Color.Green);
        internal static readonly SolidBrush royalBlueBrush = new SolidBrush(Color.RoyalBlue);
        internal static readonly SolidBrush grayBrush = new SolidBrush(Color.FromArgb(80, 80, 80));

        private readonly JtNode[] twinsFamily;
        private readonly string toolTipText;
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
        protected int xOffset = 0;
        protected int xRightOffset = 0;
        protected int yOffset = 0;
        protected int innerHeight = 0;


        internal bool IsInvalidValueType => Value.Type is not JTokenType.Null && (Value.Type != Node.JsonType);
        protected virtual bool IsFocused => Focused || txtDynamicName?.Focused is true;
        protected JsonJtfEditor RootEditor { get; }
        protected SolidBrush ForeColorBrush { get; private set; }
        protected bool CanCollapse => Node is JtContainer c && !c.DisableCollapse;
        protected bool Expanded { get => expanded; set { if (expanded == value) return; expanded = !CanCollapse || value; SuspendFocus = true; OnExpandChanged(); SuspendFocus = false; } }
        protected virtual Color BorderColor
        {
            get
            {
                if (IsInvalidValueType)
                    return Color.Red;
                else if (IsFocused)
                    return Color.Aqua;
                else
                    return Color.FromArgb(200, 200, 200);
            }
        }

        internal int ArrayIndex { get; set; } = -1;
        internal virtual bool IsSaveable => Node.Required || Node.Parent is { ContainerDisplayType: JtContainerType.Block, ContainerJsonType: JtContainerType.Array } || Node.IsRoot;
        internal bool SuspendFocus { get; private set; }

        public JtNode Node { get; }
        public abstract JToken Value { get; set; }
        public string? DynamicName { get => dynamicName; set { dynamicName = value; Invalidate(); } }

        public event EventHandler? ValueChanged;
        public event EventHandler? DynamicNameChanged;
        internal event EventHandler<TwinChangedEventArgs>? TwinTypeChanged;
        internal event EventHandler? HeightChanged;

        protected internal EditorItem(JtNode type, JToken? token, JsonJtfEditor rootEditor, IEventManagerProvider eventManagerProvider)
        {
            Node = type;
            RootEditor = rootEditor;
            if (token is null || token.Type is JTokenType.Null)
                Value = IsSaveable ? CreateValue() : JValue.CreateNull();
            else
                Value = token;
            eventManager = eventManagerProvider.GetEventManager(Node.IdentifiersManager);
            twinsFamily = RootEditor.NormalizeTwinNodeOrder ? Node.GetTwinFamily().OrderBy(x => x.Type.Id).ToArray() : Node.GetTwinFamily();

            InitializeComponent();
            ForeColorBrush = new SolidBrush(ForeColor);
            oldHeight = Height;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.Selectable, true);

            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;

            AutoScaleMode = AutoScaleMode.None;

            twinFamilyButtonBounds = new Rectangle(1, 1, twinsFamily.Length * 30, 30);

            if (Node.Id is not null)
            {
                ValueChanged += (s, ev) => eventManager.GetEvent(Node.Id)?.Invoke(Value);
                eventManager.GetEvent(Node.Id)?.Invoke(Value);
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{Node.Name}");
            if (Node.Id is not null)
                sb.AppendLine($"Id: {Node.Id}");
            if (Node.Description is not null)
                sb.AppendLine(Node.Description);
            toolTipText = sb.ToString();




            if (Node.Condition is not null)
            {
                Dictionary<string, ChangedEvent> vars = new Dictionary<string, ChangedEvent>();


                ConditionInterpreter? interpreter = new ConditionInterpreter(x =>
                {
                    string? id = x.ToLower();
                    if (vars.ContainsKey(id))
                        return vars[id]!.Value ?? JValue.CreateNull();
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
        protected void OnValueChanged() => ValueChanged?.Invoke(this, EventArgs.Empty);
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
                Value = JValue.CreateNull();
                OnValueChanged();
            }
        }
        protected void CreateDynamicNameTextBox()
        {
            if (txtDynamicName is not null)
            {
                return;
            }
            if (SuspendFocus)
                return;

            txtDynamicName = new TextBox
            {
                Font = Font,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = ForeColor,
                AutoSize = false,
                TabIndex = 0,

                Text = DynamicName,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };

            txtDynamicName.Location = new Point(dynamicNameTextboxBounds.X + 10, 16 - txtDynamicName.Height / 2);
            txtDynamicName.Width = dynamicNameTextboxBounds.Width - 20;
            txtDynamicName.TextChanged += (sender, eventArgs) =>
            {
                DynamicNameChanged?.Invoke(this, EventArgs.Empty);
                DynamicName = txtDynamicName.Text ?? DynamicName;

            };
            txtDynamicName.LostFocus += (sender, eventArgs) =>
            {
                if (txtDynamicName is null)
                    return;

                DynamicName = txtDynamicName.Text;
                OnValueChanged();
                Controls.Remove(txtDynamicName);
                txtDynamicName = null;
                Invalidate();
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
                if (Expanded)
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


            string message = string.Format(Properties.Resources.InvalidValueType, Value.Type, Node.JsonType);

            SizeF sf = g.MeasureString(message, Font);
            g.DrawString(message, Font, redBrush, new PointF(xOffset + 10, 16 - sf.Height / 2));

            xOffset += (int)sf.Width + 20;

            string discardMessage = Properties.Resources.DiscardInvalidType;

            SizeF dsf = g.MeasureString(discardMessage, Font);

            discardInvalidTypeButtonBounds = new Rectangle(xOffset, yOffset, (int)dsf.Width + 10, innerHeight);
            g.FillRectangle(redBrush, discardInvalidTypeButtonBounds);
            g.DrawString(discardMessage, Font, whiteBrush, xOffset + 5, 16 - dsf.Height / 2);

            xOffset += (int)sf.Width + 20;
        }
        private void DrawDynamicName(Graphics g)
        {
            if (!Node.IsDynamicName)
                return;


            if (Node is JtContainer)
            {
                dynamicNameTextboxBounds = new Rectangle(xOffset, yOffset, Width - xOffset - xRightOffset, innerHeight);
                g.FillRectangle(grayBrush, dynamicNameTextboxBounds);

                if (txtDynamicName is not null)
                    return;


                SizeF sf = g.MeasureString(DynamicName, Font);

                g.DrawString(DynamicName, Font, ForeColorBrush, new PointF(xOffset + 10, 16 - sf.Height / 2));
            }
            else
            {
                SizeF s = g.MeasureString(":", Font);
                int size = (Width - xOffset - (int)s.Width - 10 - xRightOffset) / 2;

                dynamicNameTextboxBounds = new Rectangle(xOffset, yOffset, size, innerHeight);
                g.FillRectangle(grayBrush, dynamicNameTextboxBounds);
                if (txtDynamicName is null)
                {

                    SizeF sf = g.MeasureString(DynamicName, Font);

                    g.DrawString(DynamicName, Font, ForeColorBrush, new PointF(xOffset + 10, 16 - sf.Height / 2));
                }
                xOffset += size;


                g.DrawString(":", Font, ForeColorBrush, new PointF(xOffset + 5, 16 - s.Height / 2));

                xOffset += (int)s.Width + 10;

            }
        }
        private void DrawRemoveButton(Graphics g)
        {
            if (!Node.IsArrayPrefab)
                return;

            removeButtonBounds = new Rectangle(Width - xRightOffset - 30, yOffset, 30, innerHeight);

            if (Expanded && !Node.IsDynamicName)
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
                g.FillPath(redBrush, rectPath);

                g.SmoothingMode = SmoothingMode.Default;
            }
            else
                g.FillRectangle(redBrush, removeButtonBounds);

            g.DrawLine(whitePen, Width - 20, 12, Width - 12, 20);
            g.DrawLine(whitePen, Width - 12, 12, Width - 20, 20);

            xRightOffset += 30;
        }
        private void DrawName(Graphics g)
        {
            if (!string.IsNullOrEmpty(Node.DisplayName))
            {
                int x = xOffset;
                xOffset += 20;

                string dn = ConvertToFriendlyName(Node.DisplayName);

                SizeF nameSize = g.MeasureString(dn, Font);

                g.DrawString(dn, Font, IsSaveable ? ForeColorBrush : lightGrayBrush, new PointF(xOffset, 16 - nameSize.Height / 2));
                xOffset += (int)nameSize.Width;



                if (Node.Required)
                {
                    g.DrawString("*", Font, goldBrush, new PointF(xOffset, 16 - nameSize.Height / 2));
                }





                xOffset += 20;
                nameLabelBounds = new Rectangle(x, 1, xOffset - x, 30);
            }
            if (ArrayIndex != -1)
            {
                int x = xOffset;
                xOffset += 10;

                string arrind = ArrayIndex.ToString();

                SizeF nameSize = g.MeasureString(arrind, Font);

                g.DrawString(arrind, Font, ForeColorBrush, new PointF(xOffset, 32 / 2 - nameSize.Height / 2));

                xOffset += (int)nameSize.Width;
                xOffset += 10;

                nameLabelBounds = new Rectangle(x, 1, xOffset - x, 30);
            }
        }
        private void DrawExpandButton(Graphics g)
        {
            if (Node is not JtContainer || IsInvalidValueType || !CanCollapse)
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

                g.FillPath(greenBrush, rectPath);

                g.SmoothingMode = SmoothingMode.Default;

            }
            else
                g.FillRectangle(greenBrush, expandButtonBounds);


            g.SmoothingMode = SmoothingMode.HighQuality;
            RectangleF innerRectBounds = new RectangleF(xOffset + 7, 8, 16, 16);
            GraphicsPath innerRectPath = new GraphicsPath();

            float iw = innerRectBounds.X + innerRectBounds.Width;
            float ih = innerRectBounds.Y + innerRectBounds.Height;

            innerRectPath.AddArc(innerRectBounds.X, innerRectBounds.Y, 4, 4, 180, 90);
            innerRectPath.AddArc(iw - 4, innerRectBounds.Y, 4, 4, 270, 90);
            innerRectPath.AddArc(iw - 4, ih - 4, 4, 4, 0, 90);

            innerRectPath.AddArc(innerRectBounds.X, ih - 4, 4, 4, 90, 90);
            innerRectPath.CloseFigure();
            g.DrawPath(whitePen, innerRectPath);
            g.SmoothingMode = SmoothingMode.Default;


            if (Expanded)
            {
                g.DrawLine(whitePen, xOffset + 12, 16, xOffset + 18, 16);
            }
            else
            {
                g.DrawLine(whitePen, xOffset + 12, 16, xOffset + 18, 16);
                g.DrawLine(whitePen, xOffset + 15, 12, xOffset + 15, 20);
            }
            xOffset += 30;
        }
        private void DrawTypeIcons(Graphics g)
        {
            bool rounded = Node is JtContainer && Expanded && (!CanCollapse || twinsFamily[^1] != Node || twinsFamily[0] != Node);



            for (int i = 0; i < twinsFamily.Length; i++)
            {
                JtNode item = twinsFamily[i];
                if (Node.IsArrayPrefab && Node != item)
                {
                    continue;
                }

                if (Properties.Resources.ResourceManager.GetObject(item.Type.Name) is not Bitmap bmp)
                    continue;

                if (Node == item)
                {
                    if (rounded)
                    {
                        RectangleF bounds = new RectangleF(xOffset, yOffset, 30, innerHeight);
                        g.SmoothingMode = SmoothingMode.HighQuality;
                        using GraphicsPath rectPath = new GraphicsPath();

                        bounds.Offset(-0.5f, -0.5f);
                        float w = bounds.X + bounds.Width;
                        float h = bounds.Y + bounds.Height;
                        rectPath.AddLine(bounds.X, bounds.Y, w, bounds.Y);

                        if (twinsFamily[^1] != Node || !CanCollapse)
                            rectPath.AddArc(w - 10, h - 10, 10, 10, 0, 90);
                        else
                            rectPath.AddLine(w, bounds.Y, w, h);
                        if (twinsFamily[0] == Node)
                            rectPath.AddLine(bounds.X, h, bounds.X, bounds.Y);
                        else
                            rectPath.AddArc(bounds.X, h - 10, 10, 10, 90, 90);
                        g.FillPath(royalBlueBrush, rectPath);

                        g.SmoothingMode = SmoothingMode.Default;
                    }
                    else
                        g.FillRectangle(royalBlueBrush, xOffset, yOffset, 30, innerHeight);

                }

                g.DrawImage(bmp, xOffset + 8, 8, 16, 16);

                xOffset += 30;
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
                if (Node is JtContainer { ContainerDisplayType: JtContainerType.Block } && !IsInvalidValueType)
                {
                    xOffset = 0;
                    xRightOffset = 0;
                    yOffset = 0;
                    innerHeight = 32;
                    return;
                }
            }
            if (Node is JtContainer c && c.DisableCollapse)
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


            if (expandButtonBounds.Contains(e.Location))
            {
                Expanded = !Expanded;
                return;
            }
            if (e.Button != MouseButtons.Left)
            {
                Focus();
                return;
            }
            if (removeButtonBounds.Contains(e.Location))
            {
                if (txtDynamicName is not null)
                {
                    DynamicName = txtDynamicName.Text;
                    OnValueChanged();
                    Controls.Remove(txtDynamicName);
                    txtDynamicName = null;
                    Invalidate();
                }

                if (Parent is not ArrayEditorItem parent)
                    throw new Exception();

                parent.RemoverChild(this);


                return;
            }
            if (discardInvalidTypeButtonBounds.Contains(e.Location))
            {
                CreateValue();

                if (!CanCollapse)
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
        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (Node is JtContainer && Expanded)
            {
                Expanded = false;
                Expanded = true;
            }
            else
                Refresh();
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (oldHeight == Height)
                return;

            HeightChanged?.Invoke(this, EventArgs.Empty);
            oldHeight = Height;

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


            Cursor = expandButtonBounds.Contains(e.Location) || removeButtonBounds.Contains(e.Location) || discardInvalidTypeButtonBounds.Contains(e.Location) || twinFamilyButtonBounds.Contains(e.Location)
                ? Cursors.Hand
                : Cursors.Default;
            if (dynamicNameTextboxBounds.Contains(e.Location))
                Cursor = Cursors.IBeam;

        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (IsInvalidValueType)
                return;

            if (Node is JtContainer && e.KeyCode == Keys.Space)
            {
                Expanded = !Expanded;
            }

        }


        protected static string ConvertToFriendlyName(string name)
        {
            return string.Create(name.Length, name, new System.Buffers.SpanAction<char, string>((span, n) =>
            {
                span[0] = char.ToUpper(n[0]);
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
                        span[i] = char.ToUpper(name[i]);
                        continue;
                    }
                    span[i] = name[i];
                }

            }));
        }
        public static EditorItem Create(JtNode type, JToken? token, JsonJtfEditor jsonJtfEditor, IEventManagerProvider eventManagerProvider)
        {
            if (type.Type == JtNodeType.Bool)
                return new BoolEditorItem(type, token, jsonJtfEditor, eventManagerProvider);
            if (type.Type == JtNodeType.String || type.Type.IsNumericType)
                return new ValueEditorItem(type, token, jsonJtfEditor, eventManagerProvider);
            if (type.Type == JtNodeType.Block)
                return new BlockEditorItem(type, token, jsonJtfEditor, eventManagerProvider);
            if (type.Type == JtNodeType.Array)
                return new ArrayEditorItem(type, token, jsonJtfEditor, eventManagerProvider);

            throw new ArgumentOutOfRangeException(nameof(type));
        }

        protected class FocusableControl : UserControl
        {
            public FocusableControl()
            {
                SetStyle(ControlStyles.Selectable, true);
            }
        }
    }
}