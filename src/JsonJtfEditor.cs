using Aadev.JTF.Editor.EditorItems;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Aadev.JTF.Editor
{
    public partial class JsonJtfEditor : UserControl
    {
        private JTemplate? template;
        private int y;
        private JObject? Root;
        private string? filename;
        private EventManager eventManager;


        internal static readonly ToolTip toolTip = new ToolTip() { BackColor = System.Drawing.Color.FromArgb(80, 80, 80), ForeColor = System.Drawing.Color.White, ShowAlways = true, Active = false };

        public event EventHandler? ValueChanged;



        public JTemplate? Template { get => template; set { template = value; OnTemplateChanged(); } }
        public string? Filename { get => filename; set { filename = value; OnTemplateChanged(); } }


        public JsonJtfEditor()
        {
            eventManager = new EventManager();
            InitializeComponent();
        }



        private void OnTemplateChanged()
        {
            if (template is null || string.IsNullOrEmpty(Filename))
            {
                return;
            }
            try
            {
                Root = JObject.Parse(File.ReadAllText(Filename));
            }
            catch
            {
                Root = new JObject();
            }

            eventManager = new EventManager();

            y = 10;

            List<string> Twins = new();

            foreach (JtToken item in template.Children)
            {


                if (item.GetTwinFamily().Length > 1)
                {
                    if (Twins.Contains(item.Name!))
                    {
                        continue;
                    }

                    if (Root[item.Name!] is not null && Root[item.Name!]!.Type != item.JsonType)
                    {
                        continue;
                    }

                    Twins.Add(item.Name!);
                }

                y = CreateEditorItem(item, y);


            }


        }

        public void Save() => File.WriteAllText(Filename!, Root!.ToString(Newtonsoft.Json.Formatting.None));

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

        private int CreateEditorItem(JtToken type, int y, bool resizeOnCreate = false)
        {
            EditorItem bei;
            if (resizeOnCreate)
            {
                Root![type.Name!] = null;
                bei = EditorItem.Create(type, null, eventManager);
            }
            else
            {
                bei = EditorItem.Create(type, Root![type.Name!], eventManager);
            }


            bei.Location = new System.Drawing.Point(10, y);
            bei.Width = Width - 20;
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


                if (bei.Value.Type is JTokenType.Null)
                {

                    Root?.Remove(bei.Type.Name!);
                }

                else
                {
                    Root![bei.Type.Name!] = bei.Value;
                }

                ValueChanged?.Invoke(sender, e);
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

            return y;
        }
    }
}