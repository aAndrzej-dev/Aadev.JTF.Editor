using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Aadev.JTF.Editor.ViewModels;

public class JtTwinFamilyViewModel : IList<JtNodeViewModel>
{
    private JtNodeViewModel? selectedNode;
    public string? Name { get; }

    public JtNodeViewModel? SelectedNode
    {
        get => selectedNode;
        private set
        {
            if (selectedNode == value)
                return;
            JtNodeViewModel? oldNode = selectedNode;
            selectedNode = value;
            if (oldNode is JtContainerViewModel cvm)
                cvm.Expanded = false;
            SelectionChanged?.Invoke(this, new JtTwinFamilySelectedNodeChangedEventArgs(oldNode, selectedNode));
        }
    }

    internal event JtTwinFamilyViewModelEventHandler<JtTwinFamilySelectedNodeChangedEventArgs>? SelectionChanged;

    internal List<JtNodeViewModel> members;
    internal JtTwinFamilyViewModel(int count)
    {
        members = new List<JtNodeViewModel>(count);
    }
    public JtTwinFamilyViewModel(JtNodeViewModel member)
    {
        members = new List<JtNodeViewModel>(1)
        {
            member
        };
        member.SetTwinFamily(this);
        Name = member.Node.Name;
    }
    public JtTwinFamilyViewModel(List<JtNodeViewModel> members)
    {
        this.members = members;
        if (members.Count > 0)
        {
            Name = members[0].Node.Name;
        }
    }

    internal void UpdateSelectedNode()
    {
        Span<JtNodeViewModel> membersSpan = CollectionsMarshal.AsSpan(members);

        if (membersSpan.Length == 0)
            return;

        if (membersSpan.Length == 1)
        {
            SelectedNode = membersSpan[0];
            return;
        }

        JtNodeViewModel? firstWithConditionMet = null;
        for (int i = 0; i < membersSpan.Length; i++)
        {
            JtNodeViewModel member = membersSpan[i];
            if (!member.IsConditionMet)
                continue;

            firstWithConditionMet ??= member;
            if (member.Node.JsonType == member.Value?.Type)
            {
                SelectedNode = member;
                return;
            }
        }

        SelectedNode = firstWithConditionMet;
    }
    public JtNodeViewModel this[int index] { get => ((IList<JtNodeViewModel>)members)[index]; set => ((IList<JtNodeViewModel>)members)[index] = value; }

    public int Count => ((ICollection<JtNodeViewModel>)members).Count;

    public bool IsReadOnly => ((ICollection<JtNodeViewModel>)members).IsReadOnly;

    public void Add(JtNodeViewModel item) => ((ICollection<JtNodeViewModel>)members).Add(item);
    public void Clear() => ((ICollection<JtNodeViewModel>)members).Clear();
    public bool Contains(JtNodeViewModel item) => ((ICollection<JtNodeViewModel>)members).Contains(item);
    public void CopyTo(JtNodeViewModel[] array, int arrayIndex) => ((ICollection<JtNodeViewModel>)members).CopyTo(array, arrayIndex);
    public IEnumerator<JtNodeViewModel> GetEnumerator() => ((IEnumerable<JtNodeViewModel>)members).GetEnumerator();
    public int IndexOf(JtNodeViewModel item) => ((IList<JtNodeViewModel>)members).IndexOf(item);
    public void Insert(int index, JtNodeViewModel item) => ((IList<JtNodeViewModel>)members).Insert(index, item);
    public bool Remove(JtNodeViewModel item) => ((ICollection<JtNodeViewModel>)members).Remove(item);
    public void RemoveAt(int index) => ((IList<JtNodeViewModel>)members).RemoveAt(index);
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)members).GetEnumerator();
    internal void RequestChange(JtNodeViewModel newType) => SelectedNode = newType;
}