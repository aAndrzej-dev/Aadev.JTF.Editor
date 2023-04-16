namespace Aadev.JTF.Editor
{
    partial class SuggestionSelectForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            textBox = new System.Windows.Forms.TextBox();
            listBox = new System.Windows.Forms.ListBox();
            SuspendLayout();
            // 
            // textBox
            // 
            textBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            textBox.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            textBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            textBox.ForeColor = System.Drawing.Color.White;
            textBox.HideSelection = false;
            textBox.Location = new System.Drawing.Point(12, 12);
            textBox.Name = "textBox";
            textBox.Size = new System.Drawing.Size(781, 25);
            textBox.TabIndex = 0;
            textBox.TextChanged += TextBox_TextChanged;
            // 
            // listBox
            // 
            listBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            listBox.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);
            listBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            listBox.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            listBox.ForeColor = System.Drawing.Color.White;
            listBox.FormattingEnabled = true;
            listBox.ItemHeight = 24;
            listBox.Location = new System.Drawing.Point(12, 43);
            listBox.Name = "listBox";
            listBox.Size = new System.Drawing.Size(781, 480);
            listBox.TabIndex = 1;
            listBox.DrawItem += ListBox_DrawItem;
            listBox.SelectedIndexChanged += ListBox_SelectedIndexChanged;
            listBox.MouseDoubleClick += ListBox_MouseDoubleClick;
            // 
            // SuggestionSelectForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            ClientSize = new System.Drawing.Size(805, 546);
            Controls.Add(listBox);
            Controls.Add(textBox);
            Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            ForeColor = System.Drawing.Color.White;
            KeyPreview = true;
            MinimizeBox = false;
            Name = "SuggestionSelectForm";
            ShowIcon = false;
            ShowInTaskbar = false;
            SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Select Value";
            KeyDown += SuggestionSelectForm_KeyDown;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TextBox textBox;
        private System.Windows.Forms.ListBox listBox;
    }
}