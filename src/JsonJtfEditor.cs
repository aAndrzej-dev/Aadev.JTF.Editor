using Aadev.JTF.Editor.EditorItems;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Aadev.JTF.Editor
{
    public partial class JsonJtfEditor : UserControl
    {
        private JTemplate? template;
        private Func<JtIdentifier, IEnumerable<IJtSuggestion>>? getDynamicSource;
        private JToken? value;
        internal SolidBrush SelectedNodeTypeBackBrush { get; } = new SolidBrush(Color.RoyalBlue);
        internal SolidBrush ExpandButtonBackBrush { get; } = new SolidBrush(Color.Green);
        internal Pen ExpandButtonForePen { get; } = new Pen(Color.White);
        internal SolidBrush AddItemButtonBackBrush { get; } = new SolidBrush(Color.Green);
        internal Pen AddItemButtonForePen { get; } = new Pen(Color.White);
        internal SolidBrush RemoveItemButtonBackBrush { get; } = new SolidBrush(Color.Red);
        internal Pen RemoveItemButtonForePen { get; } = new Pen(Color.White);
        internal SolidBrush TextBoxBackBrush { get; } = new SolidBrush(Color.FromArgb(80, 80, 80));
        internal SolidBrush TextBoxForeBrush { get; } = new SolidBrush(Color.White);
        internal SolidBrush InvalidElementForeBrush { get; } = new SolidBrush(Color.Red);
        internal SolidBrush AcitveBorderBrush { get; } = new SolidBrush(Color.DarkCyan);
        internal SolidBrush InacitveBorderBrush { get; } = new SolidBrush(Color.FromArgb(200, 200, 200));
        internal SolidBrush InvalidBorderBrush { get; } = new SolidBrush(Color.Red);
        internal SolidBrush WarningBorderBrush { get; } = new SolidBrush(Color.Yellow);
        internal SolidBrush RequiredStarBrush { get; } = new SolidBrush(Color.Gold);
        internal SolidBrush TrueValueBackBrush { get; } = new SolidBrush(Color.Green);
        internal SolidBrush TrueValueForeBrush { get; } = new SolidBrush(Color.White);
        internal SolidBrush FalseValueBackBrush { get; } = new SolidBrush(Color.Red);
        internal SolidBrush FalseValueForeBrush { get; } = new SolidBrush(Color.White);
        internal SolidBrush WarinigValueBrush { get; } = new SolidBrush(Color.Yellow);
        internal SolidBrush InvalidValueBrush { get; } = new SolidBrush(Color.Red);
        internal SolidBrush DefaultElementForeBrush { get; } = new SolidBrush(Color.LightGray);
        internal SolidBrush DiscardInvalidValueButtonBackBrush { get; } = new SolidBrush(Color.Red);
        internal SolidBrush DiscardInvalidValueButtonForeBrush { get; } = new SolidBrush(Color.White);

        public event EventHandler? ValueChanged;


        public Func<JtIdentifier, IEnumerable<IJtSuggestion>>? GetDynamicSource { get => getDynamicSource; set { getDynamicSource = value; OnTemplateChanged(); } }


        public JTemplate? Template { get => template; set { template = value; OnTemplateChanged(); } }
        public JToken? Value { get => value; set { this.value = value; OnTemplateChanged(); } }

        public bool ReadOnly { get; set; }
        public bool ShowEmptyNodesInReadOnlyMode { get; set; }
        public bool ScrollWhenExpandingNodes { get; set; }

        public Color SelectedNodeTypeBackColor { get => SelectedNodeTypeBackBrush.Color; set => SelectedNodeTypeBackBrush.Color = value; }
        public Color ExpandButtonBackColor { get => ExpandButtonBackBrush.Color; set => ExpandButtonBackBrush.Color = value; }
        public Color ExpandButtonForeColor { get => ExpandButtonForePen.Color; set => ExpandButtonForePen.Color = value; }
        public Color AddItemButtonBackColor { get => AddItemButtonBackBrush.Color; set => AddItemButtonBackBrush.Color = value; }
        public Color AddItemButtonForeColor { get => AddItemButtonForePen.Color; set => AddItemButtonForePen.Color = value; }
        public Color RemoveItemButtonBackColor { get => RemoveItemButtonBackBrush.Color; set => RemoveItemButtonBackBrush.Color = value; }
        public Color RemoveItemButtonForeColor { get => RemoveItemButtonForePen.Color; set => RemoveItemButtonForePen.Color = value; }
        public Color TextBoxBackColor { get => TextBoxBackBrush.Color; set => TextBoxBackBrush.Color = value; }
        public Color TextBoxForeColor { get => TextBoxForeBrush.Color; set => TextBoxForeBrush.Color = value; }
        public Color InvalidElementForeColor { get => InvalidElementForeBrush.Color; set => InvalidElementForeBrush.Color = value; }
        public Color AcitveBorderColor { get => AcitveBorderBrush.Color; set => AcitveBorderBrush.Color = value; }
        public Color InacitveBorderColor { get => InacitveBorderBrush.Color; set => InacitveBorderBrush.Color = value; }
        public Color InvalidBorderColor { get => InvalidBorderBrush.Color; set => InvalidBorderBrush.Color = value; }
        public Color WarningBorderColor { get => WarningBorderBrush.Color; set => WarningBorderBrush.Color = value; }
        public Color RequiredStarColor { get => RequiredStarBrush.Color; set => RequiredStarBrush.Color = value; }
        public Color TrueValueBackColor { get => TrueValueBackBrush.Color; set => TrueValueBackBrush.Color = value; }
        public Color TrueValueForeColor { get => TrueValueForeBrush.Color; set => TrueValueForeBrush.Color = value; }
        public Color FalseValueBackColor { get => FalseValueBackBrush.Color; set => FalseValueBackBrush.Color = value; }
        public Color FalseValueForeColor { get => FalseValueForeBrush.Color; set => FalseValueForeBrush.Color = value; }
        public Color WarinigValueColor { get => WarinigValueBrush.Color; set => WarinigValueBrush.Color = value; }
        public Color InvalidValueColor { get => InvalidValueBrush.Color; set => InvalidValueBrush.Color = value; }
        public Color DefaultElementForeColor { get => DefaultElementForeBrush.Color; set => DefaultElementForeBrush.Color = value; }
        public Color DiscardInvalidValueButtonBackColor { get => DiscardInvalidValueButtonBackBrush.Color; set => DiscardInvalidValueButtonBackBrush.Color = value; }
        public Color DiscardInvalidValueButtonForeColor { get => DiscardInvalidValueButtonForeBrush.Color; set => DiscardInvalidValueButtonForeBrush.Color = value; }
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
                JtNode? t = template.Roots.FirstOrDefault(x => x.JsonType == value?.Type);
                if (t is not null)
                    CreateEditorItem(t);
                else
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
        public void Save(string filename, Newtonsoft.Json.Formatting formatting = Newtonsoft.Json.Formatting.None) => File.WriteAllText(filename!, value!.ToString(formatting));
       protected override Point ScrollToControl(Control activeControl) => ScrollWhenExpandingNodes ? base.ScrollToControl(activeControl) : AutoScrollPosition;
    }
}