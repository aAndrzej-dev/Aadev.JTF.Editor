using Aadev.JTF.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
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
        private ContextMenuStrip? cmsPrefabSelect;
        private JtNode? singlePrefab;
        public JContainer? ValidValue => Value as JContainer;
        private new JtArrayNode Node => (JtArrayNode)base.Node;
        public override JToken Value
        {
            get => value;
            set
            {
                JToken oldValue = this.value;
                this.value = value;
                Invalidate();
                OnValueChanged(new JtfEditorAction(JtfEditorAction.JtEditorActionType.ChangeValue, oldValue, value, this));
            }
        }
        [MemberNotNullWhen(false, nameof(ValidValue))] public new bool IsInvalidValueType => base.IsInvalidValueType;

        internal override bool IsSavable => base.IsSavable || (!IsInvalidValueType && ValidValue.Count > 0);
        protected override bool IsFocused => base.IsFocused || focusControl?.Focused is true;

        internal ArrayEditorItem(JtArrayNode type, JToken? token, JsonJtfEditor jsonJtfEditor, EventManager eventManager) : base(type, token, jsonJtfEditor, eventManager)
        {
            SetStyle(ControlStyles.ContainerControl, true);

            value ??= (JContainer)Node.CreateDefaultValue();

            if (token is not null && Node.SingleType && ((JContainer)token).Count > 0)
            {
                JTokenType? jtype = ((JContainer)token)[0]?.Type;
                singlePrefab = Node.Children.Nodes!.Where(x => x.JsonType == jtype).FirstOrDefault();
            }


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
            JToken value;
            if (Node.ContainerJsonType is JtContainerType.Block)
                value = CreateObjectItem(prefab).Value;
            else
                value = CreateArrayItem(ValidValue.Count, prefab, null, true).Value;

            y += 10;
            Height = y;

            OnValueChanged(new JtfEditorAction(JtfEditorAction.JtEditorActionType.AddToken, null, value, this));

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
            if (Node.ContainerJsonType is JtContainerType.Array)
            {
                index = bei.ArrayIndex;
            }

            foreach (EditorItem control in Controls.Cast<Control>().Where(x => x.Top > bei.Top && x is EditorItem))
            {
                if (control.Top != oy)
                    control.Top = oy;
                oy += control.Height;
                oy += 5;
                if (Node.ContainerJsonType is JtContainerType.Array)
                {
                    control.ArrayIndex = index;
                    index++;
                }

            }

            y = oy + 10;
            Height = y;
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
                CreateArrayItem(i, Node.Prefabs.Nodes!.FirstOrDefault(x => CheckPrefab(x, jArray[i])) ?? Node.Prefabs.Nodes![0], jArray[i]);
            }

        }
        private static bool CheckPrefab(JtNode prefab, JToken value)
        {
            if (prefab.JsonType != value.Type)
                return false;

            if (prefab is JtBlockNode b && b.JsonType is JTokenType.Object)
            {
                foreach (JProperty item in ((JObject)value).Properties())
                {
                    if (!b.Children.Nodes!.Any(x => x.Name == item.Name))
                        return false;

                }
            }
            return true;

        }
        private void LoadAsObject()
        {
            if (Value is not JObject jObject)
            {
                return;
            }
            foreach (JProperty item in jObject.Properties())
            {
                CreateObjectItem(Node.Prefabs.Nodes!.FirstOrDefault(x => x.JsonType == item.Value.Type) ?? Node.Prefabs.Nodes![0], item);
            }
        }
        private EditorItem CreateArrayItem(int index, JtNode prefab, JToken? itemValue = null, bool focus = false)
        {
            EditorItem bei = Create(prefab, itemValue, RootEditor, new EventManager(prefab.IdentifiersManager, eventManager));


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

                OnValueChanged(e);


            };
            if (focus)
                bei.Focus();

            if (bei.Height != 0)
            {
                y += bei.Height + 5;
            }
            return bei;

        }
        private EditorItem CreateObjectItem(JtNode prefab, JProperty? item = null)
        {
            EditorItem bei = Create(prefab, item?.Value, RootEditor, new EventManager(prefab.IdentifiersManager, eventManager));

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
                int oy = bei.Top + bei.Height + 5;
                foreach (Control control in Controls.Cast<Control>().Where(x => x.Top > bei.Top && x is EditorItem))
                {
                    if (control.Top != oy)
                        control.Top = oy;
                    oy += control.Height;
                    oy += 5;

                }
                Height = y = oy + 10;
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
                        MessageBox.Show(string.Format(CultureInfo.CurrentCulture, Properties.Resources.ArrayObjectNameExist, bei.DynamicName), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        bei.DynamicName = keyValuePair?.Key;
                        bei.Focus();
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
                bei.DynamicName ??= keyValuePair!.Value.Key;
                obj[bei.DynamicName!] = value;

                OnValueChanged(e);



            };
            bei.DynamicNamePreviewChange += (s, ev) => OnValueChanged(new JtfEditorAction(JtfEditorAction.JtEditorActionType.None, null, null, this));

            objectsArray.Add(bei.DynamicName, bei);

            JToken value = bei.Value;
            if (value.Type is JTokenType.Null)
                value = bei.Node.CreateDefaultValue();
            if (!JToken.DeepEquals(Value[bei.DynamicName], value))
            {
                JToken? oldValue = Value[bei.DynamicName];

                Value[bei.DynamicName] = value;
                OnValueChanged(new JtfEditorAction(JtfEditorAction.JtEditorActionType.ChangeValue, oldValue, Value[bei.DynamicName], this));
            }

            if (bei.Height != 0)
            {
                y += bei.Height + 5;
            }

            return bei;
        }
        internal void RemoveChild(EditorItem editorItem)
        {
            Focus();

            EnsureValue();
            Controls.Remove(editorItem);
            JToken? oldValue;
            if (Node.MakeAsObject)
            {
                if (editorItem.DynamicName is not null)
                {
                    objectsArray.Remove(editorItem.DynamicName);
                    oldValue = ((JObject)value)[editorItem.DynamicName];
                    ((JObject)value)!.Remove(editorItem.DynamicName);

                }
                oldValue = null;
            }
            else
            {
                oldValue = ((JArray)value)[editorItem.ArrayIndex];
                ((JArray)value)!.RemoveAt(editorItem.ArrayIndex);
            }
            if (ValidValue!.Count == 0)
                singlePrefab = null;
            OnValueChanged(new JtfEditorAction(JtfEditorAction.JtEditorActionType.RemoveToken, oldValue, null, this));
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

        internal JtNode? SinglePrefab => singlePrefab;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (IsInvalidValueType)
                return;
            Graphics g = e.Graphics;


            int addWidth = (IsFocused && !Node.IsArrayPrefab) ? 29 : 30;
            addNewButtonBounds = new Rectangle(Width - xRightOffset - addWidth, yOffset, addWidth, innerHeight);
            if (!RootEditor.ReadOnly)
            {
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
                    g.FillPath(RootEditor.ColorTable.AddItemButtonBackBrush, rectPath);

                    g.SmoothingMode = SmoothingMode.Default;
                }
                else
                    g.FillRectangle(RootEditor.ColorTable.AddItemButtonBackBrush, addNewButtonBounds);
                g.DrawLine(RootEditor.ColorTable.RemoveItemButtonForePen, Width - addWidth - xRightOffset + 15, 8, Width - addWidth - xRightOffset + 15, 24);
                g.DrawLine(RootEditor.ColorTable.RemoveItemButtonForePen, Width - addWidth - xRightOffset + 7, 16, Width - addWidth - xRightOffset + 23, 16);
                xRightOffset += addWidth;

            }



            string msg;


            if (singlePrefab is null)
                msg = string.Format(CultureInfo.CurrentCulture, Properties.Resources.ArrayElementsCount, Node.MaxSize >= 0 ? $"{ValidValue.Count}/{Node.MaxSize}" : ValidValue.Count.ToString(CultureInfo.CurrentCulture));
            else
                msg = string.Format(CultureInfo.CurrentCulture, Properties.Resources.ArrayElementsCountOfType, Node.MaxSize >= 0 ? $"{ValidValue.Count}/{Node.MaxSize}" : ValidValue.Count.ToString(CultureInfo.CurrentCulture), singlePrefab.Type.DisplayName);


            SizeF msgSize = g.MeasureString(msg, Font);

            g.DrawString(msg, Font, ForeColorBrush, new PointF(Width - xRightOffset - 10 - msgSize.Width, 16 - msgSize.Height / 2));

            xRightOffset += (int)msgSize.Width;
        }
        protected override void OnExpandChanged()
        {
            Focus(); // To unfocus dynamic name textbox of child
            if (IsInvalidValueType)
                return;
            SuspendLayout();
            objectsArray.Clear();
            if (!Expanded)
            {
                focusControl = null;
                Controls.Clear();

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
        private void InitContextMenu()
        {
            if (cmsPrefabSelect is not null || Node.Prefabs.Nodes!.Count <= 1)
                return;
            cmsPrefabSelect = new ContextMenuStrip();
            Span<JtNode> collectionSpan = CollectionsMarshal.AsSpan(Node.Prefabs.Nodes!);
            for (int i = 0; i < collectionSpan.Length; i++)
            {
                JtNode? item = collectionSpan[i];
                ToolStripMenuItem? tsmi = new ToolStripMenuItem() { Tag = item };

                if (item.Name is null)
                    tsmi.Text = item.Type.DisplayName;
                else
                    tsmi.Text = $"{item.Name} ({item.Type.DisplayName})";


                Bitmap? bmp = Properties.Resources.ResourceManager.GetObject(item.Type.Name, CultureInfo.InvariantCulture) as Bitmap;

                if (bmp is not null)
                    tsmi.Image = bmp;

                tsmi.BackColor = Color.FromArgb(80, 80, 80);
                tsmi.ForeColor = Color.White;
                tsmi.ImageTransparentColor = Color.FromArgb(80, 80, 80);
                tsmi.ImageScaling = ToolStripItemImageScaling.SizeToFit;
                tsmi.Click += OnPrefabSelect_Click;
                cmsPrefabSelect.Items.Add(tsmi);
            }
            cmsPrefabSelect.BackColor = Color.FromArgb(80, 80, 80);
            cmsPrefabSelect.ForeColor = Color.White;
            cmsPrefabSelect.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());
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
                if (RootEditor.ReadOnly)
                    return;
                if (Node.Prefabs.Nodes!.Count == 0 || (Node.MaxSize >= 0 && Node.MaxSize <= ValidValue.Count))
                    return;



                if (Node.Prefabs.Nodes!.Count > 1)
                {
                    if (Node.SingleType)
                    {
                        if (singlePrefab is not null)
                        {
                            Expanded = true;
                            y -= 10;

                            EnsureValue();
                            JToken newValue2;
                            if (Node.MakeAsObject)
                                newValue2 = CreateObjectItem(singlePrefab).Value;
                            else
                                newValue2 = CreateArrayItem(((JArray)Value).Count, singlePrefab, null, true).Value;

                            y += 10;
                            Height = y;

                            OnValueChanged(new JtfEditorAction(JtfEditorAction.JtEditorActionType.AddToken, null, newValue2, this));
                            return;
                        }
                    }
                    InitContextMenu();
                    cmsPrefabSelect!.Show(MousePosition);
                    return;
                }
                Expanded = true;
                y -= 10;

                EnsureValue();
                JToken newValue;
                if (Node.MakeAsObject)
                    newValue = CreateObjectItem(Node.Prefabs.Nodes![0]).Value;
                else
                    newValue = CreateArrayItem(((JArray)Value).Count, Node.Prefabs.Nodes![0], null, true).Value;

                y += 10;
                Height = y;

                OnValueChanged(new JtfEditorAction(JtfEditorAction.JtEditorActionType.AddToken, null, newValue, this));
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.N && e.Control)
            {
                if (Node.Prefabs.Nodes!.Count == 0)
                    return;
                if (Node.Prefabs.Nodes!.Count > 1)
                {
                    cmsPrefabSelect!.Show(MousePosition);
                    return;
                }
                Expanded = true;
                y -= 10;

                EnsureValue();
                JToken newValue;
                if (Node.MakeAsObject)
                    newValue = CreateObjectItem(Node.Prefabs.Nodes![0]).Value;
                else
                    newValue = CreateArrayItem(((JArray)Value).Count, Node.Prefabs.Nodes![0], null, true).Value;

                y += 10;
                Height = y;
                OnValueChanged(new JtfEditorAction(JtfEditorAction.JtEditorActionType.AddToken, null, newValue, this));

            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (IsInvalidValueType || RootEditor.ReadOnly)
                return;
            if (addNewButtonBounds.Contains(e.Location))
            {
                if (Node.Prefabs.Nodes!.Count == 0 || (Node.MaxSize >= 0 && Node.MaxSize <= ValidValue.Count))
                    Cursor = Cursors.No;
                else
                    Cursor = Cursors.Hand;
                return;
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
    }

}