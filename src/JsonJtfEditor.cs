﻿using Aadev.JTF.Editor.EditorItems;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Aadev.JTF.Editor
{
    public partial class JsonJtfEditor : UserControl, IHaveEventManager
    {
        private JTemplate? template;
        private int y;
        private JObject? Root;
        private string? filename;
        private EventManager? eventManager;


        public event EventHandler? ValueChanged;



        public JTemplate? Template { get => template; set { template = value; OnTemplateChanged(); } }
        public string? Filename { get => filename; set { filename = value; OnTemplateChanged(); } }

        EventManager? IHaveEventManager.EventManager => eventManager;

        public JsonJtfEditor()
        {
            InitializeComponent();
        }



        private void OnTemplateChanged()
        {
            if (Template is null || string.IsNullOrEmpty(Filename))
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

            foreach (JtToken item in template!.Children)
            {


                if (item.GetTwinFamily().Length > 1)
                {
                    if (Twins.Contains(item.Name!))
                    {
                        continue;
                    }

                    if (Root[item.Name!] != null && Root[item.Name!]!.Type != item.JsonType)
                    {
                        continue;
                    }

                    Twins.Add(item.Name!);
                }

                y = CreateBei(item, y);


            }


        }

        public void Save() => File.WriteAllText(Filename!, Root!.ToString(Newtonsoft.Json.Formatting.None));

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
                Root![type.Name!] = null;
                bei = EditorItem.Create(type, null);
            }
            else
            {
                bei = EditorItem.Create(type, Root![type.Name!]);
            }

            if (bei == null)
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