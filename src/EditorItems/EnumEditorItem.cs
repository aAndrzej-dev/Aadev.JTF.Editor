using Aadev.JTF.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Windows.Forms;
namespace Aadev.JTF.Editor.EditorItems
{
    internal sealed class EnumEditorItem : EditorItem
    {
        private bool InvalidValue => _value is not null && !string.IsNullOrWhiteSpace(_value.ToString()) && !Type.Values.Contains(RawValue) && !Type.AllowCustomValues;
        private JToken _value = JValue.CreateNull();
        private Rectangle discardInvalidValueButtonBounds = Rectangle.Empty;
        private Rectangle comboBoxBounds = Rectangle.Empty;
        private ComboBox? comboBox;

        private new JtEnum Type => (JtEnum)base.Type;

        private string? RawValue
        {
            get => _value.Type == Type.JsonType ? (Type.Values.Contains((string?)_value) || Type.AllowCustomValues ? ((string?)_value ?? Type.Default) : null) : (_value.Type is JTokenType.Null ? Type.Default : null); set => _value = new JValue(value);
        }
        public override JToken Value
        {
            get => _value;
            set
            {
                _value = value;
                Invalidate();
                OnValueChanged();
            }
        }

        protected override bool IsFocused => Focused || comboBox?.Focused is true || comboBox?.DroppedDown is true;
        internal override bool IsSaveable => Type.Required || (Value.Type != JTokenType.Null && (string?)Value != Type.Default);
        internal EnumEditorItem(JtToken type, JToken? token, EventManager eventManager) : base(type, token, eventManager) { }




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

            bool createComboBox = false;
            if (Focused && comboBoxBounds == Rectangle.Empty)
            {
                createComboBox = true;
            }

            comboBoxBounds = new Rectangle(xOffset, yOffset, Width - xOffset - xRightOffset, innerHeight);
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(80, 80, 80)), comboBoxBounds);


            if (comboBox == null)
            {

                SizeF sf = e.Graphics.MeasureString(RawValue, Font);

                e.Graphics.DrawString(RawValue, Font, new SolidBrush(ForeColor), new PointF(xOffset + 12, 16 - sf.Height / 2));
            }
            if (createComboBox)
            {
                CreateComboBox();
            }
        }


        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
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
                AutoSize = false,
                DropDownStyle = ComboBoxStyle.DropDownList,
            };


            comboBox.Location = new System.Drawing.Point(xOffset + 10, 14 - comboBox.Height / 2);
            comboBox.Width = Width - xOffset - 20 - xRightOffset;
            comboBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;



            Controls.Add(comboBox);

            comboBox.Focus();
            comboBox.DroppedDown = true;


            foreach (string? item in Type.Values)
            {
                if (item is null)
                    continue;
                comboBox.Items.Add(item);
            }


            if (Type.AllowCustomValues)
            {
                comboBox.DropDownStyle = ComboBoxStyle.DropDown;
            }

            comboBox.Text = RawValue;

            comboBox.SelectedIndexChanged += (sender, eventArgs) => Value = comboBox?.Text;

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
        protected override JToken CreateValue() => Value = Type.CreateDefaultToken();
    }
}