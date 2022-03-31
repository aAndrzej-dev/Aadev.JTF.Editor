using Aadev.JTF.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Aadev.JTF.Editor.EditorItems
{
    internal class ArrayEditorItem : EditorItem
    {
        private Rectangle addNewButtonBounds = Rectangle.Empty;
        private JToken _value = JValue.CreateNull();
        private int y;
        private readonly Dictionary<string, EditorItem> objectsArray = new();


        public new JtArray Type => (JtArray)base.Type;
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

        public ArrayEditorItem(JtToken type, JToken? token) : base(type, token) { }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;

            if (IsInvalidValueType)
                return;

            addNewButtonBounds = new Rectangle(Width - 30 - xRightOffset, 1, 30, 30);
            g.FillRectangle(new SolidBrush(Color.Green), addNewButtonBounds);
            g.DrawLine(WhitePen, Width - 30 - xRightOffset + 15, 8, Width - 30 - xRightOffset + 15, 24);
            g.DrawLine(WhitePen, Width - 30 - xRightOffset + 7, 16, Width - 30 - xRightOffset + 23, 16);


        }
        protected override void OnExpandChanged()
        {
            if (IsInvalidValueType)
                return;
            if (!Expanded)
            {

                Controls.Clear();
                objectsArray.Clear();
                Height = 32;
                base.OnExpandChanged();
                return;
            }

            y = 38;

            EnsureValue();

            if (Type.MakeAsObject)
                LoadAsObject();
            else
                LoadAsArray();


            base.OnExpandChanged();
        }

        protected override void OnControlRemoved(ControlEventArgs e)
        {
            if (!Expanded)
                return;

            EditorItem bei = (EditorItem)e.Control;
            if (Type.MakeAsObject)
            {

                objectsArray.Remove(bei.DynamicName!);
                ((JObject?)Value)?.Remove(bei.DynamicName!);

            }
            else
            {
                ((JArray?)Value)?.RemoveAt((int)bei.Tag);
            }

            y = BeiResize(e.Control);
            Height = y;
            base.OnControlRemoved(e);
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (IsInvalidValueType)
                return;

            if (addNewButtonBounds.Contains(e.Location))
            {
                EnsureValue();
                if (Type.MakeAsObject)
                    CreateObjectItem();
                else
                    CreateArrayItem(((JArray)Value).Count);

                OnValueChanged();
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (addNewButtonBounds.Contains(e.Location))
            {
                Cursor = Cursors.Hand;
                return;
            }
            base.OnMouseMove(e);
        }

        protected override void CreateValue() => Value = Type.MakeAsObject ? new JObject() : new JArray();

        private void EnsureValue()
        {
            if (Value.Type != Type.JsonType)
                CreateValue();
        }


        private int BeiResize(Control bei)
        {
            int oy = bei.Top;
            int index = 0;
            if (!Type.MakeAsObject)
            {
                index = (int)bei.Tag;
            }

            foreach (Control control in Controls.Cast<Control>().Where(x => x.Top >= bei.Top && x != bei))
            {
                control.Top = oy;
                oy += control.Height;
                oy += 5;
                if (!Type.MakeAsObject)
                {
                    control.Tag = index;
                    index++;
                }

            }
            y = oy;
            return y;
        }
        private void LoadAsArray()
        {
            if (IsInvalidValueType)
                return;


            JArray array = (JArray)Value;

            for (int i = 0; i < array.Count; i++)
            {
                CreateArrayItem(i, array[i]);
            }
        }
        private void LoadAsObject()
        {
            if (IsInvalidValueType)
                return;

            foreach (JProperty item in ((JObject)Value).Properties())
            {
                CreateObjectItem(item);
            }
        }
        private void CreateArrayItem(int index, JToken? itemValue = null)
        {

            EditorItem bei = Create(Type.Prefabs[0], itemValue);

            JArray value = (JArray)Value;


            bei.Location = new Point(10, y);
            bei.Width = Width - 20;


            bei.Tag = index;
            if (itemValue is null)
                value.Add(bei.Value);



            Controls.Add(bei);

            bei.HeightChanged += (sender, ev) =>
            {
                int oy = bei.Top + bei.Height + 5;
                foreach (Control control in Controls.Cast<Control>().Where(x => x.Top >= bei.Top && x != bei))
                {
                    control.Top = oy;
                    oy += control.Height;
                    oy += 5;
                }
                y = oy;
                Height = y;
            };


            bei.ValueChanged += (s, ev) =>
            {
                int ind = (int)bei.Tag;

                if (Value is not JArray array) return;

                if (array.Count <= index)
                {
                    while (array.Count < index)
                    {
                        array.Add(JValue.CreateNull());
                    }
                    array.Add(bei.Value);
                }
                else
                {
                    array[index] = bei.Value;
                }

                OnValueChanged();


            };


            y += 10;
            y += bei.Height;
            if (Expanded)
            {
                Height = y;
            }
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            foreach (Control item in Controls)
            {
                item.Width = Width - 20;
            }

        }
        private void CreateObjectItem(JProperty? item = null)
        {
            EditorItem bei = Create(Type.Prefabs[0], null);


            bei.Location = new Point(10, y);
            bei.Width = Width - 20;

            string newDynamicName = string.Empty;

            if (item is null)
            {
                newDynamicName = $"new {Type.Name} item";
                if (objectsArray.ContainsKey(newDynamicName))
                {
                    int i = 1;
                    while (objectsArray.ContainsKey($"new {Type.Name} item {i}"))
                    {
                        i++;
                    }
                    newDynamicName = $"new {Type.Name} item {i}";


                }
            }

            Controls.Add(bei);

            bei.HeightChanged += (sender, ev) =>
            {
                int oy = bei.Top + bei.Height + 5;
                foreach (Control control in Controls.Cast<Control>().Where(x => x.Top >= bei.Top && x != bei))
                {
                    control.Top = oy;
                    oy += control.Height;
                    oy += 5;
                }
                y = oy;
                Height = y;
            };
            if (item is not null)
            {

                bei.DynamicName = item.Name;
                bei.Value = item.Value;
            }

            bei.ValueChanged += (s, ev) =>
            {
                if (Value is not JObject obj) return;



                KeyValuePair<string, EditorItem> keyValuePair = objectsArray.First(x => x.Value == bei);

                if (bei.DynamicName != keyValuePair.Key)
                {
                    if (objectsArray.ContainsKey(bei.DynamicName!))
                    {
                        MessageBox.Show($"This name allreaby exist: {bei.DynamicName}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        bei.DynamicName = keyValuePair.Key;
                        return;
                    }

                    objectsArray.Remove(keyValuePair.Key);
                    obj.Remove(keyValuePair.Key);

                    objectsArray.Add(bei.DynamicName!, bei);
                }
                obj[bei.DynamicName!] = bei.Value;

                OnValueChanged();



            };
            bei.DynamicNameChanged += (s, ev) => OnValueChanged();
            if (item is null)
                bei.DynamicName = newDynamicName;
            objectsArray.Add(bei.DynamicName!, bei);
            ((JObject)Value)[bei.DynamicName!] = bei.Value;


            y += 10;
            y += bei.Height;
            if (Expanded)
            {
                Height = y;
            }
        }
    }
}