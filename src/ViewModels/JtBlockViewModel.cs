using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Aadev.JTF.Nodes;
using Newtonsoft.Json.Linq;

namespace Aadev.JTF.Editor.ViewModels;
public sealed class JtBlockViewModel : JtContainerViewModel
{
    private JtNodeViewModel[]? children;
    private List<JtTwinFamilyViewModel>? containingTwinFamilies;
    internal JtBlockViewModel(JtBlockNode node, JToken? value, EventManagerContext eventManagerContext, IJtNodeParentViewModel parent) : base(node, value, eventManagerContext, parent) { }

    public JtNodeViewModel[] GetChildren()
    {
        if (children is null)
            InitializeChildren();
        return children;
    }
    [MemberNotNull(nameof(children))]
    private void InitializeChildren()
    {
        if (children is not null)
            throw new Exception("Children has been already initialized");

        children = new JtNodeViewModel[Node.Children.Nodes.Count];
        containingTwinFamilies = new List<JtTwinFamilyViewModel>();

        if (Node.ContainerJsonType == JtContainerType.Block)
        {
            Span<JtNode> nodesSpan = CollectionsMarshal.AsSpan(Node.Children.Nodes);
            for (int i = 0; i < nodesSpan.Length; i++)
            {
                JtNode item = nodesSpan[i];
                JtNodeViewModel vm = Create(item, item.Name is null ? null : ValidValue?[item.Name], eventManagerContext, this);
                children[i] = vm;
                vm.ValueChanged += (vm, e) =>
                {
                    //if (e.ReplaceValue) //TODO: e.ReplaceValue
                    UpdateValueForChild(vm);
                    OnValueChanged(e);
                };

                //check for existing families
                Span<JtTwinFamilyViewModel> familiesSpan2 = CollectionsMarshal.AsSpan(containingTwinFamilies);
                bool isFamilySet = false;
                for (int j = 0; j < familiesSpan2.Length; j++)
                {
                    JtTwinFamilyViewModel family = familiesSpan2[j];
                    if (family.Name == item.Name)
                    {
                        family.members.Add(vm);
                        vm.SetTwinFamily(family);
                        isFamilySet = true;
                        break;
                    }
                }

                if (isFamilySet)
                    continue;


                for (int j = 0; j < i; j++)
                {
                    if (children[j]?.Node.Name == item.Name)
                    {
                        JtTwinFamilyViewModel newFamily = new JtTwinFamilyViewModel(children[j])
                        {
                            vm
                        };
                        vm.SetTwinFamily(newFamily);
                        containingTwinFamilies.Add(newFamily);
                    }
                }
            }

            Span<JtTwinFamilyViewModel> familiesSpan = CollectionsMarshal.AsSpan(containingTwinFamilies);

            for (int i = 0; i < familiesSpan.Length; i++)
            {
                familiesSpan[i].UpdateSelectedNode();
            }
        }
        else
        {
            for (int i = 0; i < Node.Children.Nodes.Count; i++)
            {
                JToken? v = null;
                int index = Node.Children.Nodes.IndexOf(Node.Children.Nodes[i]);
                if (index >= 0 && ValidValue!.Count > index)
                    v = Value[index];

                children[i] = Create(Node, v, eventManagerContext, this);
            }
        }
    }

    internal void UpdateValueForChild(JtNodeViewModel childNvm)
    {
        if (childNvm.IsSavable)
        {
            if (Node.ContainerJsonType is JtContainerType.Block)
                Value[childNvm.Node.Name!] = childNvm.Value;
            else
            {
                int index = Node.Children.Nodes.IndexOf(childNvm.Node);
                if (ValidValue!.Count > index)
                    Value[index] = childNvm.Value;
                else
                {
                    while (ValidValue.Count < index)
                    {
                        ValidValue.Add(JValue.CreateNull());
                    }

                    ValidValue.Add(childNvm.Value);
                }
            }
        }
        else
        {
            if (Node.ContainerJsonType is JtContainerType.Array)
                ((JArray)Value).Remove(childNvm.Value);
            else
                ((JObject)Value).Remove(childNvm.Node.Name!);
        }
    }
    public List<JtTwinFamilyViewModel>? GetContainingTwinFamilies() => containingTwinFamilies;
}
