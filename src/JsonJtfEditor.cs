using Aadev.JTF.Editor.EditorItems;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Aadev.JTF.Editor
{
    public partial class JsonJtfEditor : UserControl
    {
        private JTemplate? template;
        private JToken? root;
        private string? filename;
        private Func<string, object?>? getDynamicSource;
        internal static readonly ToolTip toolTip = new ToolTip() { BackColor = System.Drawing.Color.FromArgb(80, 80, 80), ForeColor = System.Drawing.Color.White, ShowAlways = true, Active = false };

        public event EventHandler? ValueChanged;


        public Func<string, object?>? GetDynamicSource { get => getDynamicSource; set { getDynamicSource = value; OnTemplateChanged(); } }

        private readonly Dictionary<IIdentifiersManager, EventManager> identifiersEventManagersMap = new Dictionary<IIdentifiersManager, EventManager>();

        public JTemplate? Template { get => template; set { template = value; OnTemplateChanged(); } }
        public string? Filename { get => filename; set { filename = value; OnTemplateChanged(); } }




        public JsonJtfEditor()
        {
            InitializeComponent();
        }

        internal EventManager GetEventManager(IIdentifiersManager identifiersManager)
        {
            if (identifiersEventManagersMap.ContainsKey(identifiersManager))
                return identifiersEventManagersMap[identifiersManager];
            EventManager? em = new EventManager(identifiersManager);

            identifiersEventManagersMap.Add(identifiersManager, em);

            return em;
        }


        private void OnTemplateChanged()
        {
            if (template is null || string.IsNullOrEmpty(Filename))
            {
                return;
            }
            try
            {
                root = JToken.Parse(File.ReadAllText(Filename));
            }
            catch
            {
                root = template.Root.CreateDefaultValue();
            }

            EditorItem bei = EditorItem.Create(template.Root, root, this);

            bei.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left;
            bei.Location = new System.Drawing.Point(10, 10);
            bei.Width = Width - 20;
            Controls.Add(bei);
            bei.ValueChanged += (sender, e) =>
            {
                if (sender is not EditorItem bei)
                    return;

                root = bei.Value;

                ValueChanged?.Invoke(sender, e);
            };
            if (bei is BlockEditorItem)
            {
                bei.Width = Width;
                bei.Top = 0;
                bei.Left = 0;
            }
        }
        public void Save(Newtonsoft.Json.Formatting formatting = Newtonsoft.Json.Formatting.None) => File.WriteAllText(Filename!, root!.ToString(formatting));
    }
}