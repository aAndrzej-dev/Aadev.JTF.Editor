using Aadev.JTF.Editor.EditorItems;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Aadev.JTF.Editor
{
    public partial class JsonJtfEditor : UserControl, IEventManagerProvider
    {
        private JTemplate? template;
        private EditorItem? rootEditorItem;
        private Func<string, object?>? getDynamicSource;
        private JToken? value;

        public event EventHandler? ValueChanged;


        public Func<string, object?>? GetDynamicSource { get => getDynamicSource; set { getDynamicSource = value; OnTemplateChanged(); } }

        private readonly Dictionary<IIdentifiersManager, EventManager> identifiersEventManagersMap = new Dictionary<IIdentifiersManager, EventManager>();

        public JTemplate? Template { get => template; set { template = value; OnTemplateChanged(); } }
        public JToken? Value { get => value; set { this.value = value; OnTemplateChanged(); } }




        public bool NormalizeTwinNodeOrder { get; set; }


        internal ToolTip ToolTip { get; }

        public JsonJtfEditor()
        {
            InitializeComponent();
            ToolTip = new ToolTip()
            {
                BackColor = System.Drawing.Color.FromArgb(80, 80, 80),
                ForeColor = System.Drawing.Color.White,
                ShowAlways = true,
                Active = false

            };
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
            if (template is null || Value is null)
            {
                return;
            }

            rootEditorItem = EditorItem.Create(template.Root, value, this, this);

            rootEditorItem.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Left;
            rootEditorItem.Location = new System.Drawing.Point(10, 10);
            rootEditorItem.Width = Width - 20;
            Controls.Add(rootEditorItem);
            rootEditorItem.ValueChanged += (sender, e) =>
            {
                if (sender is not EditorItem bei)
                    return;

                value = bei.Value;

                ValueChanged?.Invoke(sender, e);
            };
            value = rootEditorItem.Value;
            if (rootEditorItem is BlockEditorItem && !rootEditorItem.IsInvalidValueType)
            {
                rootEditorItem.Width = Width;
                rootEditorItem.Top = 0;
                rootEditorItem.Left = 0;
            }
        }
        public void Save(string filename, Newtonsoft.Json.Formatting formatting = Newtonsoft.Json.Formatting.None) => File.WriteAllText(filename!, value!.ToString(formatting));
        EventManager IEventManagerProvider.GetEventManager(IIdentifiersManager identifiersManager) => GetEventManager(identifiersManager);
    }
}