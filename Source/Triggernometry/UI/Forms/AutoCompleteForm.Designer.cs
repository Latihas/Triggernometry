
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Triggernometry.UI.Forms
{
    partial class AutoCompleteForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private IContainer components = null;

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
            this.listBox1 = new ListBox();
            this.SuspendLayout();
            // 
            // listBox1
            // 
            this.listBox1.BackColor = SystemColors.Info;
            this.listBox1.BorderStyle = BorderStyle.FixedSingle;
            this.listBox1.FormattingEnabled = true;
            this.listBox1.ItemHeight = 16;
            this.listBox1.Location = new Point(0, 0);
            this.listBox1.Margin = new Padding(4);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new Size(100, 66);
            this.listBox1.TabIndex = 0;
            // 
            // AutoCompleteForm
            // 
            this.AutoScaleDimensions = new SizeF(8F, 16F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.ClientSize = new Size(242, 138);
            this.Controls.Add(this.listBox1);
            this.Font = new Font("Courier New", 9.75F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = FormBorderStyle.None;
            this.Margin = new Padding(4);
            this.Name = "AutoCompleteForm";
            this.Opacity = 0.8D;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Text = "AutoCompleteForm";
            this.ResumeLayout(false);

        }

        #endregion

        internal ListBox listBox1;
    }
}