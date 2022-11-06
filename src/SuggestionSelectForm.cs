using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Aadev.JTF.Editor
{
    public partial class SuggestionSelectForm : Form, ISuggestionSelector
    {
        public SuggestionSelectForm()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterParent;
        }

        public IJtSuggestion? SelectedSuggestion { get; set; }

        private IJtSuggestion? orginalSuggestion;

        private IJtSuggestion[]? suggestions;
        private bool forceUsingSuggestion;
        public DialogResult Show(IJtSuggestion[] suggestions, bool forceUsingSuggestion, IJtSuggestion? selectedSuggestion = null)
        {
            orginalSuggestion = selectedSuggestion;
            this.suggestions = suggestions;
            this.forceUsingSuggestion = forceUsingSuggestion;
            listBox.Items.Clear();
            if (!suggestions.Contains(selectedSuggestion) && !forceUsingSuggestion)
            {
                listBox.Items.Add(selectedSuggestion ?? new DynamicSuggestion<string>(string.Empty));
            }
            listBox.Items.AddRange(suggestions);
            if (selectedSuggestion != null)
            {
                listBox.SelectedItem = selectedSuggestion;
            }

            Height = Math.Min(600, 134 + listBox.Items.Count * 24);

            return ShowDialog();
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            textBox.Focus();
            textBox.Clear();
        }
        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Close();
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            Visible = false;
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            listBox.BeginUpdate();
            object? selectedItem = listBox.SelectedItem;
            listBox.Items.Clear();
            IJtSuggestion[] s = suggestions!.Where(x => x.GetValue()?.ToString()?.Contains(textBox.Text, StringComparison.CurrentCultureIgnoreCase) is true || x.DisplayName?.Contains(textBox.Text, StringComparison.CurrentCultureIgnoreCase) is true).ToArray();
            if (!forceUsingSuggestion)
            {
                if (!s.Any(x => x.StringValue == textBox.Text))
                    listBox.Items.Add(new DynamicSuggestion<string>(textBox.Text));
                if (!suggestions!.Contains(orginalSuggestion) && orginalSuggestion?.StringValue != textBox.Text)
                    listBox.Items.Add(orginalSuggestion!);
            }

            listBox.Items.AddRange(s);
            if (listBox.Items.Count > 0)
            {
                if (selectedItem is not null && listBox.Items.Contains(selectedItem))
                    listBox.SelectedItem = selectedItem;
                else
                    listBox.SelectedIndex = 0;

            }
            listBox.EndUpdate();
        }

        private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
        private void ListBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            Graphics g = e.Graphics;
            string? itemText = listBox.GetItemText(listBox.Items[e.Index]);
            if ((e.State & DrawItemState.Selected) != 0)
            {
                using SolidBrush brush = new SolidBrush(SystemColors.Highlight);
                g.FillRectangle(brush, e.Bounds);
            }
            else if (orginalSuggestion == listBox.Items[e.Index])
            {
                using SolidBrush brush = new SolidBrush(Color.ForestGreen);
                g.FillRectangle(brush, e.Bounds);
            }
            else if (e.Index % 2 == 1)
            {
                using SolidBrush brush = new SolidBrush(Color.FromArgb(60, 60, 60));
                g.FillRectangle(brush, e.Bounds);
            }
            else
            {
                using SolidBrush brush = new SolidBrush(Color.FromArgb(50, 50, 50));
                g.FillRectangle(brush, e.Bounds);
            }

            using SolidBrush ForecolorBrush = new SolidBrush(e.ForeColor);
            using SolidBrush nameBrush = new SolidBrush(Color.LightGray);
            using Font valueFont = new Font(Font.FontFamily, 8, FontStyle.Regular);

            string valueText = $"({((IJtSuggestion)listBox.Items[e.Index]).StringValue})";
            SizeF textSize = g.MeasureString(itemText, Font);
            SizeF valueTextSize = g.MeasureString(valueText, valueFont);

            g.DrawString(itemText, base.Font, ForecolorBrush, new PointF(16, e.Bounds.Top + e.Bounds.Height / 2 - textSize.Height / 2));
            g.DrawString(valueText, valueFont, nameBrush, new PointF(32 + textSize.Width, e.Bounds.Top + e.Bounds.Height / 2 - valueTextSize.Height / 2));

        }

        private void ListBox_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            int index = listBox.IndexFromPoint(e.Location);
            if (index is not ListBox.NoMatches)
            {
                SelectedSuggestion = (IJtSuggestion?)listBox.SelectedItem;
                DialogResult = DialogResult.OK;
            }
        }


        private void SuggestionSelectForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode is Keys.Enter && listBox.SelectedItem != null)
            {
                SelectedSuggestion = (IJtSuggestion)listBox.SelectedItem;
                DialogResult = DialogResult.OK;
            }
            else if (e.KeyCode is Keys.Escape)
            {
                SelectedSuggestion = null;
                DialogResult = DialogResult.Cancel;
            }
            else
            {
                textBox.Focus();
            }

        }

    }
    public class DynamicSuggestion<TSuggestion> : IJtSuggestion
    {
        public Type SuggestionType => typeof(TSuggestion);

        public string? DisplayName { get => StringValue; set { } }

        public string? StringValue => value?.ToString();

        private readonly TSuggestion value;

        public DynamicSuggestion(TSuggestion value)
        {
            this.value = value;
        }

        public override string? ToString() => StringValue;
        public T GetValue<T>() => throw new NotImplementedException();
        public object? GetValue() => value;
        public void SetValue<T>(T value) => throw new NotImplementedException();
        public void SetValue(object? value) => throw new NotImplementedException();
    }
}
