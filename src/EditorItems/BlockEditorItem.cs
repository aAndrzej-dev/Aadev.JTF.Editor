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
        public override event EventHandler? ValueChanged;
        private JToken _value = JValue.CreateNull();

        private new JtBlock Type => (JtBlock)base.Type;


        private JObject? RawValue
        {
            get => _value?.Type is JTokenType.Object ? ((JObject?)_value ?? new JObject()) : (_value?.Type is JTokenType.Null ? new JObject() : null);
            set => _value = value is not null ? value : _value;
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

        internal BlockEditorItem(JtToken type, JToken? token) : base(type, token) { }

        protected override void ChangeValue() => ValueChanged?.Invoke(this, EventArgs.Empty);

        protected override void OnExpandChanged()
        {
            if (!Expanded)
            {
                Height = 32;

                Controls.Clear();
                base.OnExpandChanged();
                return;
            }
            if (InvalidValueType)
                return;
            RawValue ??= new JObject();
            y = 38;

            List<string> Twins = new();

            foreach (JtToken item in Type.Children)
            {




                if (item.GetTwinFamily().Length > 1)
                {


                    if (Twins.Contains(item.Name!))
                    {
                        continue;
                    }

                    IEnumerable<JtToken>? t = item.GetTwinFamily().Where(x => x.JsonType == RawValue?[item.Name!]?.Type);

                    if (t is null || t?.Count() == 0)
                    {
                        y = CreateBei(item, y);
                        Twins.Add(item.Name!);

                        continue;
                    }
                    y = CreateBei(t!.First(), y);
                    Twins.Add(item.Name!);
                    continue;






                }

                y = CreateBei(item, y);


            }

            Height = y + 10;




            base.OnExpandChanged();
        }
        protected override void CreateValue() => Value = new JObject();

        private int BeiResize(EditorItem bei)
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
        private int CreateBei(JtToken type, int y, bool resizeOnCreate = false)
        {
            EditorItem? bei;
            if (resizeOnCreate)
            {
                RawValue![type.Name!] = null;
                bei = Create(type, null);
            }
            else
            {
                bei = Create(type, RawValue?[type.Name!]);
            }

            if (bei is null)
            {
                return y;
            }

            bei.Location = new System.Drawing.Point(10, y);
            bei.Width = Width - 20;
            Controls.Add(bei);
            if (resizeOnCreate)
            {
                y = BeiResize(bei);
                Height = y;
            }
            bei.HeightChanged += (sender, e) =>
            {
                y = BeiResize(bei);
                Height = y;

            };
            bei.ValueChanged += (sender, e) =>
            {


                if (bei.Value?.Type is JTokenType.Null || bei.Value is null)
                {

                    RawValue?.Remove(bei.Type.Name!);
                }

                else
                {
                    RawValue![bei.Type.Name!] = bei.Value;


                }

                ValueChanged?.Invoke(sender, e);
            };

            bei.TwinTypeChanged += (sender, e) =>
            {

                Controls.Remove(bei);

                CreateBei(e.NewTwinType!, bei.Top, true);


            };
            if (bei.Height != 0)
            {
                y += bei.Height + 5;
            }

            return y;
        }

    }
}