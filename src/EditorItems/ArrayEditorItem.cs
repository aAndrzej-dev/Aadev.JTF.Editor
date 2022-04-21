using Aadev.JTF.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Aadev.JTF.Editor.EditorItems
{
    internal partial class ArrayEditorItem : EditorItem
    {
        private Rectangle addNewButtonBounds = Rectangle.Empty;
        private JToken _value = JValue.CreateNull();
        private int y;
        private readonly Dictionary<string, EditorItem> objectsArray = new();
        private readonly ContextMenuStrip? cmsPrefabSelect;
        private new JtArray Type => (JtArray)base.Type;

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
        internal override bool IsSaveable => Type.Required || Value.Type != JTokenType.Null;
        internal ArrayEditorItem(JtToken type, JToken? token, EventManager eventManager) : base(type, token, eventManager)
        {
            SetStyle(ControlStyles.ContainerControl, true);
            if (Type.Prefabs.Count <= 1)
                return;


            cmsPrefabSelect = new ContextMenuStrip();




            foreach (JtToken? item in Type.Prefabs)
            {
                ToolStripMenuItem? tsmi = new ToolStripMenuItem() { Text = item.Type.DisplayName, Tag = item };


                Bitmap? bmp = Properties.Resources.ResourceManager.GetObject(item.Type.Name) as Bitmap;

                if (bmp is not null)
                    tsmi.Image = bmp;

                tsmi.BackColor = Color.FromArgb(80, 80, 80);
                tsmi.ForeColor = Color.White;

                tsmi.Click += OnPrefabSelect_Click;
                cmsPrefabSelect.Items.Add(tsmi);
            }
            cmsPrefabSelect.BackColor = Color.FromArgb(80, 80, 80);
            cmsPrefabSelect.ForeColor = Color.White;
            cmsPrefabSelect.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());
        }
        private void OnPrefabSelect_Click(object? sender, EventArgs e)
        {



            if (sender is not ToolStripMenuItem control)
                return;


            if (control.Tag is not JtToken prefab)
                return;


            Expanded = true;
            y -= 5;
            EnsureValue();
            if (Type.MakeAsObject)
                CreateObjectItem();
            else
                CreateArrayItem(((JArray)Value).Count, prefab, null, true);

            y += 5;
            Height = y;

            OnValueChanged();

        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (IsInvalidValueType)
                return;
            Graphics g = e.Graphics;

            if (!Type.IsFixedSize)
            {
                addNewButtonBounds = new Rectangle(Width - 30 - xRightOffset, yOffset, 30, innerHeight);
                g.FillRectangle(new SolidBrush(Color.Green), addNewButtonBounds);
                g.DrawLine(WhitePen, Width - 30 - xRightOffset + 15, 8, Width - 30 - xRightOffset + 15, 24);
                g.DrawLine(WhitePen, Width - 30 - xRightOffset + 7, 16, Width - 30 - xRightOffset + 23, 16);
                xRightOffset += 30;
            }


            string msg;

            if (Type.IsFixedSize)
            {
                if (Value.Count() != Type.FixedSize)
                    msg = string.Format(Properties.Resources.ArrayInvalidElementsCount, Value.Count(), Type.FixedSize);
                else msg = string.Format(Properties.Resources.ArrayElementsCount, Value.Count().ToString());
            }
            else
            {
                msg = string.Format(Properties.Resources.ArrayElementsCount, Value.Count().ToString());
            }

            SizeF msgSize = g.MeasureString(msg, Font);

            g.DrawString(msg, Font, new SolidBrush(ForeColor), new PointF(Width - xRightOffset - 10 - msgSize.Width, 16 - msgSize.Height / 2));

            xRightOffset += (int)msgSize.Width;
        }
        protected override void OnExpandChanged()
        {
            Focus(); // To unfocus dynamic name textbox of child
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

            EnsureValue();
            if (Type.MakeAsObject)
            {

                objectsArray.Remove(bei.DynamicName!);
                ((JObject)Value).Remove(bei.DynamicName!);

            }
            else
            {
                ((JArray)Value).RemoveAt(bei.ArrayIndex);
            }


            UpdateLayout(bei);
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

                if (Type.Prefabs.Count == 0)
                    return;



                if (Type.Prefabs.Count > 1)
                {
                    cmsPrefabSelect!.Show(MousePosition);
                    return;
                }
                Expanded = true;
                y -= 5;

                EnsureValue();
                if (Type.MakeAsObject)
                    CreateObjectItem();
                else
                    CreateArrayItem(((JArray)Value).Count, Type.Prefabs[Type.DefaultPrefabIndex], null, true);

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

        protected override JToken CreateValue() => Value = Type.CreateDefaultToken();

        private void EnsureValue()
        {
            if (Value.Type != Type.JsonType)
                CreateValue();
        }


        private void UpdateLayout(EditorItem bei)
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
                CreateArrayItem(i, Type.Prefabs.FirstOrDefault(x => x.JsonType == array[i].Type) ?? Type.Prefabs[Type.DefaultPrefabIndex], array[i]);
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
        private void CreateArrayItem(int index, JtToken prefab, JToken? itemValue = null, bool focus = false)
        {
            EditorItem bei = Create(prefab, itemValue, new EventManager());

            JArray value = (JArray)Value;


            bei.Location = new Point(10, y);
            bei.Width = Width - 20;
            bei.CreateEventHandlers();

            bei.ArrayIndex = index;
            if (itemValue is null)
                value.Add(bei.Value);


            Controls.Add(bei);

            bei.HeightChanged += (sender, e) =>
            {
                if (sender is not EditorItem bei) return;

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


            bei.ValueChanged += (sender, e) =>
            {
                if (sender is not EditorItem bei) return;

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
            if (focus)
                bei.Focus();

            if (bei.Height != 0)
            {
                y += bei.Height + 5;
            }


        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            int w = Width - 20;

            foreach (Control item in Controls)
            {
                if (item.Width != w)
                    item.Width = w;
            }

        }
        private void CreateObjectItem(JProperty? item = null)
        {
            JtToken? type = Type.Prefabs[Type.DefaultPrefabIndex];
            EditorItem bei = Create(type, null, new EventManager());


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
                if (sender is not EditorItem bei) return;
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

            bei.ValueChanged += (sender, e) =>
            {
                if (Value is not JObject obj) return;
                if (sender is not EditorItem bei) return;


                KeyValuePair<string, EditorItem>? keyValuePair = objectsArray.FirstOrDefault(x => x.Value == bei);

                if (keyValuePair is null)
                {
                    objectsArray.Add(bei.DynamicName!, bei);
                    keyValuePair = objectsArray.FirstOrDefault(x => x.Value == bei);
                }


                if (bei.DynamicName != keyValuePair?.Key)
                {
                    if (objectsArray.ContainsKey(bei.DynamicName!))
                    {
                        MessageBox.Show(string.Format(Properties.Resources.ArrayObjectNameExist, bei.DynamicName), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        bei.DynamicName = keyValuePair?.Key;
                        return;
                    }

                    objectsArray.Remove(keyValuePair?.Key);
                    obj.Remove(keyValuePair?.Key);

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