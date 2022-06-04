using Aadev.JTF.Editor.EditorItems;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Windows.Forms;

namespace Aadev.JTF.Editor
{
    public partial class JsonJtfEditor : UserControl
    {
        private JTemplate? template;
        private JToken? Root;
        private string? filename;
        private EventManager? eventManager;
        private bool showConditionsCount;
        internal static readonly ToolTip toolTip = new ToolTip() { BackColor = System.Drawing.Color.FromArgb(80, 80, 80), ForeColor = System.Drawing.Color.White, ShowAlways = true, Active = false };

        public event EventHandler? ValueChanged;



        public JTemplate? Template { get => template; set { template = value; OnTemplateChanged(); } }
        public string? Filename { get => filename; set { filename = value; OnTemplateChanged(); } }

        public bool ShowConditionsCount { get => showConditionsCount; set { showConditionsCount = value; Invalidate(); } }


        public JsonJtfEditor()
        {
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
                Root = JToken.Parse(File.ReadAllText(Filename));
            }
            catch
            {
                Root = template.Root.CreateDefaultValue();
            }

            eventManager = new EventManager();

            EditorItem bei = EditorItem.Create(template.Root, Root, eventManager, this);

            bei.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left;
            bei.Location = new System.Drawing.Point(10, 10);
            bei.Width = Width - 20;
            bei.CreateEventHandlers();
            Controls.Add(bei);
            bei.ValueChanged += (sender, e) =>
            {
                if (sender is not EditorItem bei) return;

                Root = bei.Value;

                ValueChanged?.Invoke(sender, e);
            };
            if (bei is BlockEditorItem)
            {
                bei.Width = Width;
                bei.Top = 0;
                bei.Left = 0;
            }

        }
        public void Save(Newtonsoft.Json.Formatting formatting = Newtonsoft.Json.Formatting.None) => File.WriteAllText(Filename!, Root!.ToString(formatting));
    }
}