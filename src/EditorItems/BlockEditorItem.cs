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
        private readonly IEventManagerProvider childrenEventManagerProvider;

        private new JtBlock Node => (JtBlock)base.Node;
        public override JToken Value
        {
            get => value;
            set
            {
                this.value = value;
                Invalidate();
                OnValueChanged();
            }
        }

        protected override bool IsFocused => base.IsFocused || focusControl?.Focused is true;
        internal override bool IsSaveable => base.IsSaveable || (!IsInvalidValueType && ValidValue.Count > 0);

        public JContainer? ValidValue => Value as JContainer;
        [MemberNotNullWhen(false, "ValidValue")] public new bool IsInvalidValueType => base.IsInvalidValueType;
        internal BlockEditorItem(JtNode type, JToken? token, JsonJtfEditor jsonJtfEditor, IEventManagerProvider eventManagerProvider) : base(type, token, jsonJtfEditor, eventManagerProvider)
        {
            if (value is null)
                value = (JContainer)Node.CreateDefaultValue();

            SetStyle(ControlStyles.ContainerControl, true);

            if (Node.Children.CustomSourceId is not null)
            {
                childrenEventManagerProvider = new BlankEventManagerProvider();
            }
            else
            {
                childrenEventManagerProvider = eventManagerProvider;
            }
        }

        private void UpdateLayout()
        {
            SuspendLayout();
            int yOffset = Node.IsRoot ? 10 : 38;
            foreach (Control ctr in Controls)
            {
                if (ctr is not IJsonItem)
                    continue;
                if (ctr.Top != yOffset)
                    ctr.Top = yOffset;
                yOffset += ctr.Height;
                if (ctr.Height != 0)
                {
                    yOffset += 5;
                }
            }
            y = yOffset + 10;
            if (!Expanded)
                y = 32;
            Height = y;
            ResumeLayout();
        }
        private (int, EditorItem) CreateEditorItem(JtNode type, int y, bool resizeOnCreate = false, int insertIndex = -1)
        {
            EditorItem bei;

            if (resizeOnCreate)
            {
                value[type.Name!] = null;
                bei = Create(type, null, RootEditor, childrenEventManagerProvider);
            }
            else
            {
                if (Node.ContainerJsonType is JtContainerType.Block)
                    bei = Create(type, value[type.Name!], RootEditor, childrenEventManagerProvider);
                else
                {
                    JToken? value = null;
                    int index = type.Parent?.Children.IndexOf(type) ?? -1;
                    if (index >= 0 && ValidValue!.Count > index)
                        value = this.value[index];



                    bei = Create(type, value, RootEditor, childrenEventManagerProvider);
                }
            }


            bei.Location = new System.Drawing.Point(10, y);
            bei.Width = Width - 20;

            if (bei.IsSaveable)
            {
                if (Node.ContainerJsonType is JtContainerType.Block)
                    value[type.Name!] = bei.Value;
                else
                {
                    int index = type.Parent?.Children.IndexOf(type) ?? -1;
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
                        value[type.Name!] = bei.Value;
                    else
                    {
                        int index = type.Parent?.Children.IndexOf(type) ?? -1;
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

                OnValueChanged();
            };

            bei.TwinTypeChanged += (sender, e) =>
            {
                if (sender is not EditorItem bei)
                    return;
                SuspendLayout();
                int index = Controls.IndexOf(bei);
                Controls.Remove(bei);

                (_, EditorItem newei) = CreateEditorItem(e.NewTwinNode, bei.Top, true, index);

                newei.TabIndex = bei.TabIndex;

                if (Value is JObject jobject)
                {
                    if (jobject[bei.Node.Name!] is JToken)
                    {
                        jobject.Remove(bei.Node.Name!);
                        OnValueChanged();
                    }

                }


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
            y = Node.IsRoot ? 10 : 38;
            if (!Node.IsRoot)
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
            foreach (JtNode item in Node.Children)
            {
                if (!jsonNodes.Contains(item.Name!))
                    jsonNodes.Add(item.Name!);
                JtNode[]? twinFamily = item.GetTwinFamily().ToArray();
                


                if (twinFamily.Length > 1)
                {
                    if (twins.Contains(item.Name!))
                    {
                        continue;
                    }

                    JtNode? t = twinFamily.FirstOrDefault(x => x.JsonType == value[item.Name!]?.Type);

                    if (t is null)
                    {
                        (y, EditorItem ei2) = CreateEditorItem(item, y);
                        ei2.TabIndex = index;
                        twins.Add(item.Name!);
                        index++;

                        continue;
                    }
                    (y, EditorItem ei3) = CreateEditorItem(t, y);
                    ei3.TabIndex = index;
                    twins.Add(item.Name!);
                    index++;
                    continue;
                }
                (y, EditorItem ei) = CreateEditorItem(item, y);
                ei.TabIndex = index;
                index++;

            }
            if (value is JObject obj)
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
            if (!Expanded)
                return;
            OnValueChanged();
            UpdateLayout();
        }
    }
}