using Aadev.JTF.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Aadev.JTF.Editor.EditorItems
{
    internal sealed class BlockEditorItem : EditorItem
    {
        private int y;
        private JToken _value = JValue.CreateNull();

        private new JtBlock Type => (JtBlock)base.Type;


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

        internal override bool IsSaveable => Type.Required || (Value.Type != JTokenType.Null && RawValue.Count > 0);

        internal BlockEditorItem(JtToken type, JToken? token, EventManager eventManager) : base(type, token, eventManager)
        {
            SetStyle(ControlStyles.ContainerControl, true);
        }

        protected override void OnExpandChanged()
        {
            if (!Expanded)
            {
                Height = 32;

                Controls.Clear();
                base.OnExpandChanged();
                return;
            }
            if (IsInvalidValueType)
                return;
            y = 38;

            List<string> Twins = new();

            foreach (JtToken item in Type.Children)
            {

                JtToken[]? twinFamily = item.GetTwinFamily();


                if (twinFamily.Length > 1)
                {
                    if (Twins.Contains(item.Name!))
                    {
                        continue;
                    }

                    JtToken? t = twinFamily.FirstOrDefault(x => x.JsonType == RawValue[item.Name!]?.Type);

                    if (t is null)
                    {
                        (y, _) = CreateEditorItem(item, y);
                        Twins.Add(item.Name!);

                        continue;
                    }
                    (y, _) = CreateEditorItem(t, y);
                    Twins.Add(item.Name!);
                    continue;
                }

                (y, _) = CreateEditorItem(item, y);


            }

            Height = y + 5;




            base.OnExpandChanged();
        }
        protected override JToken CreateValue() => Value = new JObject();

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
        private (int, EditorItem) CreateEditorItem(JtToken type, int y, bool resizeOnCreate = false)
        {
            EditorItem bei;
            if (resizeOnCreate)
            {
                RawValue[type.Name!] = null;
                bei = Create(type, null, EventManager);
            }
            else
            {
                bei = Create(type, RawValue[type.Name!], EventManager);
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
                y = UpdateLayout(bei);
                Height = y;

            };
            bei.ValueChanged += (sender, e) =>
            {
                if (bei.IsSaveable)
                {
                    RawValue[bei.Type.Name!] = bei.Value;
                }
                else
                {
                    RawValue.Remove(bei.Type.Name!);
                }

                OnValueChanged();
            };

            bei.TwinTypeChanged += (sender, e) =>
            {
                Controls.Remove(bei);


                CreateEditorItem(e.NewTwinType!, bei.Top, true);


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
            foreach (EditorItem item in Controls)
            {
                item.CreateEventHandlers();
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