using Aadev.JTF.Types;
using Newtonsoft.Json.Linq;
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

            set => _value = value is not null ? value : _value;
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

        internal BlockEditorItem(JtToken type, JToken? token) : base(type, token) { }


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
                        y = CreateBei(item, y);
                        Twins.Add(item.Name!);

                        continue;
                    }
                    y = CreateBei(t, y);
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
            EditorItem bei;
            if (resizeOnCreate)
            {
                RawValue[type.Name!] = null;
                bei = Create(type, null);
            }
            else
            {
                bei = Create(type, RawValue[type.Name!]);
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


                if (bei.Value.Type is JTokenType.Null)
                {

                    RawValue.Remove(bei.Type.Name!);
                }

                else
                {
                    RawValue[bei.Type.Name!] = bei.Value;


                }

                OnValueChanged();
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