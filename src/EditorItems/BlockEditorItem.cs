using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Aadev.JTF.Editor.ViewModels;
using Aadev.JTF.Nodes;
using Newtonsoft.Json.Linq;

namespace Aadev.JTF.Editor.EditorItems;

internal sealed class BlockEditorItem : ContainerEditorItem
{
    private Dictionary<JtNodeViewModel, EditorItem?>? childrenMap;
    private bool suspendUpdatingLayout;

    public new JtBlockViewModel ViewModel => (JtBlockViewModel)base.ViewModel;
    private new JtBlockNode Node => (JtBlockNode)base.Node;

    internal BlockEditorItem(JtBlockViewModel node, JsonJtfEditor rootEditor) : base(node, rootEditor) { }

    private void UpdateLayout()
    {
        if (suspendUpdatingLayout)
            return;
        SuspendLayout();
        int yOffset = (Node.IsRootChild && Node.Template.Roots.Count == 1) ? 10 : 38;
        foreach (Control ctr in Controls)
        {
            if (ctr is not IJsonItem)
                continue;
            if (ctr.Top != yOffset)
                ctr.Top = yOffset;
            if (ctr.Height != 0)
                yOffset += ctr.Height + 5;
        }

        if (!ViewModel.Expanded)
            Height = 32;
        else
            Height = yOffset + 5;

        ResumeLayout();
    }
    private (int, EditorItem) CreateEditorItem(JtNodeViewModel nvm, int y, bool twinTypeChanged = false, int insertIndex = -1)
    {
        EditorItem bei = Create(nvm, RootEditor);

        bei.Location = new Point(Indent, y);
        bei.Width = Width - 20;

        ViewModel.UpdateValueForChild(nvm);

        Controls.Add(bei);
        if (insertIndex >= 0)
        {
            Controls.SetChildIndex(bei, insertIndex);
        }

        bei.HeightChanged += bei => UpdateLayout();



        if (bei.Height != 0)
        {
            y += bei.Height + 5;
        }

        return (y, bei);
    }


    internal override void OnExpandChanged()
    {
        int y;
        SuspendLayout();
        if (!ViewModel.Expanded)
        {
            Height = 32;
            DestroyFocusPanel();
            Controls.Clear();
            ResumeLayout();
            return;
        }

        if (ViewModel.IsInvalidValueType)
        {
            ResumeLayout();
            return;
        }

        if (!Node.IsRootChild || Node.Template.Roots.Count != 1)
        {
            CreateFocusPanel();
            y = 38;
        }
        else
        {
            y = 10;
        }

        if (childrenMap is null)
        {
            Span<JtNodeViewModel> childrenVMs = ViewModel.GetChildren();
            childrenMap = new Dictionary<JtNodeViewModel, EditorItem?>(childrenVMs.Length);
            int tabIndex = 0;
            if (Node.IsDynamicName)
                tabIndex++;
            for (int i = 0; i < childrenVMs.Length; i++)
            {
                JtNodeViewModel item = childrenVMs[i];
                item.ConditionMetChanged += ChildsConditionChanged;

                if (!item.IsConditionMet || !item.IsSelectedTwin)
                {
                    childrenMap.Add(item, null);
                    tabIndex++;
                    continue;
                }

                (y, EditorItem ei) = CreateEditorItem(item, y);
                ei.TabIndex = tabIndex;
                childrenMap.Add(item, ei);
                tabIndex++;
            }
        }
        else
        {
            foreach (KeyValuePair<JtNodeViewModel, EditorItem?> item in childrenMap)
            {
                if (!item.Key.IsSelectedTwin || !item.Key.IsConditionMet || item.Value is null)
                    continue;
                Controls.Add(item.Value);
                item.Value.Location = new Point(Indent, y);
                y += item.Value.Height + 5;
            }
        }

        Span<JtTwinFamilyViewModel> twinFamilies = CollectionsMarshal.AsSpan(ViewModel.GetContainingTwinFamilies());

        for (int i = 0; i < twinFamilies.Length; i++)
        {
            twinFamilies[i].SelectionChanged += ChildsTwinFamilySelectionChanged;
        }


        if (ViewModel.Value is JObject obj)
        {
            foreach (KeyValuePair<string, JToken?> item in obj)
            {
                if (ViewModel.GetChildren().Any(x => x.Node.Name == item.Key))
                    continue;

                InvalidJsonItem invalidJsonItem = new InvalidJsonItem(item.Value!, RootEditor)
                {
                    Location = new Point(Indent, y),
                    Width = Width - (2 * Indent)
                };
                Controls.Add(invalidJsonItem);
                y += 32 + 5;
            }
        }

        Height = y + 5;
        ResumeLayout();
        return;
    }

