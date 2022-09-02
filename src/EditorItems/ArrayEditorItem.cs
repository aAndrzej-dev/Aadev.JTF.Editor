using Aadev.JTF.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Aadev.JTF.Editor.EditorItems
{
    internal partial class ArrayEditorItem : EditorItem
    {
        private Rectangle addNewButtonBounds = Rectangle.Empty;
        private JToken value;
        private int y;
        private FocusableControl? focusControl;
        private readonly Dictionary<string, EditorItem> objectsArray = new();
        private readonly ContextMenuStrip? cmsPrefabSelect;
        private JtNode? singlePrefab;
        public JContainer? ValidValue => Value as JContainer;
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
        [MemberNotNullWhen(false, "ValidValue")] public new bool IsInvalidValueType => base.IsInvalidValueType;

        internal override bool IsSaveable => base.IsSaveable || (!IsInvalidValueType && ValidValue.Count > 0);
        protected override bool IsFocused => base.IsFocused || focusControl?.Focused is true;

        internal ArrayEditorItem(JtNode type, JToken? token, JsonJtfEditor jsonJtfEditor, IEventManagerProvider eventManagerProvider) : base(type, token, jsonJtfEditor, eventManagerProvider)
        {
            SetStyle(ControlStyles.ContainerControl, true);

            if (value is null)
                value = (JContainer)Node.CreateDefaultValue();

            if(token is not null && Node.SingleType && ((JContainer)token).Count > 0)
            {
                var jtype = ((JContainer)token)[0]?.Type;
                singlePrefab = Node.Children.Where(x => x.JsonType == jtype).FirstOrDefault();
            }


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
            if (IsInvalidValueType)
                return;

            if (Node.SingleType)
                singlePrefab = prefab;

            Expanded = true;
            y -= 10;
            EnsureValue();
            if (Node.MakeAsObject)
                CreateObjectItem(prefab);
            else
                CreateArrayItem(ValidValue.Count, prefab, null, true);

            y += 10;
            Height = y;

            OnValueChanged();

        }
        private void EnsureValue()
        {
            if (IsInvalidValueType)
                Value = Node.CreateDefaultValue();
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

            Height = y = oy + 10;
            ResumeLayout();
        }
        private void LoadAsArray()
        {
            if (Value is not JArray jArray)
            {
                return;
            }
            for (int i = 0; i < jArray.Count; i++)
            {
                CreateArrayItem(i, Node.Prefabs.FirstOrDefault(x => x.JsonType == jArray[i].Type) ?? Node.Prefabs[0], jArray[i]);
            }
            
        }
        private void LoadAsObject()
        {
            if (Value is not JObject jObject)
            {
                return;
            }
            foreach (JProperty item in jObject.Properties())
            {
                CreateObjectItem(Node.Prefabs.FirstOrDefault(x => x.JsonType == item.Value.Type) ?? Node.Prefabs[0], item);
            }
        }
        private void CreateArrayItem(int index, JtNode prefab, JToken? itemValue = null, bool focus = false)
        {
            EditorItem bei = Create(prefab, itemValue, RootEditor, new BlankEventManagerProvider());


            JArray value = (JArray)Value;


            bei.Location = new Point(10, y);
            bei.Width = Width - 20;

            bei.ArrayIndex = index;
            if (itemValue is null)
            {
                JToken jtoken = bei.Value;
                if (jtoken.Type is JTokenType.Null)
                    jtoken = bei.Node.CreateDefaultValue();
                value.Add(jtoken);
            }


            Controls.Add(bei);

            bei.HeightChanged += (sender, e) =>
            {
                if (sender is not EditorItem bei)
                    return;
                SuspendLayout();
                int oy = bei.Top + bei.Height + 5;
                foreach (Control control in Controls.Cast<Control>().Where(x => x.Top >= bei.Top && x != bei))
                {
                    control.Top = oy;
                    oy += control.Height;
                    oy += 5;
                }
                Height = y = oy + 10;
                ResumeLayout();
            };


            bei.ValueChanged += (sender, e) =>
            {
                if (sender is not EditorItem bei)
                    return;

                int ind = bei.ArrayIndex;

                if (Value is not JArray array)
                    return;

                JToken value = bei.Value;
                if (value.Type is JTokenType.Null)
                    value = bei.Node.CreateDefaultValue();

                if (array.Count <= ind)
                {
                    while (array.Count < ind)
                    {
                        array.Add(JValue.CreateNull());
                    }
                    array.Add(value);
                }
                else
                {
                    array[ind] = value;
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
            EditorItem bei = Create(prefab, item?.Value, RootEditor, new BlankEventManagerProvider());

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
                bei.DynamicName = newDynamicName;
            }
            else
            {
                bei.DynamicName = item.Name;
            }


            Controls.Add(bei);

            bei.HeightChanged += (sender, ev) =>
            {
                if (sender is not EditorItem bei)
                    return;
                SuspendLayout();
                int oy = bei.Top + bei.Height + 5;
                foreach (Control control in Controls.Cast<Control>().Where(x => x.Top >= bei.Top && x != bei))
                {
                    control.Top = oy;
                    oy += control.Height;
                    oy += 5;
                }
                Height = y = oy + 10;
                ResumeLayout();
            };
           

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
                JToken value = bei.Value;
                if (value.Type is JTokenType.Null)
                    value = bei.Node.CreateDefaultValue();
                obj[bei.DynamicName!] = value;

                OnValueChanged();



            };
            bei.DynamicNameChanged += (s, ev) => OnValueChanged();

            objectsArray.Add(bei.DynamicName, bei);

            JToken value = bei.Value;
            if (value.Type is JTokenType.Null)
                value = bei.Node.CreateDefaultValue();
            Value[bei.DynamicName] = value;
            OnValueChanged();

            if (bei.Height != 0)
            {
                y += bei.Height + 5;
            }

        }
        internal void RemoveChild(EditorItem editorItem)
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
            if (ValidValue!.Count == 0)
                singlePrefab = null;
            OnValueChanged();
            UpdateLayout(editorItem);
        }

        protected override Color BorderColor
        {
            get
            {
                if (IsInvalidValueType)
                    return Color.Red;
                if (Node.MaxSize >= 0 && Node.MaxSize < ValidValue.Count)
                    return Color.Yellow;
                return base.BorderColor;
            }
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

            if(singlePrefab is null)
             msg = string.Format(Properties.Resources.ArrayElementsCount, Node.MaxSize >= 0 ? $"{ValidValue.Count}/{Node.MaxSize}" : ValidValue.Count.ToString());
            else
                msg = string.Format(Properties.Resources.ArrayElementsCountOfType, Node.MaxSize >= 0 ? $"{ValidValue.Count}/{Node.MaxSize}" : ValidValue.Count.ToString(), singlePrefab.Type.DisplayName);


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



            if (Node.MakeAsObject)
                LoadAsObject();
            else
                LoadAsArray();

            y += 10;
            if (Expanded)
            {
                Height = y;
            }


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

                if (Node.Prefabs.Count == 0 || (Node.MaxSize >= 0 && Node.MaxSize <= ValidValue.Count))
                    return;



                if (Node.Prefabs.Count > 1)
                {
                    if(Node.SingleType)
                    {
                        if(singlePrefab is not null)
                        {
                            Expanded = true;
                            y -= 10;

                            EnsureValue();
                            if (Node.MakeAsObject)
                                CreateObjectItem(singlePrefab);
                            else
                                CreateArrayItem(((JArray)Value).Count, singlePrefab, null, true);

                            y += 10;
                            Height = y;

                            OnValueChanged();
                            return;
                        }
                    }
                    cmsPrefabSelect!.Show(MousePosition);
                    return;
                }
                Expanded = true;
                y -= 10;

                EnsureValue();
                if (Node.MakeAsObject)
                    CreateObjectItem(Node.Prefabs[0]);
                else
                    CreateArrayItem(((JArray)Value).Count, Node.Prefabs[0], null, true);

                y += 10;
                Height = y;

                OnValueChanged();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if(e.KeyCode == Keys.N && e.Control)
            {
                if (Node.Prefabs.Count == 0)
                    return;
                if (Node.Prefabs.Count > 1)
                {
                    cmsPrefabSelect!.Show(MousePosition);
                    return;
                }
                Expanded = true;
                y -= 10;

                EnsureValue();
                if (Node.MakeAsObject)
                    CreateObjectItem(Node.Prefabs[0]);
                else
                    CreateArrayItem(((JArray)Value).Count, Node.Prefabs[0], null, true);

                y += 10;
                Height = y;

                OnValueChanged();
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (IsInvalidValueType)
                return;
            if (addNewButtonBounds.Contains(e.Location))
            {
                if (Node.Prefabs.Count == 0 || (Node.MaxSize >= 0 && Node.MaxSize <= ValidValue.Count))
                    Cursor = Cursors.No;
                else Cursor = Cursors.Hand;
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