using Aadev.JTF.Editor.EditorItems;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Aadev.JTF.Editor
{
    public partial class JsonJtfEditor : UserControl
    {
        private JTemplate? template;
        private Func<JtIdentifier, IEnumerable<IJtSuggestion>>? dynamicSuggestionsSource;
        private JToken? value;


        public event EventHandler? ValueChanged;
        public JtColorTable ColorTable { get; set; } = JtColorTable.Default;

        public Func<JtIdentifier, IEnumerable<IJtSuggestion>>? DynamicSuggestionsSource { get => dynamicSuggestionsSource; set { dynamicSuggestionsSource = value; OnTemplateChanged(); } }



        public JTemplate? Template { get => template; set { template = value; OnTemplateChanged(); } }
        public JToken? Value { get => value; set { this.value = value; OnTemplateChanged(); } }

        public bool ReadOnly { get; set; }
        public bool ShowEmptyNodesInReadOnlyMode { get; set; }
        internal bool DisableScrollingToControl { get; set; }
        public bool ShowAdvancedToolTip { get; set; }

        public bool NormalizeTwinNodeOrder { get; set; }

        public ISuggestionSelector SuggestionSelector { get; set; }
        public int MaximumSuggestionCountForComboBox { get; set; } = -1;
        internal ToolTip ToolTip { get; }

        public JsonJtfEditor()
        {
            InitializeComponent();

            AutoScroll = true;
            SuggestionSelector = new SuggestionSelectForm();
            ToolTip = new ToolTip()
            {
                BackColor = System.Drawing.Color.FromArgb(80, 80, 80),
                ForeColor = System.Drawing.Color.White,
                ShowAlways = true,
                Active = false

            };
        }
        private void OnTemplateChanged()
        {
            if (template is null || Value is null)
            {
                return;
            }
            Controls.Clear();
            if (template.Roots.Count == 1)
            {
                CreateEditorItem(template.Roots[0]);
            }
            else if (template.Roots.Count > 1)
            {
                for (int i = 0; i < template.Roots.Count; i++)
                {
                    JtNode root = template.Roots[i];
                    if (root.JsonType == Value.Type)
                    {
                        CreateEditorItem(root);
                        return;
                    }    
                }
                CreateEditorItem(template.Roots[0]);
            }

        }
        private EditorItem? CreateEditorItem(JtNode root)
        {
            if (template is null)
                return null;
            EditorItem rootEditorItem = EditorItem.Create(root, value, this, new EventManager(root.IdentifiersManager, null));

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
            rootEditorItem.TwinTypeChanged += (sender, e) =>
            {
                if (sender is not EditorItem bei || ReadOnly)
                    return;
                SuspendLayout();
                JToken oldValue = bei.Value;
                Controls.Remove(bei);
                value = e.NewTwinNode.CreateDefaultValue();
                EditorItem newei = CreateEditorItem(e.NewTwinNode)!;
                newei.TabIndex = 0;
                ValueChanged?.Invoke(this, EventArgs.Empty);

                ResumeLayout();
            };
            value = rootEditorItem.Value;
            if (rootEditorItem is BlockEditorItem && !rootEditorItem.IsInvalidValueType && template.Roots.Count == 1)
            {
                rootEditorItem.Width = Width;
                rootEditorItem.Top = 0;
                rootEditorItem.Left = 0;
            }
            return rootEditorItem;
        }
        public void Save(string filename, Newtonsoft.Json.Formatting formatting = Newtonsoft.Json.Formatting.None)
        {
            using StreamWriter sr = new StreamWriter(filename);
            using JsonWriter jw = new JsonTextWriter(sr);

            jw.Formatting = formatting;

            value!.WriteTo(jw);

            jw.Close();
        }

        protected override Point ScrollToControl(Control activeControl) => DisableScrollingToControl ? AutoScrollPosition : base.ScrollToControl(activeControl);
    }
}