    private void ChildsTwinFamilySelectionChanged(JtTwinFamilyViewModel family, JtTwinFamilySelectedNodeChangedEventArgs e)
    {
        if (ViewModel.Root.IsReadOnly)
            return;
        RootEditor.SuspendSrollingToControl = true;
        SuspendLayout();
        suspendUpdatingLayout = true; // Suspend not to call UpdateLayout when old editor item is removed
        JToken? oldValue = null;
        int oldHeight = 0;
        int index = Controls.Count - 1;
        int top = Height - 5;



        if (e.OldNode is not null && childrenMap!.TryGetValue(e.OldNode, out EditorItem? oldEi) is true && oldEi is not null)
        {
            oldHeight = oldEi.Height;
            top = oldEi.Top;
            index = Controls.IndexOf(oldEi);
            if (index == -1)
                index = Controls.Count - 1;
            Controls.Remove(oldEi);
            oldValue = e.OldNode.Value;
        }

        JtNodeViewModel? newNode = e.NewNode;

        suspendUpdatingLayout = false;
        if (newNode is not null)
        {
            newNode.Value = newNode.Node.CreateDefaultValue();
            if (childrenMap!.TryGetValue(newNode, out EditorItem? newEiFromMap) is true && newEiFromMap is not null)
            {
                Controls.Add(newEiFromMap);
                Controls.SetChildIndex(newEiFromMap, index);
                newEiFromMap.Focus();
                newEiFromMap.TabIndex = index;
                UpdateLayout();
            }
            else
            {
                (_, EditorItem newei) = CreateEditorItem(newNode, top, true, index);

                childrenMap[newNode] = newei;

                newei.TabIndex = index;
                newei.Focus();
                ViewModel.OnValueChanged(new JtfEditorAction(JtfEditorAction.JtEditorActionType.ChangeTwinType, oldValue, newNode.Value, ViewModel));

                if (oldHeight != newei.Height)
                    UpdateLayout();
            }
        }
        else
        {
            UpdateLayout();
        }
        RootEditor.SuspendSrollingToControl = false;
        ResumeLayout();
    }

    private void ChildsConditionChanged(JtNodeViewModel vm)
    {
        if (vm.IsConditionMet && vm.IsSelectedTwin)
        {
            int index = Array.IndexOf(ViewModel.GetChildren(), vm) + 1; // Focusable Control
            if (childrenMap?.TryGetValue(vm, out EditorItem? ei) is true && ei is not null)
            {
                Controls.Add(ei);
                Controls.SetChildIndex(ei, index);
                UpdateLayout();
                return;
            }

            (_, EditorItem nei) = CreateEditorItem(vm, 0, false, index);

            childrenMap![vm] = nei;

            UpdateLayout();
        }
        else
        {
            if (childrenMap?.TryGetValue(vm, out EditorItem? ei) is true && ei is not null)
            {
                Controls.Remove(ei);
            }
        }
    }
    protected override void OnControlRemoved(ControlEventArgs e)
    {
        base.OnControlRemoved(e);
        if (!ViewModel.Expanded || e.Control is not IJsonItem jsonItem)
            return;
        ViewModel.OnValueChanged(new JtfEditorAction(JtfEditorAction.JtEditorActionType.RemoveToken, jsonItem.Value, null, ViewModel));
        UpdateLayout();
    }
}