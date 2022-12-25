using Aadev.JTF.Types;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;

namespace Aadev.JTF.Editor.EditorItems
{
    internal sealed class BlockEditorItem : EditorItem
    {
        private int y;
        private FocusableControl? focusControl;
        private JToken value;

        private new JtBlock Node => (JtBlock)base.Node;
        public override JToken Value
        {
            get => value;
            set
            {
                JToken oldValue = this.value;
                this.value = value;
                Invalidate();
                OnValueChanged(new JtfEditorAction(JtfEditorAction.JtEditorActionType.ChangeValue, oldValue, value, this));
            }
        }

        protected override bool IsFocused => base.IsFocused || focusControl?.Focused is true;
        internal override bool IsSaveable => base.IsSaveable || (!IsInvalidValueType && ValidValue.Count > 0);

        public JContainer? ValidValue => Value as JContainer;
        [MemberNotNullWhen(false, "ValidValue")] public new bool IsInvalidValueType => base.IsInvalidValueType;

        private bool suspendUpdatingLayout;

        internal BlockEditorItem(JtBlock node, JToken? token, JsonJtfEditor jsonJtfEditor, EventManager eventManager) : base(node, token, jsonJtfEditor, eventManager)
        {
            value ??= (JContainer)Node.CreateDefaultValue();

            SetStyle(ControlStyles.ContainerControl, true);

            childrenEventManager = node.HasExternalChildren ? new EventManager(node.Children.GetIdentifiersManagerForChild(), eventManager) : eventManager;
        }

        private void UpdateLayout()
        {
            if (suspendUpdatingLayout)
                return;


            SuspendLayout();
            int yOffset = (Node.IsRoot && Node.Template.Roots.Count == 1) ? 10 : 38;
            foreach (Control ctr in Controls)
            {
                if (ctr is not IJsonItem)
                    continue;
                if (ctr.Top != yOffset)
                    ctr.Top = yOffset;
                if (ctr.Height != 0)
                {
                    yOffset += ctr.Height + 5;
                }
            }
            y = yOffset + 10;
            if (!Expanded)
                y = 32;
            Height = y;




            ResumeLayout();
        }
        private readonly EventManager childrenEventManager;
        private (int, EditorItem) CreateEditorItem(JtNode node, int y, bool resizeOnCreate = false, int insertIndex = -1)
        {
            EditorItem bei;


            if (resizeOnCreate)
            {
                if(node.Name is not null)
                    value[node.Name] = null;
                bei = Create(node, null, RootEditor, childrenEventManager);
            }
            else
            {
                if (Node.ContainerJsonType is JtContainerType.Block)
                    bei = Create(node, node.Name is null ? null : value[node.Name], RootEditor, childrenEventManager);
                else
                {
                    JToken? value = null;
                    int index = node.Parent.Owner?.Children.IndexOf(node) ?? -1;
                    if (index >= 0 && ValidValue!.Count > index)
                        value = this.value[index];



                    bei = Create(node, value, RootEditor, childrenEventManager);
                }
            }


            bei.Location = new System.Drawing.Point(10, y);
            bei.Width = Width - 20;

            if (bei.IsSaveable)
            {
                if (Node.ContainerJsonType is JtContainerType.Block)
                    value[node.Name!] = bei.Value;
                else
                {
                    int index = node.Parent.Owner?.Children.IndexOf(node) ?? -1;
                    if (ValidValue!.Count > index)
                        value[index] = bei.Value;
                    else
                    {
                        while (ValidValue.Count < index)
                        {
                            ValidValue.Add(JValue.CreateNull());
                        }
                        ValidValue.Add(bei.Value);
                    }

                }
            }

            Controls.Add(bei);
            if (insertIndex >= 0)
            {
                Controls.SetChildIndex(bei, insertIndex);
            }





            if (resizeOnCreate)
            {
                UpdateLayout();
            }
            bei.HeightChanged += (sender, e) => UpdateLayout();
            bei.ValueChanged += (sender, e) =>
            {
                if (sender is not EditorItem bei)
                    return;
                if (bei.IsSaveable)
                {
                    if (Node.ContainerJsonType is JtContainerType.Block)
                        value[node.Name!] = bei.Value;
                    else
                    {
                        int index = node.Parent.Owner?.Children.IndexOf(node) ?? -1;
                        if (ValidValue!.Count > index)
                            value[index] = bei.Value;
                        else
                        {
                            while (ValidValue.Count < index)
                            {
                                ValidValue.Add(JValue.CreateNull());
                            }
                            ValidValue.Add(bei.Value);
                        }

                    }
                }
                else
                {
                    if (Node.ContainerJsonType is JtContainerType.Array)
                    {
                        ((JArray)value).Remove(bei.Value);
                    }
                    else
                        ((JObject)value).Remove(bei.Node.Name!);

                }
                Invalidate();
                OnValueChanged(e);
            };

            bei.TwinTypeChanged += (sender, e) =>
            {
                if (sender is not EditorItem bei || RootEditor.ReadOnly)
                    return;
                SuspendLayout();
                suspendUpdatingLayout = true;
                int oldHeight = bei.Height;
                int index = Controls.IndexOf(bei);
                Controls.Remove(bei);
                JToken oldValue = bei.Value;
                (_, EditorItem newei) = CreateEditorItem(e.NewTwinNode, bei.Top, true, index);

                newei.TabIndex = bei.TabIndex;
                newei.Focus();
                if (Value is JObject jobject)
                {
                    if (jobject[bei.Node.Name!] is not null)
                    {
                        jobject.Remove(bei.Node.Name!);
                        OnValueChanged(new JtfEditorAction(JtfEditorAction.JtEditorActionType.ChangeTwinType, oldValue, newei.Value, this));
                    }

                }

                suspendUpdatingLayout = false;

                if (oldHeight != newei.Height)
                    UpdateLayout();


                ResumeLayout();
            };
            if (bei.Height != 0)
            {
                y += bei.Height + 5;
            }

            return (y, bei);
        }


