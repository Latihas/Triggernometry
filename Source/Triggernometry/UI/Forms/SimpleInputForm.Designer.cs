using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Triggernometry.UI.CustomControls;

namespace Triggernometry.UI.Forms
{
    partial class SimpleInputForm
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
            this.components = new Container();
            this.expEditedValue = new ExpressionTextBox();
            this.okButton = new Button();
            this.cancelButton = new Button();
            this.buttons = new FlowLayoutPanel();
            this.table = new TableLayoutPanel();
            this.buttons.SuspendLayout();
            this.table.SuspendLayout();
            this.SuspendLayout();
            //
            // expEditedValue
            //
            this.expEditedValue.Dock = DockStyle.Fill;
            this.expEditedValue.Location = new Point(18, 18);
            this.expEditedValue.Name = "expEditedValue";
            this.expEditedValue.Size = new Size(444, 48);
            this.expEditedValue.TabIndex = 0;
            //
            // okButton
            //
            this.okButton.AutoSize = true;
            this.okButton.DialogResult = DialogResult.OK;
            this.okButton.Location = new Point(285, 13);
            this.okButton.Name = "okButton";
            this.okButton.Size = new Size(75, 24);
            this.okButton.TabIndex = 1;
            this.okButton.Text = "OK";
            //
            // cancelButton
            //
            this.cancelButton.AutoSize = true;
            this.cancelButton.DialogResult = DialogResult.Cancel;
            this.cancelButton.Location = new Point(366, 13);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new Size(75, 24);
            this.cancelButton.TabIndex = 0;
            this.cancelButton.Text = "Cancel";
            //
            // buttons
            //
            this.buttons.AutoSize = true;
            this.buttons.Controls.Add(this.cancelButton);
            this.buttons.Controls.Add(this.okButton);
            this.buttons.Dock = DockStyle.Fill;
            this.buttons.FlowDirection = FlowDirection.RightToLeft;
            this.buttons.Location = new Point(18, 72);
            this.buttons.Name = "buttons";
            this.buttons.Padding = new Padding(0, 10, 0, 0);
            this.buttons.Size = new Size(444, 40);
            this.buttons.TabIndex = 1;
            this.buttons.WrapContents = false;
            //
            // table
            //
            this.table.ColumnCount = 1;
            this.table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.table.Controls.Add(this.expEditedValue, 0, 0);
            this.table.Controls.Add(this.buttons, 0, 1);
            this.table.Dock = DockStyle.Fill;
            this.table.Location = new Point(0, 0);
            this.table.Name = "table";
            this.table.Padding = new Padding(15);
            this.table.RowCount = 2;
            this.table.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.table.RowStyles.Add(new RowStyle());
            this.table.Size = new Size(480, 130);
            this.table.TabIndex = 0;
            //
            // SimpleInputForm
            //
            this.AcceptButton = this.okButton;
            this.AutoScaleMode = AutoScaleMode.Inherit;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new Size(480, 130);
            this.Controls.Add(this.table);
            this.KeyPreview = true;
            this.MinimizeBox = false;
            this.MinimumSize = new Size(360, 160);
            this.Name = "SimpleInputForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Disposed += new EventHandler(this.SimpleInputForm_Disposed);
            this.Shown += new EventHandler(this.SimpleInputForm_Shown);
            this.KeyDown += new KeyEventHandler(this.SimpleInputForm_KeyDown);
            this.buttons.ResumeLayout(false);
            this.buttons.PerformLayout();
            this.table.ResumeLayout(false);
            this.table.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private ExpressionTextBox expEditedValue;
        private Button okButton;
        private Button cancelButton;
        private FlowLayoutPanel buttons;
        private TableLayoutPanel table;
    }
}
