using Aadev.JTF.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace Aadev.JTF.Editor.EditorItems
{
    internal sealed class EnumEditorItem : EditorItem
    {
        private bool InvalidValue => _value is not null && !string.IsNullOrWhiteSpace(_value.ToString()) && !Node.Values.Contains(RawValue) && !Node.AllowCustomValues;
        private JToken _value = JValue.CreateNull();
        private Rectangle discardInvalidValueButtonBounds = Rectangle.Empty;
        private Rectangle comboBoxBounds = Rectangle.Empty;
        private ComboBox? comboBox;

        private new JtEnum Node => (JtEnum)base.Node;

        private JtEnum.EnumValue RawValue
        {
            get
            {
                if (_value.Type == Node.JsonType)
                {
                    if (Node.Values.Any(x => x.Name == (string?)_value))
                    {
                        return Node.Values.FirstOrNull(x => x.Name == (string?)_value) ?? Node.Values.FirstOrNull(x => x.Name == Node.Default) ?? new JtEnum.EnumValue();
                    }
                    if (Node.AllowCustomValues)
                    {
                        return new JtEnum.EnumValue((string)Value!);
                    }
                    return new JtEnum.EnumValue();
                }
                if (_value.Type is JTokenType.Null)
                    return Node.Values.FirstOrNull(x => x.Name == Node.Default) ?? new JtEnum.EnumValue();
                else
                    return new JtEnum.EnumValue();

 
            }

            set => _value = new JValue(value.Name);
        }
        public override JToken Value
        {
            get => _value;
            set
            {
                if (_value.Equals(value))
                    return;
                _value = value;
                Invalidate();
                OnValueChanged();
            }
        }

        protected override bool IsFocused => base.IsFocused || comboBox?.Focused is true || comboBox?.DroppedDown is true;
        internal override bool IsSaveable => Node.Required || (Value.Type != JTokenType.Null && (string?)Value != Node.Default);
        internal EnumEditorItem(JtNode type, JToken? token, EventManager eventManager, JsonJtfEditor jsonJtfEditor) : base(type, token, eventManager, jsonJtfEditor) { }




        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (IsInvalidValueType)
                return;

            if (InvalidValue)
            {
                string message = string.Format(Properties.Resources.InvalidValue, _value.ToString());

                SizeF sf = e.Graphics.MeasureString(message, Font);
                e.Graphics.DrawString(message, Font, new SolidBrush(Color.Red), new PointF(xOffset + 10, 16 - sf.Height / 2));

                xOffset += (int)sf.Width + 10;



                string discardMessage = Properties.Resources.DiscardInvalidValue;


                SizeF dsf = e.Graphics.MeasureString(discardMessage, Font);

                discardInvalidValueButtonBounds = new Rectangle(xOffset, yOffset, (int)dsf.Width + 10, innerHeight);
                e.Graphics.FillRectangle(new SolidBrush(Color.Red), discardInvalidValueButtonBounds);
                e.Graphics.DrawString(discardMessage, Font, new SolidBrush(Color.White), xOffset + 5, 16 - dsf.Height / 2);



                xOffset += (int)sf.Width + 20;


                return;
            }

            comboBoxBounds = new Rectangle(xOffset, yOffset, Width - xOffset - xRightOffset, innerHeight);
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(80, 80, 80)), comboBoxBounds);


            if (comboBox is null)
            {

                SizeF sf = e.Graphics.MeasureString(RawValue.Name, Font);

                e.Graphics.DrawString(RawValue.Name, Font, new SolidBrush(ForeColor), new PointF(xOffset + 12, 16 - sf.Height / 2));
            }
        }


        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);


            if (txtDynamicName is not null)
                return;

            CreateComboBox();
        }
        private void CreateComboBox()
        {
            if (IsInvalidValueType || InvalidValue)
                return;
            if (comboBoxBounds == Rectangle.Empty)
                return;
            if (comboBox is not null)
                return;


            comboBox = new ComboBox
            {
                Font = Font,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(80, 80, 80),
                ForeColor = ForeColor,
                AutoSize = false
            };


            comboBox.Location = new Point(xOffset + 10, 16 - comboBox.Height / 2 - 4);
            comboBox.Width = Width - xOffset - 12 - xRightOffset;
            comboBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            if (Node.AllowCustomValues)
            {
                comboBox.DropDownStyle = ComboBoxStyle.DropDown;
                comboBox.AutoCompleteMode = AutoCompleteMode.Suggest;
                comboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
            }
            else
            {
                comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            }


            Controls.Add(comboBox);

            comboBox.Focus();
            comboBox.DroppedDown = true;


            foreach (JtEnum.EnumValue? item in Node.Values)
            {
                if (item is null)
                    continue;
                comboBox.Items.Add(item);
            }




            comboBox.Text = RawValue.Name;

            comboBox.SelectedIndexChanged += (sender, eventArgs) => Value = comboBox?.Text;


            if (Node.AllowCustomValues)
            {
                comboBox.TextChanged += (sender, eventArgs) => Value = comboBox?.Text;
            }

            comboBox.LostFocus += (s, e) =>
            {
                Value = comboBox?.Text;
                Controls.Remove(comboBox);
                comboBox = null;
                Invalidate();
            };



        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (discardInvalidValueButtonBounds.Contains(e.Location))
            {
                Cursor = Cursors.Hand;
                return;
            }
            base.OnMouseMove(e);
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (discardInvalidValueButtonBounds.Contains(e.Location))
            {
                CreateValue();
                Invalidate();
                return;
            }
            if (comboBoxBounds.Contains(e.Location))
            {
                CreateComboBox();
                Invalidate();
                return;
            }
            base.OnMouseClick(e);
        }
    }
}