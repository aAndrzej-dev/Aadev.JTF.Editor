using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Aadev.JTF.Editor.EditorItems
{
    internal abstract partial class EditorItem : UserControl, IHaveEventManager
    {


        protected virtual bool InvalidValueType => Value is not null && Value.Type is not JTokenType.Null && (Value.Type != Type.JsonType);

        private EventManager? eventManager;
        private readonly JtToken[] twinsFamily;
        private string? dynamicName;
        private int oldHeight;
        private bool expanded;
        private bool isFirstPaint = true;

        protected int xOffset = 0;
        protected int xRightOffset = 0;
        protected TextBox? tbDynamicName;
        protected Rectangle expandButtonBounds = Rectangle.Empty;
        protected Rectangle removeButtonBounds = Rectangle.Empty;
        protected Rectangle discardInvalidTypeButtonBounds = Rectangle.Empty;
        protected Rectangle dynamicNameTextboxBounds = Rectangle.Empty;

        public abstract event EventHandler? ValueChanged;
        public event EventHandler? DynamicNameChanged;
        public event EventHandler<TwinChangedEventArgs>? TwinTypeChanged;
        public event EventHandler? HeightChanged;



        public abstract JToken Value { get; set; }
        public JtToken Type { get; }
        public EventManager? EventManager =>
                Parent is ArrayEditorItem
                    ? (eventManager ??= new EventManager())
                    : Parent is IHaveEventManager em
                        ? em.EventManager
                        : null;
        public string? DynamicName { get => dynamicName; set { dynamicName = value; Invalidate(); } }
        protected bool Expanded { get => expanded; set { expanded = value; OnExpandChanged(); } }

        protected abstract void CreateValue();
        protected abstract void ChangeValue();


        protected virtual void OnExpandChanged() => Invalidate();

        protected EditorItem(JtToken type, JToken? token)
        {
            Type = type;
            Value = token ?? JValue.CreateNull();

            twinsFamily = Type.GetTwinFamily();

            InitializeComponent();
            oldHeight = Height;
            SetStyle(ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);


            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;

            AutoScaleMode = AutoScaleMode.None;



        }

        public static EditorItem? Create(JtToken type, JToken? token)
        {
            if (type.Type == JtTokenType.Bool) return new BoolEditorItem(type, token);
            if (type.Type == JtTokenType.String) return new StringEditorItem(type, token);
            if (type.Type == JtTokenType.Byte || type.Type == JtTokenType.Short || type.Type == JtTokenType.Int || type.Type == JtTokenType.Float || type.Type == JtTokenType.Double) return new NumberEditorItem(type, token);
            if (type.Type == JtTokenType.Enum) return new EnumEditorItem(type, token);
            if (type.Type == JtTokenType.Block) return new BlockEditorItem(type, token);
            if (type.Type == JtTokenType.Array) return new ArrayEditorItem(type, token);

            return null;
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);

            if (string.IsNullOrWhiteSpace(Type.Id))
            {
                return;
            }

            EventManager?.RegistryEvent(Type.Id, Value);

            ValueChanged += (s, ev) => EventManager?.GetEvent(Type.Id)?.Invoke(Value);


        }
        protected override void OnPaint(PaintEventArgs e)
        {
            xOffset = 1;
            xRightOffset = 1;
            Graphics g = e.Graphics;
            ControlPaint.DrawBorder(g, new Rectangle(0, 0, Width, Height), Color.FromArgb(200, 200, 200), ButtonBorderStyle.Solid);



            foreach (JtToken item in twinsFamily)
            {
                string? v = item?.Type.Name!;
                Bitmap? bmp = Properties.Resources.ResourceManager.GetObject(item?.Type.Name!) as Bitmap;
                if (bmp is not null)
                {
                    if (Type == item!)
                    {
                        g.FillRectangle(new SolidBrush(Color.RoyalBlue), xOffset, 1, 30, 30);
                    }

                    g.DrawImage(bmp, xOffset + 8, 8, 16, 16);


                    xOffset += 30;



                }
            }





            if ((Type.Type == JtTokenType.Array || Type.Type == JtTokenType.Block) && !InvalidValueType)
            {
                g.FillRectangle(new SolidBrush(Color.Green), xOffset, 1, 30, 30);

                Pen pen = new(Color.White);

                g.DrawRectangle(pen, xOffset + 7, 8, 16, 16);
                if (Expanded)
                {
                    g.DrawLine(pen, xOffset + 12, 16, xOffset + 18, 16);
                }
                else
                {
                    g.DrawLine(pen, xOffset + 12, 16, xOffset + 18, 16);
                    g.DrawLine(pen, xOffset + 15, 12, xOffset + 15, 20);
                }
                expandButtonBounds = new Rectangle(xOffset, 0, 30, 30);
                xOffset += 30;
            }


            if (!string.IsNullOrWhiteSpace(Type.DisplayName))
            {
                xOffset += 20;


                SizeF nameSize = g.MeasureString(Type.DisplayName, Font);

                g.DrawString(Type.DisplayName, Font, new SolidBrush(ForeColor), new PointF(xOffset, 32 / 2 - nameSize.Height / 2));

                xOffset += (int)nameSize.Width;
                xOffset += 20;
            }

            if (Type.IsArrayPrefab)
            {
                removeButtonBounds = new Rectangle(Width - 32 + 1, 1, 32 - 2, 32 - 2);
                g.FillRectangle(new SolidBrush(Color.Red), removeButtonBounds);

                Pen pen = new(Color.White);
                g.DrawLine(pen, Width - 32 + 12, 12, Width - 32 + 20, 20);
                g.DrawLine(pen, Width - 32 + 20, 12, Width - 32 + 12, 20);

                xRightOffset += 31;
            }
            if (Type.IsDynamicName)
            {

                if (Type.Type == JtTokenType.Array || Type.Type == JtTokenType.Block)
                {
                    e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(80, 80, 80)), xOffset, 1, Width - xOffset - xRightOffset, 30);
                    dynamicNameTextboxBounds = new Rectangle(xOffset, 0, Width - xOffset - xRightOffset, 32);

                    if (tbDynamicName is null)
                    {

                        SizeF sf = e.Graphics.MeasureString(DynamicName, Font);

                        e.Graphics.DrawString(DynamicName, Font, new SolidBrush(ForeColor), new PointF(xOffset + 10, 16 - sf.Height / 2));
                    }
                }
                else
                {
                    int size = (Width - xOffset) / 2;

                    e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(80, 80, 80)), xOffset, 1, size, 30);
                    dynamicNameTextboxBounds = new Rectangle(xOffset, 0, size, 32);
                    xOffset += size;
                    if (tbDynamicName is null)
                    {

                        SizeF sf = e.Graphics.MeasureString(DynamicName, Font);

                        e.Graphics.DrawString(DynamicName, Font, new SolidBrush(ForeColor), new PointF(xOffset + 10, 16 - sf.Height / 2));
                    }
                }


            }

            if (InvalidValueType)
            {

                string message = $"Invalid value type: {Value?.Type}, required: {Type.JsonType}";

                SizeF sf = g.MeasureString(message, Font);
                g.DrawString(message, Font, new SolidBrush(Color.Red), new PointF(xOffset + 10, 16 - sf.Height / 2));

                xOffset += (int)sf.Width + 10;


                string discardMessage = "Discard Invalid Type";

                SizeF dsf = g.MeasureString(discardMessage, Font);

                g.FillRectangle(new SolidBrush(Color.Red), xOffset, 1, dsf.Width + 10, 30);
                g.DrawString(discardMessage, Font, new SolidBrush(Color.White), xOffset + 5, 16 - dsf.Height / 2);


                discardInvalidTypeButtonBounds = new Rectangle(xOffset, 0, (int)dsf.Width + 10, 32);

                xOffset += (int)sf.Width + 20;



            }


            if (isFirstPaint)
            {
                isFirstPaint = false;

                if (Type.Conditions.Count > 0)
                {

                    ChangedEvent? ce = EventManager?.GetEvent(Type?.Conditions?[0].VariableId!);

                    if (ce is null)
                    {
                        throw new Exception("Invalid event id: " + Type?.Conditions?[0].VariableId);
                    }

                    if (Type?.Conditions?.Check(JtConditionCollection.CheckOperation.Or, ce?.Value?.ToString()!) is false)
                    {
                        Height = 0;
                    }
                    ce!.Event += (s, ev) => Height = !Type?.Conditions?.Check(JtConditionCollection.CheckOperation.Or, ce?.Value?.ToString()) is true ? 0 : 32;

                }
                else
                {
                    Height = 32;
                }
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

            Cursor = expandButtonBounds.Contains(e.Location) || removeButtonBounds.Contains(e.Location) || discardInvalidTypeButtonBounds.Contains(e.Location) || (twinsFamily is not null && new Rectangle(0, 1, twinsFamily.Length * 30, 30).Contains(e.Location))
                ? Cursors.Hand
                : Cursors.Default;

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

            };
            tbDynamicName.Width = Width - xOffset - 20 - xRightOffset;
            tbDynamicName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            tbDynamicName.Location = new Point(xOffset + 10, 16 - tbDynamicName.Height / 2 + 2);
            tbDynamicName.TextChanged += (sender, eventArgs) =>
            {
                DynamicNameChanged?.Invoke(this, EventArgs.Empty);
                DynamicName = tbDynamicName.Text ?? DynamicName;

            };
            tbDynamicName.LostFocus += (sender, eventArgs) =>
            {
                DynamicName = tbDynamicName.Text;
                ChangeValue();
                Controls.Remove(tbDynamicName);
                tbDynamicName = null;
            };

            Controls.Add(tbDynamicName);
            tbDynamicName?.Focus();
            tbDynamicName?.SelectAll();
        }
        public class TwinChangedEventArgs : EventArgs
        {
            public JtToken? NewTwinType { get; set; }
        }
    }
}