        protected override void OnExpandChanged()
        {
            SuspendLayout();
            if (!Expanded)
            {
                Height = 32;
                if (Node.IsDynamicName)
                    Controls.Remove(focusControl);
                Controls.Clear();
                focusControl = null;
                base.OnExpandChanged();
                ResumeLayout();
                return;
            }
            if (IsInvalidValueType)
            {
                ResumeLayout();
                return;
            }
            y = (Node.IsRoot && Node.Template.Roots.Count == 1) ? 10 : 38;
            if (!Node.IsRoot || Node.Template.Roots.Count > 1)
            {

                focusControl = new FocusableControl
                {
                    Height = 0,
                    Width = 0,
                    Top = 0,
                    Left = 0
                };
                focusControl.GotFocus += (s, e) =>
                {
                    if (Node.IsDynamicName)
                    {
                        if (txtDynamicName is null)
                            CreateDynamicNameTextBox();
                        else
                            txtDynamicName.Focus();
                    }
                    else
                    {
                        Invalidate();

                    }

                };
                focusControl.LostFocus += (s, e) => Invalidate();
                focusControl.KeyDown += (s, e) =>
                {
                    if (IsInvalidValueType)
                        return;

                    if (e.KeyCode == Keys.Space)
                    {
                        Expanded = !Expanded;
                    }
                };
                Controls.Add(focusControl);
                focusControl?.Focus();
            }
            else
            {
                Controls.Remove(focusControl);
                focusControl = null;
            }

            List<string> jsonNodes = new List<string>();

            List<string> twins = new();
            int index = 0;

            if (Node.IsDynamicName)
                index++;
            foreach (JtNode item in Node.Children.Nodes!)
            {
                if (item.Name is null)
                    continue;
                if (!jsonNodes.Contains(item.Name))
                    jsonNodes.Add(item.Name);


                if (RootEditor.ReadOnly && !RootEditor.ShowEmptyNodesInReadOnlyMode && Node.ContainerJsonType is JtContainerType.Block && value[item.Name] is null or { Type: JTokenType.Null })
                {
                    continue;
                }



                JtNode[]? twinFamily = item.GetTwinFamily().ToArray();

                if (twinFamily.Length > 1)
                {
                    if (twins.Contains(item.Name))
                    {
                        continue;
                    }
                    if (((JObject)value).ContainsKey(item.Name))
                    {
                        JtNode? t = twinFamily.FirstOrDefault(x => x.JsonType == value[item.Name]?.Type);
                        if (t is not null)
                        {
                            (y, EditorItem ei3) = CreateEditorItem(t, y);
                            ei3.TabIndex = index;
                            twins.Add(item.Name);
                            index++;
                            continue;
                        }
                    }


                    (y, EditorItem ei2) = CreateEditorItem(item, y);
                    ei2.TabIndex = index;
                    twins.Add(item.Name);
                    index++;

                    continue;


                }
                (y, EditorItem ei) = CreateEditorItem(item, y);
                ei.TabIndex = index;
                index++;

            }
            if (value is JObject obj)
            {
                foreach (KeyValuePair<string, JToken?> item in obj)
                {
                    if (jsonNodes.Contains(item.Key))
                        continue;

                    InvalidJsonItem invalidJsonItem = new InvalidJsonItem(item.Value!, RootEditor)
                    {
                        Location = new System.Drawing.Point(10, y),
                        Width = Width - 20
                    };
                    Controls.Add(invalidJsonItem);
                    y += 32 + 5;
                }
            }
                

            Height = y + 5;
            ResumeLayout();
            base.OnExpandChanged();
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);

            if (Expanded)
            {
                focusControl?.Focus();
            }
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            int w = Width - 20;

            foreach (Control item in Controls)
            {
                if (item.Width != w)
                    item.Width = w;
            }

        }
        protected override void OnControlRemoved(ControlEventArgs e)
        {
            base.OnControlRemoved(e);
            if (!Expanded || e.Control is not IJsonItem jsonItem)
                return;
            OnValueChanged(new JtfEditorAction(JtfEditorAction.JtEditorActionType.RemoveToken, jsonItem.Value, null, this));
            UpdateLayout();
        }
    }
}