using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Aadev.JTF.Editor.ViewModels;
using Aadev.JTF.Nodes;

namespace Aadev.JTF.Editor.EditorItems;

internal sealed class ValueEditorItem : EditorItem
{
    private Control? valueBox;
    private Rectangle textBoxBounds = Rectangle.Empty;
    private Rectangle discardInvalidValueButtonBounds = Rectangle.Empty;
    private Rectangle restoreDefaultValueButtonBounds = Rectangle.Empty;
    private bool dontCreateTextBoxUntilNewFocus = false;

    public new JtValueViewModel ViewModel => (JtValueViewModel)base.ViewModel;
    private new JtValueNode Node => (JtValueNode)base.Node;


    protected override bool IsFocused => base.IsFocused || valueBox?.Focused is true;
    protected override Color BorderColor
    {
        get
        {
            if (ViewModel.InvalidValue)
            {
                if (Node.ForceUsingSuggestions)
                    return RootEditor.ColorTable.InvalidBorderColor;
                else
                    return RootEditor.ColorTable.WarningBorderColor;
            }

            return base.BorderColor;
        }
    }
    internal ValueEditorItem(JtValueViewModel node, JsonJtfEditor rootEditor) : base(node, rootEditor) { }



    private void CreateTextBox(bool doubleClick = false)
    {
        if (ViewModel.IsInvalidValueType)
            return;
        if (textBoxBounds == Rectangle.Empty)
            return;
        if (valueBox is not null)
            return;
        if (Parent is EditorItem parent && parent.SuspendFocus)
            return;

        if (Node.TryGetSuggestions() is null || Node.Suggestions.IsEmpty)
        {
            TextBox textBox = new TextBox
            {
                Font = Font,
                BorderStyle = BorderStyle.None,
                BackColor = RootEditor.ColorTable.TextBoxBackColor,
                ForeColor = RootEditor.ColorTable.TextBoxForeColor,
                AutoSize = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Width = textBoxBounds.Width - 20,

                Text = ViewModel.Value.ToString(),
                ReadOnly = ViewModel.Root.IsReadOnly
            };
            textBox.Location = new Point(textBoxBounds.X + 10, 16 - (textBox.Height / 2));

            textBox.LostFocus += (sender, e) =>
            {
                if (textBox is null)
                    return;
                Controls.Remove(textBox);
                valueBox = null;
                Invalidate();
            };
            textBox.KeyDown += (sender, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    dontCreateTextBoxUntilNewFocus = true;
                    Controls.Remove(textBox);
                    e.Handled = true;
                }
            };

            if (Node is JtStringNode strNode)
            {
                if (strNode.MaxLength > 0)
                    textBox.MaxLength = strNode.MaxLength;
                textBox.TextChanged += (sender, eventArgs) =>
                {
                    ViewModel.SetValue(textBox.Text);
                };
            }
            else
            {
                textBox.KeyPress += (sender, e) =>
                {
                    if (char.IsDigit(e.KeyChar) || char.IsControl(e.KeyChar))
                        e.Handled = false;
                    else if (e.KeyChar is '-' && !textBox.Text.Contains('-', StringComparison.Ordinal))
                        e.Handled = false;
                    else if (e.KeyChar == ',' && (Node is JtFloatNode or JtDoubleNode) && !textBox.Text.Contains(',', StringComparison.Ordinal))
                        e.Handled = false;
                    else if (e.KeyChar == 'e' && (Node is JtFloatNode or JtDoubleNode) && !textBox.Text.Contains('e', StringComparison.OrdinalIgnoreCase))
                        e.Handled = false;
                    else
                        e.Handled = true;
                };

                textBox.TextChanged += (sender, e) =>
                {
                    if (string.IsNullOrEmpty(textBox.Text))
                    {
                        textBox.Text = Node.GetDefaultValue().ToString();
                        textBox.SelectAll();

                        ViewModel.SetValue(Node.GetDefaultValue());
                        return;
                    }

                    ViewModel.ParseValue(textBox.Text);
                };

            }




            Controls.Add(textBox);
            textBox?.Focus();
            textBox?.SelectAll();
            valueBox = textBox;

        }
        else
        {
            IJtSuggestion[] suggestions = Node.Suggestions.GetSuggestions(ViewModel.GetDynamicSuggestions).ToArray();

            if (ViewModel.IsUsingSuggestionSelector(suggestions.Length))
            {
                if (!doubleClick)
                    return;
                ViewModel.ShowSuggestionSelector(suggestions);
            }
            else
            {
                ComboBox comboBox = new ComboBox
                {
                    Font = Font,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = RootEditor.ColorTable.TextBoxBackColor,
                    ForeColor = RootEditor.ColorTable.TextBoxForeColor,
                    AutoSize = false,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    Width = textBoxBounds.Width - 20,
                    Text = ViewModel.Value.ToString(),

                };


                comboBox.Location = new Point(textBoxBounds.X + 10, 16 - (comboBox.Height / 2) - 4);
                if (Node.ForceUsingSuggestions)
                {
                    comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                }
                else
                {
                    comboBox.DropDownStyle = ComboBoxStyle.DropDown;
                    comboBox.AutoCompleteMode = AutoCompleteMode.Suggest;
                    comboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
                }


                Controls.Add(comboBox);


                comboBox.Items.AddRange(suggestions);

                comboBox.SelectedItem = ViewModel.ValidValue.Value;

                comboBox.Focus();
                comboBox.DroppedDown = true;




                comboBox.SelectedIndexChanged += (sender, eventArgs) =>
                {
                    if (comboBox.SelectedItem is null)
                        return;
                    ViewModel.SetValue(((IJtSuggestion)comboBox.SelectedItem).GetValue());
                };


                if (!Node.ForceUsingSuggestions)
                {
                    comboBox.TextChanged += (sender, eventArgs) => ViewModel.SetValue(comboBox?.Text);
                }

                comboBox.LostFocus += (s, e) =>
                {
                    Controls.Remove(comboBox);
                    valueBox = null;
                    Invalidate();
                };
                valueBox = comboBox;
            }
        }
    }
    protected override void PostDraw(Graphics g, ref DrawingBounds db)
    {
        if (ViewModel.IsInvalidValueType)
            return;


        if (ViewModel.InvalidValue && Node.ForceUsingSuggestions)
        {
            string message = string.Format(CultureInfo.CurrentCulture, Properties.Resources.InvalidValue, ViewModel.Value.ToString());

            SizeF sf = g.MeasureString(message, Font);
            g.DrawString(message, Font, RootEditor.ColorTable.InvalidValueBrush, new PointF(db.xOffset + 10, 16 - (sf.Height / 2)));

            db.xOffset += (int)sf.Width + 10;



            string discardMessage = Properties.Resources.DiscardInvalidValue;


            SizeF dsf = g.MeasureString(discardMessage, Font);

            discardInvalidValueButtonBounds = new Rectangle(db.xOffset, db.yOffset, (int)dsf.Width + 10, db.innerHeight);
            g.FillRectangle(RootEditor.ColorTable.DiscardInvalidValueButtonBackBrush, discardInvalidValueButtonBounds);
            g.DrawString(discardMessage, Font, RootEditor.ColorTable.DiscardInvalidValueButtonForeBrush, db.xOffset + 5, 16 - (dsf.Height / 2));



            db.xOffset += (int)sf.Width + 20;


            return;
        }

        if (!ViewModel.IsEqualToDefaultValue() && !Node.IsArrayPrefab)
        {
            int width = IsFocused ? 29 : 30;
            restoreDefaultValueButtonBounds = new Rectangle(Width - db.xRightOffset - width, db.yOffset, width, db.innerHeight);

            g.FillRectangle(RootEditor.ColorTable.RestoreDefaultValueButtonBackBrush, restoreDefaultValueButtonBounds);

            g.DrawLine(RootEditor.ColorTable.RestoreDefaultValueButtonForePen, Width - db.xRightOffset - width + 8, 16, Width - db.xRightOffset - 8, 16);


            db.xRightOffset += width;
        }


        textBoxBounds = new Rectangle(db.xOffset, db.yOffset, Width - db.xOffset - db.xRightOffset, db.innerHeight);
        g.FillRectangle(RootEditor.ColorTable.TextBoxBackBrush, textBoxBounds);

        if (valueBox is null)
        {
            if (!(Node.TryGetSuggestions() is null || Node.Suggestions.IsEmpty) && ViewModel.ValidValue.Value is not null)
            {
                foreach (IJtSuggestion item in Node.Suggestions.GetSuggestions(ViewModel.GetDynamicSuggestions))
                {
                    if (!ViewModel.SuggestionEqualJValue(item, ViewModel.ValidValue))
                        continue;

                    SizeF s = g.MeasureString(item.DisplayName, Font);

                    SolidBrush brush2;

                    if (ViewModel.InvalidValue)
                    {
                        if (Node.ForceUsingSuggestions)
                            brush2 = RootEditor.ColorTable.InvalidValueBrush;
                        else
                            brush2 = RootEditor.ColorTable.WarningValueBrush;
                    }
                    else
                        brush2 = RootEditor.ColorTable.TextBoxForeBrush;

                    g.DrawString(item.DisplayName, Font, brush2, new PointF(db.xOffset + 10, 16 - (s.Height / 2)));
                    return;
                }
            }

            SolidBrush brush;

            if (ViewModel.InvalidValue)
            {
                if (Node.ForceUsingSuggestions)
                    brush = RootEditor.ColorTable.InvalidValueBrush;
                else
                    brush = RootEditor.ColorTable.WarningValueBrush;
            }
            else
                brush = RootEditor.ColorTable.TextBoxForeBrush;

            string? displayValue = Node.GetDisplayString(ViewModel.Value);

            SizeF sf = g.MeasureString(displayValue, Font);

            g.DrawString(displayValue, Font, brush, new PointF(db.xOffset + 10, 16 - (sf.Height / 2)));
        }
    }
    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);

        if (textBoxBounds.Contains(e.Location))
        {
            CreateTextBox(true);
            Invalidate();
            return;
        }

        if (ViewModel.Root.IsReadOnly)
            return;
        if (discardInvalidValueButtonBounds.Contains(e.Location))
        {
            ViewModel.CreateValue();
            return;
        }

        if (restoreDefaultValueButtonBounds.Contains(e.Location))
        {
            if (valueBox is not null)
            {
                Parent?.Focus();
                Controls.Remove(valueBox);
                valueBox = null;
                Focus();

            }

            ViewModel.CreateValue();
            return;
        }
    }
    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);

        if (txtDynamicName is not null)
            return;
        if (dontCreateTextBoxUntilNewFocus)
        {
            dontCreateTextBoxUntilNewFocus = false;
            return;
        }

        CreateTextBox();
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (ViewModel.IsInvalidValueType)
            return;
        if (textBoxBounds.Contains(e.Location))
        {
            if (Node.TryGetSuggestions() is null || Node.Suggestions.IsEmpty)
                Cursor = Cursors.IBeam;
            else
                Cursor = Cursors.Hand;
            return;
        }

        if ((discardInvalidValueButtonBounds.Contains(e.Location) || restoreDefaultValueButtonBounds.Contains(e.Location)) && !ViewModel.Root.IsReadOnly)
        {
            Cursor = Cursors.Hand;
            return;
        }
    }
}
