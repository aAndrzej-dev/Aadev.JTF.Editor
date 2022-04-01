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

        public ArrayEditorItem(JtToken type, JToken? token, EventManager eventManager) : base(type, token, eventManager) { }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (IsInvalidValueType)
                return;
            Graphics g = e.Graphics;


            addNewButtonBounds = new Rectangle(Width - 30 - xRightOffset, 1, 30, 30);
            g.FillRectangle(new SolidBrush(Color.Green), addNewButtonBounds);
            g.DrawLine(WhitePen, Width - 30 - xRightOffset + 15, 8, Width - 30 - xRightOffset + 15, 24);
            g.DrawLine(WhitePen, Width - 30 - xRightOffset + 7, 16, Width - 30 - xRightOffset + 23, 16);
            xRightOffset += 30;


            string msg = $"{Value.Count()} elements";

            SizeF msgSize = g.MeasureString(msg, Font);

            g.DrawString(msg, Font, new SolidBrush(ForeColor), new PointF(Width - xRightOffset - 10 - msgSize.Width, 16 - msgSize.Height / 2));

            xRightOffset += (int)msgSize.Width;

            

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
                ((JArray?)Value)?.RemoveAt(bei.ArrayIndex);
            }


            BeiResize(bei);
            y += 5;
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

                Expanded = true;
                y -= 5;
                EnsureValue();
                if (Type.MakeAsObject)
                    CreateObjectItem();
                else
                    CreateArrayItem(((JArray)Value).Count);

                y += 5;
                Height = y;

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


        private void BeiResize(EditorItem bei)
        {

            SuspendLayout();
            int oy = bei.Top;
            int index = 0;
            if (!Type.MakeAsObject)
            {
                index = bei.ArrayIndex;
            }

            foreach (EditorItem control in Controls.Cast<EditorItem>().Where(x => x.Top > bei.Top))
            {
                control.Top = oy;
                oy += control.Height;
                oy += 5;
                if (!Type.MakeAsObject)
                {
                    control.ArrayIndex = index;
                    index++;
                }

            }
            y = oy;
            ResumeLayout();
        }
        private void LoadAsArray()
        {

            JArray array = (JArray)Value;

            for (int i = 0; i < array.Count; i++)
            {
                CreateArrayItem(i, array[i]);
            }
            y += 5;
            if (Expanded)
            {
                Height = y;
            }
        }
        private void LoadAsObject()
        {
            foreach (JProperty item in ((JObject)Value).Properties())
            {
                CreateObjectItem(item);
            }
            y += 5;
            if (Expanded)
            {
                Height = y;
            }
        }
        private void CreateArrayItem(int index, JToken? itemValue = null)
        {

            EditorItem bei = Create(Type.Prefabs[0], itemValue, new EventManager());

            JArray value = (JArray)Value;


            bei.Location = new Point(10, y);
            bei.Width = Width - 20;
            bei.CreateEventHandlers();

            bei.ArrayIndex = index;
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
                int ind = bei.ArrayIndex;

                if (Value is not JArray array) return;

                if (array.Count <= ind)
                {
                    while (array.Count < ind)
                    {
                        array.Add(JValue.CreateNull());
                    }
                    array.Add(bei.Value);
                }
                else
                {
                    array[ind] = bei.Value;
                }

                OnValueChanged();


            };

            if (bei.Height != 0)
            {
                y += bei.Height + 5;
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
            EditorItem bei = Create(Type.Prefabs[0], null, new EventManager());


            bei.Location = new Point(10, y);
            bei.Width = Width - 20;
            bei.CreateEventHandlers();
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


            if (bei.Height != 0)
            {
                y += bei.Height + 5;
            }

        }

        internal override void CreateEventHandlers()
        {
            base.CreateEventHandlers();
            foreach (EditorItem item in Controls)
            {
                item.CreateEventHandlers();
            }
        }
    }

}