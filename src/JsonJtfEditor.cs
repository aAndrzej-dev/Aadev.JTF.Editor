using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Aadev.JTF.Editor.EditorItems;
using Aadev.JTF.Editor.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Aadev.JTF.Editor;

public partial class JsonJtfEditor : ContainerControl
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public JtColorTable ColorTable { get; set; } = JtColorTable.Default;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool SuspendSrollingToControl { get; set; }



    internal ToolTip ToolTip { get; }

    internal ISuggestionSelector SuggestionSelector => ViewModel.SuggestionSelector ??= new SuggestionSelectForm();
    public JtRootViewModel ViewModel { get; }

    public JsonJtfEditor(JtRootViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        AutoScroll = true;
        ToolTip = new ToolTip()
        {
            BackColor = Color.FromArgb(80, 80, 80),
            ForeColor = Color.White,
            ShowAlways = true,
            Active = false

        };
        viewModel.TemplateChanged += (s, e) => OnTemplateChanged();
        OnTemplateChanged();


    }


    private void OnTemplateChanged()
    {
        if (ViewModel.Template is null || ViewModel.Value is null)
        {
            return;
        }

        Controls.Clear();

        if (ViewModel.GetChildren().Length == 0)
            return;

        if (ViewModel.twinFamily is null || ViewModel.twinFamily.SelectedNode is null)
        {

            CreateEditorItem(ViewModel.GetChildren()[0]);
        }
        else
        {
            CreateEditorItem(ViewModel.twinFamily.SelectedNode);
        }

        if (ViewModel.twinFamily is null || ViewModel.IsReadOnly)
            return;
        ViewModel.twinFamily.SelectionChanged += (family, e) =>
        {
            SuspendLayout();
            JToken? oldValue = e.OldNode?.Value;

            Controls.Clear();

            JtNodeViewModel? newNode = e.NewNode;

            if (newNode is not null)
            {
                newNode.Value = newNode.Node.CreateDefaultValue();
                EditorItem newei = CreateEditorItem(newNode);
                newei.TabIndex = 0;
                newei.Focus();
                ViewModel.OnValueChanged(new JtfEditorAction(JtfEditorAction.JtEditorActionType.ChangeTwinType, oldValue, newNode.Value, null));
            }

            ResumeLayout();
        };
    }
    private EditorItem CreateEditorItem(JtNodeViewModel root)
    {
        if (ViewModel.Template is null)
            throw new System.Exception();


        EditorItem rootEditorItem = EditorItem.Create(root, this);

        rootEditorItem.Location = new System.Drawing.Point(10, 10);
        rootEditorItem.Width = Width - 20;
        Controls.Add(rootEditorItem);
        root.ValueChanged += (sender, e) =>
        {
            Debug.WriteLine(e.Action?.ToString());
        };
        ViewModel.ChangeValue(root.Value);
        if (rootEditorItem is BlockEditorItem && !rootEditorItem.ViewModel.IsInvalidValueType && ViewModel.Template.Roots.Count == 1)
        {
            rootEditorItem.Width = Width;
            rootEditorItem.Top = 0;
            rootEditorItem.Left = 0;
        }

        return rootEditorItem;
    }
    public void Save(string filename, Formatting formatting = Formatting.None)
    {
        using StreamWriter sr = new StreamWriter(filename);
        using JsonWriter jw = new JsonTextWriter(sr);

        jw.Formatting = formatting;

        ViewModel.Value!.WriteTo(jw);

        jw.Close();
    }

    protected override Point ScrollToControl(Control activeControl) => SuspendSrollingToControl ? AutoScrollPosition : base.ScrollToControl(activeControl);
}