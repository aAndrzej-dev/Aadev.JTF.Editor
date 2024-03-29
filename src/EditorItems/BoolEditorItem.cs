﻿using System;
using System.Drawing;
using System.Windows.Forms;
using Aadev.JTF.Types;
using Newtonsoft.Json.Linq;

namespace Aadev.JTF.Editor.EditorItems;

internal sealed class BoolEditorItem : EditorItem
{
    private JToken value;
    private Rectangle falsePanelRect = Rectangle.Empty;
    private Rectangle truePanelRect = Rectangle.Empty;

    internal override bool IsSavable => base.IsSavable || (Value.Type != JTokenType.Null && (bool?)Value != Node.Default);
    private bool? RawValue
    {
        get => value.Type == Node.JsonType ? ((bool?)value ?? Node.Default) : (value.Type is JTokenType.Null ? Node.Default : null);
        set => this.value = new JValue(value);
    }
    public override JToken Value
    {
        get => value;
        set
        {
            if (!JToken.DeepEquals(this.value, value))
            {
                JToken oldValue = this.value;
                this.value = value;
                Invalidate();
                OnValueChanged(new JtfEditorAction(JtfEditorAction.JtEditorActionType.ChangeValue, oldValue, value, this));
            }
        }
    }

    private new JtBoolNode Node => (JtBoolNode)base.Node;

    internal BoolEditorItem(JtBoolNode node, JToken? token, JsonJtfEditor rootEditor, EventManagerContext eventManagerContext) : base(node, token, rootEditor, eventManagerContext)
    {
        value ??= Node.CreateDefaultValue();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (IsInvalidValueType)
            return;
        if (IsFocused)
            xOffset++;
        int width = Width - xOffset - xRightOffset;


        Graphics g = e.Graphics;

        if (Node.Constant)
        {
            if (Node.Default)
            {
                truePanelRect = new Rectangle(xOffset, yOffset, width, innerHeight);
                falsePanelRect = Rectangle.Empty;
            }
            else
            {
                truePanelRect = Rectangle.Empty;
                falsePanelRect = new Rectangle(xOffset, yOffset, width, innerHeight);
            }
        }
        else
        {
            double halfWidth = width * 0.5;
            falsePanelRect = new Rectangle(xOffset, yOffset, (int)halfWidth, innerHeight);
            truePanelRect = new Rectangle(xOffset + (int)halfWidth, yOffset, (int)Math.Ceiling(halfWidth), innerHeight);
        }


        if (RawValue ?? Node.Default)
            g.FillRectangle(RootEditor.ColorTable.TrueValueBackBrush, truePanelRect);
        else
            g.FillRectangle(RootEditor.ColorTable.FalseValueBackBrush, falsePanelRect);

        if (!falsePanelRect.IsEmpty)
        {
            SizeF falseLabelSize = g.MeasureString(Properties.Resources.FalseValue, Font);

            g.DrawString(Properties.Resources.FalseValue, Font, (RawValue ?? Node.Default) ? ForeColorBrush : RootEditor.ColorTable.FalseValueForeBrush, falsePanelRect.X + (falsePanelRect.Width / 2) - (falseLabelSize.Width / 2), falsePanelRect.Y + (falsePanelRect.Height / 2) - (falseLabelSize.Height / 2));
        }

        if (!truePanelRect.IsEmpty)
        {
            SizeF trueLabelSize = g.MeasureString(Properties.Resources.TrueValue, Font);

            g.DrawString(Properties.Resources.TrueValue, Font, (RawValue ?? Node.Default) ? RootEditor.ColorTable.TrueValueForeBrush : ForeColorBrush, truePanelRect.X + (truePanelRect.Width / 2) - (trueLabelSize.Width / 2), truePanelRect.Y + (truePanelRect.Height / 2) - (trueLabelSize.Height / 2));
        }
    }
    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);

        if (IsInvalidValueType || RootEditor.ReadOnly)
            return;


        if (falsePanelRect.Contains(e.Location))
        {
            Value = false;
            Focus();
            return;
        }

        if (truePanelRect.Contains(e.Location))
        {
            Value = true;
            Focus();
            return;
        }
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (IsInvalidValueType || RootEditor.ReadOnly)
            return;
        if (e.KeyCode == Keys.Space)
        {
            Value = (bool?)Value is false;
        }
    }
}