using Aadev.JTF.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Aadev.JTF.Editor.EditorItems
{
    internal partial class ArrayEditorItem : EditorItem
    {
        private Rectangle addNewButtonBounds = Rectangle.Empty;
        private JToken value = JValue.CreateNull();
        private int y;
        private FocusableControl? focusControl;
        private readonly Dictionary<string, EditorItem> objectsArray = new();
        private readonly ContextMenuStrip? cmsPrefabSelect;

        private new JtArray Node => (JtArray)base.Node;
        public override JToken Value
        {
            get => value;
            set
            {
                this.value = value;
                Invalidate();
                OnValueChanged();
            }
        }

        internal override bool IsSaveable => base.IsSaveable || (Value.Type != JTokenType.Null && ((JContainer)Value).Count > 0);
        protected override bool IsFocused => base.IsFocused || focusControl?.Focused is true;

        internal ArrayEditorItem(JtNode type, JToken? token, JsonJtfEditor jsonJtfEditor, EventManager? eventManager = null) : base(type, token, jsonJtfEditor, eventManager)
        {
            SetStyle(ControlStyles.ContainerControl, true);





            if (Node.Prefabs.Count <= 1)
                return;
            cmsPrefabSelect = new ContextMenuStrip();
            foreach (JtNode? item in Node.Prefabs)
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


            if (control.Tag is not JtNode prefab)
                return;


            Expanded = true;
            y -= 5;
            EnsureValue();
            if (Node.MakeAsObject)
                CreateObjectItem(prefab);
            else
                CreateArrayItem(((JArray)Value).Count, prefab, null, true);

            y += 5;
            Height = y;

            OnValueChanged();

        }
        private void EnsureValue()
        {
            if (Value.Type != Node.JsonType)
                CreateValue();
        }
        private void UpdateLayout(EditorItem bei)
        {
            SuspendLayout();
            int oy = bei.Top;
            int index = 0;
            if (!Node.MakeAsObject)
            {
                index = bei.ArrayIndex;
            }

            foreach (EditorItem control in Controls.Cast<Control>().Where(x => x.Top > bei.Top && x is EditorItem))
            {
                if (control.Top != oy)
                    control.Top = oy;
                oy += control.Height;
                oy += 5;
                if (!Node.MakeAsObject)
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
                CreateArrayItem(i, Node.Prefabs.FirstOrDefault(x => x.JsonType == array[i].Type) ?? Node.Prefabs[0], array[i]);
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
                CreateObjectItem(Node.Prefabs.FirstOrDefault(x => x.JsonType == item.Value.Type) ?? Node.Prefabs[0], item);
            }
            y += 5;
            if (Expanded)
            {
                Height = y;
            }
        }
        private void CreateArrayItem(int index, JtNode prefab, JToken? itemValue = null, bool focus = false)
        {
            EditorItem bei = Create(prefab, itemValue, RootEditor, new EventManager(prefab.IdentifiersManager));


            JArray value = (JArray)Value;


            bei.Location = new Point(10, y);
            bei.Width = Width - 20;

            bei.ArrayIndex = index;
            if (itemValue is null)
                value.Add(bei.Value);


            Controls.Add(bei);

            bei.HeightChanged += (sender, e) =>
            {
                if (sender is not EditorItem bei)
                    return;

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
                if (sender is not EditorItem bei)
                    return;

                int ind = bei.ArrayIndex;

                if (Value is not JArray array)
                    return;

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
        private void CreateObjectItem(JtNode prefab, JProperty? item = null)
        {
            EditorItem bei = Create(prefab, null, RootEditor, new EventManager(prefab.IdentifiersManager));


            bei.Location = new Point(10, y);
            bei.Width = Width - 20;




            string newDynamicName = string.Empty;

            if (item is null)
            {
                newDynamicName = $"new {Node.Name} item";
                if (objectsArray.ContainsKey(newDynamicName))
                {
                    int i = 1;
                    while (objectsArray.ContainsKey($"new {Node.Name} item {i}"))
                    {
                        i++;
                    }
                    newDynamicName = $"new {Node.Name} item {i}";


                }
            }

            Controls.Add(bei);

            bei.HeightChanged += (sender, ev) =>
            {
                if (sender is not EditorItem bei)
                    return;
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
                if (Value is not JObject obj)
                    return;
                if (sender is not EditorItem bei)
                    return;


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
                    if (keyValuePair?.Key is not null)
                    {
                        objectsArray.Remove(keyValuePair?.Key!);
                        obj.Remove(keyValuePair?.Key!);
                    }


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
        internal void RemoverChild(EditorItem editorItem)
        {
            Focus();

            EnsureValue();
            Controls.Remove(editorItem);
            if (Node.MakeAsObject)
            {
                if (editorItem.DynamicName is not null)
                {
                    objectsArray.Remove(editorItem.DynamicName);
                    ((JObject)value)!.Remove(editorItem.DynamicName);

                }

            }
            else
            {
                ((JArray)value)!.RemoveAt(editorItem.ArrayIndex);
            }
            OnValueChanged();
            UpdateLayout(editorItem);
            y += 5;
            Height = y;
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (IsInvalidValueType)
                return;
            Graphics g = e.Graphics;


            addNewButtonBounds = new Rectangle(Width - 30 - xRightOffset, yOffset, 30, innerHeight);

            if (Expanded && !Node.IsDynamicName)
            {
                RectangleF bounds = new RectangleF(addNewButtonBounds.Location, addNewButtonBounds.Size);
                g.SmoothingMode = SmoothingMode.HighQuality;
                using GraphicsPath rectPath = new GraphicsPath();

                bounds.Offset(-0.5f, -0.5f);
                float w = bounds.X + bounds.Width;
                float h = bounds.Y + bounds.Height;
                rectPath.AddLine(bounds.X, bounds.Y, w, bounds.Y);
                rectPath.AddLine(w, bounds.Y, w, h);
                rectPath.AddArc(bounds.X, h - 10, 10, 10, 90, 90);
                g.FillPath(greenBrush, rectPath);

                g.SmoothingMode = SmoothingMode.Default;
            }
            else
                g.FillRectangle(greenBrush, addNewButtonBounds);
            g.DrawLine(whitePen, Width - 30 - xRightOffset + 15, 8, Width - 30 - xRightOffset + 15, 24);
            g.DrawLine(whitePen, Width - 30 - xRightOffset + 7, 16, Width - 30 - xRightOffset + 23, 16);
            xRightOffset += 30;


            string msg;


            msg = string.Format(Properties.Resources.ArrayElementsCount, Value.Count().ToString());

            SizeF msgSize = g.MeasureString(msg, Font);

            g.DrawString(msg, Font, new SolidBrush(ForeColor), new PointF(Width - xRightOffset - 10 - msgSize.Width, 16 - msgSize.Height / 2));

            xRightOffset += (int)msgSize.Width;
        }
        protected override void OnExpandChanged()
        {
            Focus(); // To unfocus dynamic name textbox of child
            if (IsInvalidValueType)
                return;
            SuspendLayout();
            if (!Expanded)
            {
                focusControl = null;
                Controls.Clear();
                objectsArray.Clear();

                Height = 32;
                base.OnExpandChanged();
                ResumeLayout();
                return;
            }


            focusControl = new FocusableControl
            {
                Height = 0,
                Width = 0,
                Top = 0,
                Left = 0
            };
            focusControl.GotFocus += (s, e) => Invalidate();
            focusControl.LostFocus += (s, e) => Invalidate();
            focusControl.KeyDown += (s, e) =>
            {
                if (IsInvalidValueType)
                    return;

                if (e.KeyCode == Keys.Space)
                {
                    Expanded = !Expanded;
                }
            };
            Controls.Add(focusControl);
            focusControl?.Focus();


            y = 38;

            EnsureValue();

            if (Node.MakeAsObject)
                LoadAsObject();
            else
                LoadAsArray();


            base.OnExpandChanged();
            ResumeLayout();
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (IsInvalidValueType)
                return;
            if (!Expanded)
            {
                Focus();
            }
            else
            {
                focusControl?.Focus();
            }
            if (e.Button != MouseButtons.Left)
                return;
            if (addNewButtonBounds.Contains(e.Location))
            {

                if (Node.Prefabs.Count == 0)
                    return;



                if (Node.Prefabs.Count > 1)
                {
                    cmsPrefabSelect!.Show(MousePosition);
                    return;
                }
                Expanded = true;
                y -= 5;

                EnsureValue();
                if (Node.MakeAsObject)
                    CreateObjectItem(Node.Prefabs[0]);
                else
                    CreateArrayItem(((JArray)Value).Count, Node.Prefabs[0], null, true);

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


    }

}