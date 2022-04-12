using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Aadev.JTF.Editor.EditorItems
{
    internal abstract partial class EditorItem : UserControl
    {
        protected static Pen WhitePen = new Pen(Color.White);
        protected virtual bool IsFocused => Focused;

        protected bool IsInvalidValueType => Value.Type is not JTokenType.Null && (Value.Type != Type.JsonType);

        private string? dynamicName;
        private int oldHeight;
        private bool expanded;


        protected bool AreEventHandlersCreated { get; private set; }
        protected readonly JtToken[] twinsFamily;
        protected int xOffset = 0;
        protected int xRightOffset = 0;
        protected int yOffset = 0;
        protected int innerHeight = 0;
        protected TextBox? tbDynamicName;
        protected Rectangle expandButtonBounds = Rectangle.Empty;
        protected Rectangle removeButtonBounds = Rectangle.Empty;
        protected Rectangle discardInvalidTypeButtonBounds = Rectangle.Empty;
        protected Rectangle dynamicNameTextboxBounds = Rectangle.Empty;
        protected Rectangle twinFamilyButtonBounds = Rectangle.Empty;
        protected Rectangle nameLabelBounds = Rectangle.Empty;


        public event EventHandler? ValueChanged;
        public event EventHandler? DynamicNameChanged;
        internal event EventHandler<TwinChangedEventArgs>? TwinTypeChanged;
        internal event EventHandler? HeightChanged;


        internal int ArrayIndex { get; set; } = -1;
        public abstract JToken Value { get; set; }
        public JtToken Type { get; }
        protected EventManager EventManager { get; }

        internal abstract bool IsSaveable { get; }


        public string? DynamicName { get => dynamicName; set { dynamicName = value; Invalidate(); } }
        protected bool Expanded { get => expanded; set { if (expanded == value) return; expanded = value; OnExpandChanged(); } }

        protected abstract JToken CreateValue();
        protected void OnValueChanged() => ValueChanged?.Invoke(this, EventArgs.Empty);
        protected virtual void OnExpandChanged()
        {
            Focus();
            Invalidate();

        }

        protected EditorItem(JtToken type, JToken? token, EventManager eventManager)
        {
            Type = type;
            Value = token ?? ((Type.Required || Type.IsArrayPrefab) ? CreateValue() : JValue.CreateNull());
            EventManager = eventManager;

            twinsFamily = Type.GetTwinFamily();

            InitializeComponent();
            oldHeight = Height;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.Selectable, true);

            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;

            AutoScaleMode = AutoScaleMode.None;

            twinFamilyButtonBounds = new Rectangle(0, 1, twinsFamily.Length * 30, 30);


            EventManager.RegistryEvent(Type.Id, Value);

            ValueChanged += (s, ev) => EventManager.GetEvent(Type.Id)?.Invoke(Value);
        }

        public static EditorItem Create(JtToken type, JToken? token, EventManager eventManager)
        {
            if (type.Type == JtTokenType.Bool) return new BoolEditorItem(type, token, eventManager);
            if (type.Type == JtTokenType.String) return new StringEditorItem(type, token, eventManager);
            if (type.Type.IsNumericType) return new NumberEditorItem(type, token, eventManager);
            if (type.Type == JtTokenType.Enum) return new EnumEditorItem(type, token, eventManager);
            if (type.Type == JtTokenType.Block) return new BlockEditorItem(type, token, eventManager);
            if (type.Type == JtTokenType.Array) return new ArrayEditorItem(type, token, eventManager);

            throw new ArgumentOutOfRangeException(nameof(type));
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

            Graphics g = e.Graphics;

            if (IsFocused)
            {
                ControlPaint.DrawBorder(g, new Rectangle(0, 0, Width, Height), Color.Aqua, 2, ButtonBorderStyle.Solid, Color.Aqua, 2, ButtonBorderStyle.Solid, Color.Aqua, 2, ButtonBorderStyle.Solid, Color.Aqua, 2, ButtonBorderStyle.Solid);
                xOffset = 2;
                xRightOffset = 2;

                yOffset = 2;
                innerHeight = 28;
            }

            else
            {
                ControlPaint.DrawBorder(g, new Rectangle(0, 0, Width, Height), Color.FromArgb(200, 200, 200), ButtonBorderStyle.Solid);
                xOffset = 1;
                xRightOffset = 1;
                yOffset = 1;
                innerHeight = 30;
            }



            foreach (JtToken item in twinsFamily)
            {
                if (Type.IsArrayPrefab && Type != item)
                {
                    continue;
                }

                Bitmap? bmp = Properties.Resources.ResourceManager.GetObject(item.Type.Name) as Bitmap;
                if (bmp is not null)
                {
                    if (Type == item)
                    {
                        g.FillRectangle(new SolidBrush(Color.RoyalBlue), xOffset, yOffset, 30, innerHeight);
                    }

                    g.DrawImage(bmp, xOffset + 8, 8, 16, 16);

                    xOffset += 30;
                }
            }

            if (Type.Type.IsContainerType && !IsInvalidValueType)
            {
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


            if (!string.IsNullOrWhiteSpace(Type.DisplayName))
            {
                int x = xOffset;
                xOffset += 20;

                string dn = ConvertToFriendlyName(Type.DisplayName);

                SizeF nameSize = g.MeasureString(dn, Font);


                g.DrawString(dn, Font, new SolidBrush(ForeColor), new PointF(xOffset, 32 / 2 - nameSize.Height / 2));

                xOffset += (int)nameSize.Width;

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


            if (Type.IsArrayPrefab)
            {
                removeButtonBounds = new Rectangle(Width - xRightOffset - 30, yOffset, 30, innerHeight);
                g.FillRectangle(new SolidBrush(Color.Red), removeButtonBounds);

                g.DrawLine(WhitePen, Width - 20, 12, Width - 12, 20);
                g.DrawLine(WhitePen, Width - 12, 12, Width - 20, 20);

                xRightOffset += 30;
            }
            if (Type.IsDynamicName)
            {

                if (Type.Type.IsContainerType)
                {
                    dynamicNameTextboxBounds = new Rectangle(xOffset, yOffset, Width - xOffset - xRightOffset, innerHeight);
                    e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(80, 80, 80)), dynamicNameTextboxBounds);

                    if (tbDynamicName is null)
                    {

                        SizeF sf = e.Graphics.MeasureString(DynamicName, Font);

                        e.Graphics.DrawString(DynamicName, Font, new SolidBrush(ForeColor), new PointF(xOffset + 10, 16 - sf.Height / 2));
                    }
                }
                else
                {
                    int size = (Width - xOffset) / 2;

                    dynamicNameTextboxBounds = new Rectangle(xOffset, yOffset, size, innerHeight);
                    e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(80, 80, 80)), dynamicNameTextboxBounds);
                    if (tbDynamicName is null)
                    {

                        SizeF sf = e.Graphics.MeasureString(DynamicName, Font);

                        e.Graphics.DrawString(DynamicName, Font, new SolidBrush(ForeColor), new PointF(xOffset + 10, 16 - sf.Height / 2));
                    }
                    xOffset += size;
                }


            }

            if (IsInvalidValueType)
            {

                string message = string.Format(Properties.Resources.InvalidValueType, Value.Type, Type.JsonType);

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
        }



        internal virtual void CreateEventHandlers()
        {
            if (AreEventHandlersCreated)
                return;

            AreEventHandlersCreated = true;


            if (Type.Conditions.Count > 0)
            {

                ChangedEvent? ce = EventManager.GetEvent(Type.Conditions[0].VariableId!);

                if (ce is null)
                {
                    throw new Exception($"Invalid event id: {Type.Conditions[0].VariableId}");
                }

                Height = Type.Conditions.Check(JtConditionCollection.CheckOperation.Or, ce.Value?.ToString()) is true ? 32 : 0;
                ce.Event += (s, ev) => Height = Type.Conditions.Check(JtConditionCollection.CheckOperation.Or, ev.Value?.ToString()) ? 32 : 0;
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
                Invalidate();
                return;
            }
            if (dynamicNameTextboxBounds.Contains(e.Location))
            {
                CreateTextBox();
                return;
            }

            if (twinsFamily is not null && new Rectangle(0, 1, twinsFamily.Length * 30, 30).Contains(e.Location))
            {
                if ((e.Location.X - 1) / 30 + 1 <= twinsFamily.Length)
                {
                    if (twinsFamily[(e.Location.X - 1) / 30] != Type)
                    {
                        TwinTypeChanged?.Invoke(this, new TwinChangedEventArgs() { NewTwinType = twinsFamily[(e.Location.X - 1) / 30] });
                    }
                }
                return;

            }


        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (oldHeight != Height)
            {
                HeightChanged?.Invoke(this, EventArgs.Empty);
                oldHeight = Height;
            }

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

                JsonJtfEditor.toolTip.Show($"{Type.Name}\n{Type.Description}", this);
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


            Cursor = expandButtonBounds.Contains(e.Location) || removeButtonBounds.Contains(e.Location) || discardInvalidTypeButtonBounds.Contains(e.Location) || twinFamilyButtonBounds.Contains(e.Location)
                ? Cursors.Hand
                : Cursors.Default;
            if (dynamicNameTextboxBounds.Contains(e.Location))
                Cursor = Cursors.IBeam;

        }


        private void CreateTextBox()
        {
            if (tbDynamicName is not null)
            {
                return;
            }

            tbDynamicName = new TextBox
            {
                Font = Font,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = ForeColor,
                AutoSize = false,


                Text = DynamicName,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Width = Width - xOffset - 20 - xRightOffset
            };

            tbDynamicName.Location = new Point(xOffset + 10, 16 - tbDynamicName.Height / 2 + 2);
            tbDynamicName.TextChanged += (sender, eventArgs) =>
            {
                DynamicNameChanged?.Invoke(this, EventArgs.Empty);
                DynamicName = tbDynamicName.Text ?? DynamicName;

            };
            tbDynamicName.LostFocus += (sender, eventArgs) =>
            {
                DynamicName = tbDynamicName.Text;
                OnValueChanged();
                Controls.Remove(tbDynamicName);
                tbDynamicName = null;
            };

            Controls.Add(tbDynamicName);
            tbDynamicName?.Focus();
            tbDynamicName?.SelectAll();
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (IsInvalidValueType)
                return;

            if (Type.Type.IsContainerType && e.KeyCode == Keys.Space)
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
                    if (name[i] == '_')
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
    }
}