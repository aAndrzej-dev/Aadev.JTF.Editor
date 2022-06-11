using Aadev.JTF.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Aadev.JTF.Editor.EditorItems
{
    internal sealed class BlockEditorItem : EditorItem
    {
        private int y;
        private FocusableControl? focusControl;
        private JToken _value = JValue.CreateNull();

        private new JtBlock Node => (JtBlock)base.Node;


        private JObject RawValue
        {
            get
            {
                if (_value is not JObject)
                    _value = new JObject();
                return (JObject)_value;
            }

            set => _value = value is null ? _value : value;
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

        protected override bool IsFocused => base.IsFocused || focusControl?.Focused is true;

        internal override bool IsSaveable => Node.Required || (Value.Type != JTokenType.Null && RawValue.Count > 0);

        internal BlockEditorItem(JtNode type, JToken? token, EventManager eventManager, JsonJtfEditor jsonJtfEditor) : base(type, token, eventManager, jsonJtfEditor)
        {
            SetStyle(ControlStyles.ContainerControl, true);



        }





        protected override void OnExpandChanged()
        {
            SuspendLayout();
            if (!Expanded)
            {
                Height = 32;
                if (Node.IsDynamicName)
                    Controls.Remove(focusControl);
                focusControl = null;
                Controls.Clear();
                base.OnExpandChanged();
                ResumeLayout();
                return;
            }
            if (IsInvalidValueType)
            {
                ResumeLayout();
                return;
            }
            y = !CanCollapse ? 10 : 38;
            if (!Node.IsRoot)
            {

                focusControl = new FocusableControl
                {
                    Height = 0,
                    Width = 0,
                    Top = 0,
                    Left = 0
                };
                focusControl.GotFocus += (s, e) =>
                {
                    if (Node.IsDynamicName)
                    {
                        if (txtDynamicName is null)
                            CreateDynamicNameTextBox();
                        else
                            txtDynamicName.Focus();
                    }
                    else
                    {
                        Invalidate();

                    }

                };
                focusControl.LostFocus += (s, e) => Invalidate();
                focusControl.KeyDown += (s, e) =>
                {
                    if (IsInvalidValueType)
                        return;

                    if (Node.Type.IsContainerType && e.KeyCode == Keys.Space)
                    {
                        Expanded = !Expanded;
                    }
                };
                Controls.Add(focusControl);
                focusControl?.Focus();
            }
            else
            {
                Controls.Remove(focusControl);
                focusControl = null;
            }



            List<string> Twins = new();
            int index = 0;

            if (Node.IsDynamicName)
                index++;

            foreach (JtNode item in Node.Children)
            {

                JtNode[]? twinFamily = item.GetTwinFamily();


                if (twinFamily.Length > 1)
                {
                    if (Twins.Contains(item.Name!))
                    {
                        continue;
                    }

                    JtNode? t = twinFamily.FirstOrDefault(x => x.JsonType == RawValue[item.Name!]?.Type);

                    if (t is null)
                    {
                        EditorItem ei2;
                        (y, ei2) = CreateEditorItem(item, y);
                        ei2.TabIndex = index;
                        Twins.Add(item.Name!);
                        index++;

                        continue;
                    }
                    EditorItem ei3;
                    (y, ei3) = CreateEditorItem(t, y);
                    ei3.TabIndex = index;
                    Twins.Add(item.Name!);
                    index++;
                    continue;
                }
                EditorItem ei;
                (y, ei) = CreateEditorItem(item, y);
                ei.TabIndex = index;
                index++;

            }

            Height = y + 5;



            ResumeLayout();
            base.OnExpandChanged();
        }
        protected override JToken CreateValue() => Value = Node.CreateDefaultValue();
        private int UpdateLayout(EditorItem bei)
        {
            int oy = bei.Top + bei.Height + 5;
            if (bei.Height == 0)
            {
                oy = bei.Top;
            }

            foreach (Control control in Controls.Cast<Control>().Where(x => x.Top >= bei.Top && x != bei))
            {
                control.Top = oy;
                oy += control.Height;
                if (control.Height != 0)
                {
                    oy += 5;
                }
            }
            y = oy + 10;
            return y;
        }
        private (int, EditorItem) CreateEditorItem(JtNode type, int y, bool resizeOnCreate = false)
        {
            EditorItem bei;
            if (resizeOnCreate)
            {
                RawValue[type.Name!] = null;
                bei = Create(type, null, EventManager, RootEditor);
            }
            else
            {
                bei = Create(type, RawValue[type.Name!], EventManager, RootEditor);
            }


            bei.Location = new System.Drawing.Point(10, y);
            bei.Width = Width - 20;

            if (bei.IsSaveable)
            {
                RawValue[type.Name!] = bei.Value;
            }

            bei.CreateEventHandlers();
            Controls.Add(bei);
            if (resizeOnCreate)
            {
                y = UpdateLayout(bei);
                Height = y;
            }
            bei.HeightChanged += (sender, e) =>
            {
                if (sender is not EditorItem bei) return;
                y = UpdateLayout(bei);
                Height = y;

            };
            bei.ValueChanged += (sender, e) =>
            {
                if (sender is not EditorItem bei) return;
                if (bei.IsSaveable)
                {
                    RawValue[bei.Node.Name!] = bei.Value;
                }
                else
                {
                    RawValue.Remove(bei.Node.Name!);
                }

                OnValueChanged();
            };

            bei.TwinTypeChanged += (sender, e) =>
            {
                if (sender is not EditorItem bei) return;
                Controls.Remove(bei);


                (_, EditorItem newei) = CreateEditorItem(e.NewTwinNode!, bei.Top, true);

                newei.TabIndex = bei.TabIndex;

            };
            if (bei.Height != 0)
            {
                y += bei.Height + 5;
            }

            return (y, bei);
        }
        internal override void CreateEventHandlers()
        {
            base.CreateEventHandlers();
            foreach (EditorItem item in Controls.Cast<Control>().Where(x => x is EditorItem))
            {
                item.CreateEventHandlers();
            }
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (Expanded)
            {
                focusControl?.Focus();
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