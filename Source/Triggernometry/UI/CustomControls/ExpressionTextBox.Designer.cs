using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Triggernometry.UI.CustomControls
{
    partial class ExpressionTextBox
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new Container();
            ComponentResourceManager resources = new ComponentResourceManager(typeof(ExpressionTextBox));
            this.panel1 = new Panel();
            this.textBox1 = new TextBox();
            this.toolTip1 = new ToolTip(this.components);
            this.panel2 = new Panel();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackgroundImage = ((Image)(resources.GetObject("panel1.BackgroundImage")));
            this.panel1.BackgroundImageLayout = ImageLayout.Center;
            this.panel1.Dock = DockStyle.Left;
            this.panel1.Location = new Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new Size(24, 106);
            this.panel1.TabIndex = 0;
            this.panel1.Click += new EventHandler(this.panel1_Click);
            this.panel1.DoubleClick += new EventHandler(this.panel1_DoubleClick);
            // 
            // textBox1
            // 
            this.textBox1.Dock = DockStyle.Top;
            this.textBox1.Location = new Point(24, 0);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new Size(407, 20);
            this.textBox1.TabIndex = 1;
            this.textBox1.WordWrap = false;
            // 
            // panel2
            // 
            this.panel2.BackgroundImage = ((Image)(resources.GetObject("panel2.BackgroundImage")));
            this.panel2.BackgroundImageLayout = ImageLayout.None;
            this.panel2.Location = new Point(43, 35);
            this.panel2.Name = "panel2";
            this.panel2.Size = new Size(41, 28);
            this.panel2.TabIndex = 0;
            this.panel2.Visible = false;
            // 
            // ExpressionTextBox
            // 
            this.AutoSize = true;
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.panel1);
            this.DoubleBuffered = true;
            this.Name = "ExpressionTextBox";
            this.Size = new Size(431, 106);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private ToolTip toolTip1;
        private Panel panel2;
        internal Panel panel1;
        internal TextBox textBox1;
    }
}
