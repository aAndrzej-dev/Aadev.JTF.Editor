using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;

namespace Aadev.JTF.Editor.ViewModels;
public sealed class JtRootViewModel : IJtNodeParentViewModel
{
    private JtNodeViewModel[]? children;

    internal JtTwinFamilyViewModel? twinFamily;
    private IntelligentSuggestionsProvider? intelligentSuggestionsProvider;
    private JTemplate? template;
    private JToken? value;

    public event EventHandler? ValueChanged;
    public event EventHandler? TemplateChanged;

    public JTemplate? Template { get => template; set { template = value; OnTemplateChanged(); } }
    public JToken? Value { get => value; set { this.value = value; OnTemplateChanged(); } }

    public bool ShowEmptyNodesInReadOnlyMode { get; set; }
    public ISuggestionSelector? SuggestionSelector { get; set; }
    public int MaximumSuggestionCountForComboBox { get; set; } = -1;
    public bool IsReadOnly { get; }
    public bool NormalizeTwinNodeOrder { get; set; }
    public bool ShowAdvancedToolTip { get; set; }

    JtRootViewModel IJtNodeParentViewModel.Root => this;

    public IntelligentSuggestionsProvider IntelligentSuggestionsProvider { get => intelligentSuggestionsProvider ??= new IntelligentSuggestionsProvider(); set => intelligentSuggestionsProvider = value; }
    public Func<JtIdentifier, IEnumerable<IJtSuggestion>>? DynamicSuggestionsSource { get; set; }

    public JtRootViewModel(bool readOnly = false)
    {
        IsReadOnly = readOnly;
    }
    private void OnTemplateChanged()
    {
        TemplateChanged?.Invoke(this, EventArgs.Empty);

        if (Template is null || Value is null)
            return;

        children = new JtNodeViewModel[Template.Roots.Nodes!.Count];
        if (Template.Roots.Nodes!.Count > 1)
        {
            twinFamily = new JtTwinFamilyViewModel(Template.Roots.Nodes!.Count);
            for (int i = 0; i < Template.Roots.Count; i++)
            {
                JtNodeViewModel child = JtNodeViewModel.Create(Template.Roots.Nodes[i], Value, new EventManagerContext(null), this);
                children[i] = child;
                twinFamily.Add(child);
                child.SetTwinFamily(twinFamily);

                child.ValueChanged += (s, e) =>
                {
                    if (e.ReplaceValue)
                        value = s.Value;
                    ValueChanged?.Invoke(this, e);
                };
            }

            twinFamily.UpdateSelectedNode();
        }
        else
        {
            JtNodeViewModel child = JtNodeViewModel.Create(Template.Roots.Nodes[0], Value, new EventManagerContext(null), this);
            children[0] = child;
            child.ValueChanged += (s, e) =>
            {
                if (e.ReplaceValue)
                    value = s.Value;
                ValueChanged?.Invoke(this, e);
            };
        }
    }
    public void ChangeValue(JToken newValue)
    {
        value = newValue;
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }
    [MemberNotNull(nameof(SuggestionSelector))]
    public void EnsureSuggestionSelector() => SuggestionSelector ??= new SuggestionSelectForm();
    public JtNodeViewModel[] GetChildren() => children ?? Array.Empty<JtNodeViewModel>();
    internal void OnValueChanged(JtfEditorAction jtfEditorAction) => ValueChanged?.Invoke(this, new ValueChangedEventArgs(jtfEditorAction, true));
}
