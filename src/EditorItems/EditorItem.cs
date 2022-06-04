using Aadev.JTF.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Aadev.JTF.Editor.EditorItems
{
    internal abstract partial class EditorItem : UserControl
    {
        protected static Pen WhitePen = new Pen(Color.White);
        protected virtual bool IsFocused => Focused || txtDynamicName?.Focused is true;
        protected bool IsInvalidValueType => Value.Type is not JTokenType.Null && (Value.Type != Node.JsonType);


        private string? dynamicName;
        private int oldHeight;
        private bool expanded;
        private readonly JtNode[] twinsFamily;
        protected TextBox? txtDynamicName;
        private Rectangle expandButtonBounds = Rectangle.Empty;
        private Rectangle removeButtonBounds = Rectangle.Empty;
        private Rectangle discardInvalidTypeButtonBounds = Rectangle.Empty;
        private Rectangle dynamicNameTextboxBounds = Rectangle.Empty;
        private Rectangle nameLabelBounds = Rectangle.Empty;
        private Rectangle conditionsLabelBounds = Rectangle.Empty;
        private Rectangle twinFamilyButtonBounds;
        private bool hasInvalidConditions;


        protected bool HaveEventHandlersBeenCreated { get; private set; }

        protected int xOffset = 0;
        protected int xRightOffset = 0;
        protected int yOffset = 0;
        protected int innerHeight = 0;
        protected JsonJtfEditor RootEditor { get; }



        public event EventHandler? ValueChanged;
        public event EventHandler? DynamicNameChanged;
        internal event EventHandler<TwinChangedEventArgs>? TwinTypeChanged;
        internal event EventHandler? HeightChanged;


        internal int ArrayIndex { get; set; } = -1;
        public abstract JToken Value { get; set; }
        public JtNode Node { get; }
        public EventManager EventManager { get; }
        protected bool CanCollapse => Node is JtArray or JtBlock && Parent is not JsonJtfEditor;
        internal abstract bool IsSaveable { get; }


        public string? DynamicName { get => dynamicName; set { dynamicName = value; Invalidate(); } }
        protected bool Expanded { get => expanded; set { if (expanded == value) return; expanded = !CanCollapse || value; OnExpandChanged(); } }

        protected abstract JToken CreateValue();
        protected void OnValueChanged() => ValueChanged?.Invoke(this, EventArgs.Empty);
        protected virtual void OnExpandChanged()
        {
            Focus();
            Invalidate();

        }

        protected internal EditorItem(JtNode type, JToken? token, EventManager eventManager, JsonJtfEditor rootEditor)
        {
            Node = type;
            Value = token ?? ((Node.Required || Node.IsArrayPrefab) ? CreateValue() : JValue.CreateNull());
            EventManager = eventManager;

            twinsFamily = Node.GetTwinFamily();

            InitializeComponent();
            oldHeight = Height;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.Selectable, true);

            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;

            AutoScaleMode = AutoScaleMode.None;

            twinFamilyButtonBounds = new Rectangle(1, 1, twinsFamily.Length * 30, 30);

            if (Node.Id is not null)
            {
                EventManager.RegistryEvent(this, Value);
                ValueChanged += (s, ev) => EventManager.GetEvent(Node.Id)?.Invoke(Value);
            }
            RootEditor = rootEditor;
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            if (Node.IsDynamicName)
            {
                if (txtDynamicName is null) CreateDynamicNameTextBox();
                else txtDynamicName.Focus();
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

            if (Parent is JsonJtfEditor)
            {
                Expanded = true;
                if (Node is JtBlock && !IsInvalidValueType)
                {
                    xOffset = 0;
                    xRightOffset = 0;
                    yOffset = 0;
                    innerHeight = 32;
                    return;
                }


            }


            InitDraw(g);
            if (ArrayIndex != -1)
            {
                TabIndex = ArrayIndex;
            }


            DrawTypeIcons(g);
            DrawExpandButton(g);
            DrawName(g);
            DrawRemoveButton(g);
            DrawConditionsLable(g);
            DrawDynamicName(e);
            DrawInvalidValueMessage(g);
        }

        private void DrawConditionsLable(Graphics g)
        {
            if (Node.Conditions.Count > 0 && RootEditor.ShowConditionsCount)
            {

                string msg = string.Format("{0} conditions", Node.Conditions.Count.ToString());


                SizeF msgSize = g.MeasureString(msg, Font);


                g.DrawString(msg, Font, new SolidBrush(hasInvalidConditions ? Color.Red : ForeColor), new PointF(Width - xRightOffset - 10 - msgSize.Width, 16 - msgSize.Height / 2));


                conditionsLabelBounds = new Rectangle((int)(Width - xRightOffset - 10 - msgSize.Width), (int)(16 - msgSize.Height / 2), (int)msgSize.Width, (int)msgSize.Height);
                xRightOffset += (int)msgSize.Width + 20;
            }
        }
        private void InitDraw(Graphics g)
        {
            Color borderColor;

            if (IsInvalidValueType)
                borderColor = Color.Red;
            else if (IsFocused)
                borderColor = Color.Aqua;
            else
                borderColor = Color.FromArgb(200, 200, 200);
            if (IsFocused)
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
        }
        private void DrawInvalidValueMessage(Graphics g)
        {
            if (!IsInvalidValueType)
                return;


            string message = string.Format(Properties.Resources.InvalidValueType, Value.Type, Node.JsonType);

            SizeF sf = g.MeasureString(message, Font);
            g.DrawString(message, Font, new SolidBrush(Color.Red), new PointF(xOffset + 10, 16 - sf.Height / 2));

            xOffset += (int)sf.Width + 10;

            string discardMessage = Properties.Resources.DiscardInvalidType;

            SizeF dsf = g.MeasureString(discardMessage, Font);

            discardInvalidTypeButtonBounds = new Rectangle(xOffset, yOffset, (int)dsf.Width + 10, innerHeight);
            g.FillRectangle(new SolidBrush(Color.Red), discardInvalidTypeButtonBounds);
            g.DrawString(discardMessage, Font, new SolidBrush(Color.White), xOffset + 5, 16 - dsf.Height / 2);

            xOffset += (int)sf.Width + 20;
        }
        private void DrawDynamicName(PaintEventArgs e)
        {
            if (!Node.IsDynamicName)
                return;


            if (Node.Type.IsContainerType)
            {
                dynamicNameTextboxBounds = new Rectangle(xOffset, yOffset, Width - xOffset - xRightOffset, innerHeight);
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(80, 80, 80)), dynamicNameTextboxBounds);

                if (txtDynamicName is not null)
                    return;


                SizeF sf = e.Graphics.MeasureString(DynamicName, Font);

                e.Graphics.DrawString(DynamicName, Font, new SolidBrush(ForeColor), new PointF(xOffset + 10, 16 - sf.Height / 2));
            }
            else
            {
                int size = (Width - xOffset) / 2;

                dynamicNameTextboxBounds = new Rectangle(xOffset, yOffset, size, innerHeight);
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(80, 80, 80)), dynamicNameTextboxBounds);
                if (txtDynamicName is null)
                {

                    SizeF sf = e.Graphics.MeasureString(DynamicName, Font);

                    e.Graphics.DrawString(DynamicName, Font, new SolidBrush(ForeColor), new PointF(xOffset + 10, 16 - sf.Height / 2));
                }
                xOffset += size;
            }
        }
        private void DrawRemoveButton(Graphics g)
        {
            if (!Node.IsArrayPrefab)
                return;

            removeButtonBounds = new Rectangle(Width - xRightOffset - 30, yOffset, 30, innerHeight);
            g.FillRectangle(new SolidBrush(Color.Red), removeButtonBounds);

            g.DrawLine(WhitePen, Width - 20, 12, Width - 12, 20);
            g.DrawLine(WhitePen, Width - 12, 12, Width - 20, 20);

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


                g.DrawString(dn, Font, new SolidBrush(ForeColor), new PointF(xOffset, 16 - nameSize.Height / 2));

                xOffset += (int)nameSize.Width;



                if (Node.Required)
                {
                    g.DrawString("*", Font, new SolidBrush(Color.Gold), new PointF(xOffset, 16 - nameSize.Height / 2));
                }





                xOffset += 20;
                nameLabelBounds = new Rectangle(x, 1, xOffset - x, 30);
            }
            if (ArrayIndex != -1)
            {
                xOffset += 10;

                string arrind = ArrayIndex.ToString();

                SizeF nameSize = g.MeasureString(arrind, Font);

                g.DrawString(arrind, Font, new SolidBrush(ForeColor), new PointF(xOffset, 32 / 2 - nameSize.Height / 2));

                xOffset += (int)nameSize.Width;
                xOffset += 10;
            }
        }
        private void DrawExpandButton(Graphics g)
        {
            if (!Node.Type.IsContainerType || IsInvalidValueType || !CanCollapse)
                return;

            expandButtonBounds = new Rectangle(xOffset, yOffset, 30, innerHeight);

            g.FillRectangle(new SolidBrush(Color.Green), expandButtonBounds);



            g.DrawRectangle(WhitePen, xOffset + 7, 8, 16, 16);
            if (Expanded)
            {
                g.DrawLine(WhitePen, xOffset + 12, 16, xOffset + 18, 16);
            }
            else
            {
                g.DrawLine(WhitePen, xOffset + 12, 16, xOffset + 18, 16);
                g.DrawLine(WhitePen, xOffset + 15, 12, xOffset + 15, 20);
            }
            xOffset += 30;
        }
        private void DrawTypeIcons(Graphics g)
        {
            foreach (JtNode item in twinsFamily)
            {
                if (Node.IsArrayPrefab && Node != item)
                {
                    continue;
                }

                if (Properties.Resources.ResourceManager.GetObject(item.Type.Name) is not Bitmap bmp)
                    continue;

                if (Node == item)
                {
                    g.FillRectangle(new SolidBrush(Color.RoyalBlue), xOffset, yOffset, 30, innerHeight);
                }

                g.DrawImage(bmp, xOffset + 8, 8, 16, 16);

                xOffset += 30;
            }
        }

        internal virtual void CreateEventHandlers()
        {
            if (HaveEventHandlersBeenCreated)
                return;

            HaveEventHandlersBeenCreated = true;


            if (Node.Conditions.Count > 0)
            {

                ChangedEvent? ce = EventManager.GetEvent(Node.Conditions[0].VariableId!);

                if (ce is null)
                {
                    hasInvalidConditions = true;
                    Height = 32;
                    return;
                    
                }

                if (Node.Conditions.Check(JtConditionCollection.CheckOperation.Or, ce.Value?.ToString()) is true)
                {
                    Height = 32;
                    TabStop = true;
                }
                else
                {
                    Height = 0;
                    TabStop = false;
                }
                ce.Event += (s, ev) =>
                    {
                        if (Node.Conditions.Check(JtConditionCollection.CheckOperation.Or, ce.Value?.ToString()) is true)
                        {
                            Height = 32;
                            TabStop = true;
                        }
                        else
                        {
                            Height = 0;
                            TabStop = false;
                        }
                    };
               }
            else
            {
                Height = 32;
            }
        }


        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (expandButtonBounds.Contains(e.Location))
            {
                Expanded = !Expanded;
                return;
            }
            if (removeButtonBounds.Contains(e.Location))
            {
                Parent.Controls.Remove(this);


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
            if (conditionsLabelBounds.Contains(e.Location))
            {
                new ConditionsViewForm(this).ShowDialog();
                return;
            }

            if (new Rectangle(0, 1, twinsFamily.Length * 30, 30).Contains(e.Location))
            {


                JtNode? newType = twinsFamily[(e.Location.X - 1) / 30];

                if (newType == Node)
                    return;
                TwinTypeChanged?.Invoke(this, new TwinChangedEventArgs() { NewTwinNode = newType });

                return;

            }


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
                if (JsonJtfEditor.toolTip.Active)
                    return;
                JsonJtfEditor.toolTip.Active = true;


                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"{Node.Name}");
                if (Node.Id is not null)
                    sb.AppendLine($"Id: {Node.Id}");
                if (Node.Description is not null)
                    sb.AppendLine(Node.Description);
                JsonJtfEditor.toolTip.Show(sb.ToString(), this);
                return;
            }
            else
            {
                if (JsonJtfEditor.toolTip.Active)
                {

                    JsonJtfEditor.toolTip.Active = false;
                    JsonJtfEditor.toolTip.Hide(this);
                }
            }


            Cursor = expandButtonBounds.Contains(e.Location) || removeButtonBounds.Contains(e.Location) || discardInvalidTypeButtonBounds.Contains(e.Location) || twinFamilyButtonBounds.Contains(e.Location) || conditionsLabelBounds.Contains(e.Location)
                ? Cursors.Hand
                : Cursors.Default;
            if (dynamicNameTextboxBounds.Contains(e.Location))
                Cursor = Cursors.IBeam;

        }


        protected void CreateDynamicNameTextBox()
        {
            if (txtDynamicName is not null)
            {
                return;
            }

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
                Width = Width - xOffset - 20 - xRightOffset
            };

            txtDynamicName.Location = new Point(xOffset + 10, 16 - txtDynamicName.Height / 2);
            txtDynamicName.TextChanged += (sender, eventArgs) =>
            {

                DynamicNameChanged?.Invoke(this, EventArgs.Empty);
                DynamicName = txtDynamicName.Text ?? DynamicName;

            };
            txtDynamicName.LostFocus += (sender, eventArgs) =>
            {
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
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (IsInvalidValueType)
                return;

            if (Node.Type.IsContainerType && e.KeyCode == Keys.Space)
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
        public static EditorItem Create(JtNode type, JToken? token, EventManager eventManager, JsonJtfEditor jsonJtfEditor)
        {
            if (type.Type == JtNodeType.Bool) return new BoolEditorItem(type, token, eventManager, jsonJtfEditor);
            if (type.Type == JtNodeType.String) return new StringEditorItem(type, token, eventManager, jsonJtfEditor);
            if (type.Type.IsNumericType) return new NumberEditorItem(type, token, eventManager, jsonJtfEditor);
            if (type.Type == JtNodeType.Enum) return new EnumEditorItem(type, token, eventManager, jsonJtfEditor);
            if (type.Type == JtNodeType.Block) return new BlockEditorItem(type, token, eventManager, jsonJtfEditor);
            if (type.Type == JtNodeType.Array) return new ArrayEditorItem(type, token, eventManager, jsonJtfEditor);

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