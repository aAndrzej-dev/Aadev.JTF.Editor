using Aadev.JTF.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Windows.Forms;
namespace Aadev.JTF.Editor.EditorItems
{
    internal sealed class EnumEditorItem : EditorItem
    {
        private bool InvalidValue => _value is not null && !string.IsNullOrWhiteSpace(_value.ToString()) && !Type.Values.Contains(RawValue);
        private JToken _value = JValue.CreateNull();
        private Rectangle discardInvalidValueButtonBounds = Rectangle.Empty;
        private ComboBox? comboBox;
        public override event EventHandler? ValueChanged;

        private new JtEnum Type => (JtEnum)base.Type;

        private string? RawValue
        {
            get => _value?.Type == Type.JsonType ? (Type.Values.Contains((string?)_value) ? ((string?)_value ?? Type.Default) : null) : (_value?.Type is JTokenType.Null ? Type.Default : null); set => _value = new JValue(value);
        }
        public override JToken Value
        {
            get => _value;
            set
            {
                _value = value;
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }



        internal EnumEditorItem(JtToken type, JToken? token) : base(type, token)
        {
        }





        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (InvalidValueType)
                return;

            if (InvalidValue)
            {
                string message = $"Invalid value: '{_value}'";

                SizeF sf = e.Graphics.MeasureString(message, Font);
                e.Graphics.DrawString(message, Font, new SolidBrush(Color.Red), new PointF(xOffset + 10, 16 - sf.Height / 2));

                xOffset += (int)sf.Width + 10;



                string discardMessage = "Discard Invalid Value";

                SizeF dsf = e.Graphics.MeasureString(discardMessage, Font);

                e.Graphics.FillRectangle(new SolidBrush(Color.Red), xOffset, 1, dsf.Width + 10, 30);
                e.Graphics.DrawString(discardMessage, Font, new SolidBrush(Color.White), xOffset + 5, 16 - dsf.Height / 2);


                discardInvalidValueButtonBounds = new Rectangle(xOffset, 0, (int)dsf.Width + 10, 32);

                xOffset += (int)sf.Width + 20;


                return;
            }


            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(80, 80, 80)), xOffset, 1, Width - xOffset - xRightOffset, 30);


            if (comboBox == null)
            {

                SizeF sf = e.Graphics.MeasureString(RawValue, Font);

                e.Graphics.DrawString(RawValue, Font, new SolidBrush(ForeColor), new PointF(xOffset + 12, 16 - sf.Height / 2));
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            CreateComboBox();
        }

        private void CreateComboBox()
        {
            if (InvalidValueType || InvalidValue)
                return;
            if (comboBox != null)
            {
                return;
            }

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


            if (Type.CanUseCustomValue)
            {
                comboBox.DropDownStyle = ComboBoxStyle.DropDown;
            }

            comboBox.SelectedItem = RawValue;

            comboBox.SelectedIndexChanged += (sender, eventArgs) => Value = comboBox?.SelectedItem?.ToString();

            comboBox.LostFocus += (s, e) =>
            {
                Controls.Remove(comboBox);
                comboBox = null;
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
            base.OnMouseClick(e);
        }
        protected override void CreateValue() => Value = Type.Default;
        protected override void ChangeValue() => ValueChanged?.Invoke(this, EventArgs.Empty);
    }
}