using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Triggernometry.UI.CustomControls
{
    partial class RegisteredVariableViewer
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
            ComponentResourceManager resources = new ComponentResourceManager(typeof(RegisteredVariableViewer));
            this.toolStrip = new ToolStrip();
            this.btnAddVariable = new ToolStripButton();
            this.btnAddComment = new ToolStripButton();
            this.btnExpandEdit = new ToolStripButton();
            this.btnApply = new ToolStripButton();
            this.btnUp = new ToolStripButton();
            this.btnDown = new ToolStripButton();
            this.btnTop = new ToolStripButton();
            this.btnBottom = new ToolStripButton();
            this.btnRemove = new ToolStripButton();
            this.btnApplyAll = new ToolStripButton();
            this.sepToolbar1 = new ToolStripSeparator();
            this.sepToolbar2 = new ToolStripSeparator();
            this.dgvVariables = new CustomControls.DataGridViewEx();
            this.colEnabled = new DataGridViewCheckBoxColumn();
            this.colName = new DataGridViewTextBoxColumn();
            this.colValue = new DataGridViewTextBoxColumn();
            this.colLoadWhenActive = new DataGridViewComboBoxColumn();
            this.ctxVariables = new ContextMenuStrip(this.components);
            this.ctxApply = new ToolStripMenuItem();
            this.ctxApplyAll = new ToolStripMenuItem();
            this.ctxAddVariable = new ToolStripMenuItem();
            this.ctxAddComment = new ToolStripMenuItem();
            this.ctxExpandEdit = new ToolStripMenuItem();
            this.ctxCopy = new ToolStripMenuItem();
            this.ctxPaste = new ToolStripMenuItem();
            this.ctxUp = new ToolStripMenuItem();
            this.ctxDown = new ToolStripMenuItem();
            this.ctxTop = new ToolStripMenuItem();
            this.ctxBottom = new ToolStripMenuItem();
            this.ctxRemove = new ToolStripMenuItem();
            this.sepContext1 = new ToolStripSeparator();
            this.sepContext2 = new ToolStripSeparator();
            this.sepContext3 = new ToolStripSeparator();
            this.toolStrip.SuspendLayout();
            ((ISupportInitialize)(this.dgvVariables)).BeginInit();
            this.ctxVariables.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip
            // 
            this.toolStrip.GripStyle = ToolStripGripStyle.Hidden;
            this.toolStrip.Items.AddRange(new ToolStripItem[] {
            this.btnAddVariable,
            this.btnAddComment,
            this.btnExpandEdit,
            this.btnApply,
            this.sepToolbar1,
            this.btnUp,
            this.btnDown,
            this.btnTop,
            this.btnBottom,
            this.sepToolbar2,
            this.btnRemove,
            this.btnApplyAll});
            this.toolStrip.Location = new Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new Size(700, 25);
            this.toolStrip.TabIndex = 0;
            // 
            // btnAddVariable
            // 
            this.btnAddVariable.Image = ((Image)(resources.GetObject("btnAddVariable.Image")));
            this.btnAddVariable.Name = "btnAddVariable";
            this.btnAddVariable.Size = new Size(90, 22);
            this.btnAddVariable.Text = "Add variable";
            this.btnAddVariable.Click += new EventHandler(this.AddVariable_Click);
            // 
            // btnAddComment
            // 
            this.btnAddComment.Image = ((Image)(resources.GetObject("btnAddComment.Image")));
            this.btnAddComment.Name = "btnAddComment";
            this.btnAddComment.Size = new Size(90, 22);
            this.btnAddComment.Text = "Add comment";
            this.btnAddComment.Click += new EventHandler(this.AddComment_Click);
            // 
            // btnExpandEdit
            // 
            this.btnExpandEdit.Image = ((Image)(resources.GetObject("btnExpandEdit.Image")));
            this.btnExpandEdit.Name = "btnExpandEdit";
            this.btnExpandEdit.Size = new Size(90, 22);
            this.btnExpandEdit.Text = "Expand edit";
            this.btnExpandEdit.Click += new EventHandler(this.ExpandEdit_Click);
            // 
            // btnApply
            // 
            this.btnApply.Image = ((Image)(resources.GetObject("btnApply.Image")));
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new Size(90, 22);
            this.btnApply.Text = "Apply";
            this.btnApply.Click += new EventHandler(this.Apply_Click);
            // 
            // btnUp
            // 
            this.btnUp.DisplayStyle = ToolStripItemDisplayStyle.Image;
            this.btnUp.Image = ((Image)(resources.GetObject("btnUp.Image")));
            this.btnUp.Name = "btnUp";
            this.btnUp.Size = new Size(23, 22);
            this.btnUp.Text = "Up";
            this.btnUp.Click += new EventHandler(this.Up_Click);
            // 
            // btnDown
            // 
            this.btnDown.DisplayStyle = ToolStripItemDisplayStyle.Image;
            this.btnDown.Image = ((Image)(resources.GetObject("btnDown.Image")));
            this.btnDown.Name = "btnDown";
            this.btnDown.Size = new Size(23, 22);
            this.btnDown.Text = "Down";
            this.btnDown.Click += new EventHandler(this.Down_Click);
            // 
            // btnTop
            // 
            this.btnTop.DisplayStyle = ToolStripItemDisplayStyle.Image;
            this.btnTop.Image = ((Image)(resources.GetObject("btnTop.Image")));
            this.btnTop.Name = "btnTop";
            this.btnTop.Size = new Size(23, 22);
            this.btnTop.Text = "Top";
            this.btnTop.Click += new EventHandler(this.Top_Click);
            // 
            // btnBottom
            // 
            this.btnBottom.DisplayStyle = ToolStripItemDisplayStyle.Image;
            this.btnBottom.Image = ((Image)(resources.GetObject("btnBottom.Image")));
            this.btnBottom.Name = "btnBottom";
            this.btnBottom.Size = new Size(23, 22);
            this.btnBottom.Text = "Bottom";
            this.btnBottom.Click += new EventHandler(this.Bottom_Click);
            // 
            // btnRemove
            // 
            this.btnRemove.Image = ((Image)(resources.GetObject("btnRemove.Image")));
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new Size(90, 22);
            this.btnRemove.Text = "Remove";
            this.btnRemove.Click += new EventHandler(this.Remove_Click);
            // 
            // btnApplyAll
            // 
            this.btnApplyAll.Alignment = ToolStripItemAlignment.Right;
            this.btnApplyAll.Image = ((Image)(resources.GetObject("btnApplyAll.Image")));
            this.btnApplyAll.Name = "btnApplyAll";
            this.btnApplyAll.Size = new Size(90, 22);
            this.btnApplyAll.Text = "Apply all";
            this.btnApplyAll.Click += new EventHandler(this.ApplyAll_Click);
            // 
            // ctxApply
            // 
            this.ctxApply.Image = ((Image)(resources.GetObject("ctxApply.Image")));
            this.ctxApply.Name = "ctxApply";
            this.ctxApply.Size = new Size(180, 22);
            this.ctxApply.Text = "Apply";
            this.ctxApply.ShortcutKeys = ((Keys)(Keys.Control | Keys.Q));
            this.ctxApply.Click += new EventHandler(this.Apply_Click);
            // 
            // ctxApplyAll
            // 
            this.ctxApplyAll.Image = ((Image)(resources.GetObject("ctxApplyAll.Image")));
            this.ctxApplyAll.Name = "ctxApplyAll";
            this.ctxApplyAll.Size = new Size(180, 22);
            this.ctxApplyAll.Text = "Apply all";
            this.ctxApplyAll.Click += new EventHandler(this.ApplyAll_Click);
            // 
            // ctxAddVariable
            // 
            this.ctxAddVariable.Image = ((Image)(resources.GetObject("ctxAddVariable.Image")));
            this.ctxAddVariable.Name = "ctxAddVariable";
            this.ctxAddVariable.Size = new Size(180, 22);
            this.ctxAddVariable.Text = "Add variable";
            this.ctxAddVariable.Click += new EventHandler(this.AddVariable_Click);
            // 
            // ctxAddComment
            // 
            this.ctxAddComment.Image = ((Image)(resources.GetObject("ctxAddComment.Image")));
            this.ctxAddComment.Name = "ctxAddComment";
            this.ctxAddComment.Size = new Size(180, 22);
            this.ctxAddComment.Text = "Add comment";
            this.ctxAddComment.Click += new EventHandler(this.AddComment_Click);
            // 
            // ctxExpandEdit
            // 
            this.ctxExpandEdit.Image = ((Image)(resources.GetObject("ctxExpandEdit.Image")));
            this.ctxExpandEdit.Name = "ctxExpandEdit";
            this.ctxExpandEdit.Size = new Size(180, 22);
            this.ctxExpandEdit.Text = "Expand edit";
            this.ctxExpandEdit.Click += new EventHandler(this.ExpandEdit_Click);
            // 
            // ctxCopy
            // 
            this.ctxCopy.Image = ((Image)(resources.GetObject("ctxCopy.Image")));
            this.ctxCopy.Name = "ctxCopy";
            this.ctxCopy.Size = new Size(180, 22);
            this.ctxCopy.Text = "Copy";
            this.ctxCopy.ShortcutKeyDisplayString = "Ctrl+C";
            this.ctxCopy.Click += new EventHandler(this.Copy_Click);
            // 
            // ctxPaste
            // 
            this.ctxPaste.Image = ((Image)(resources.GetObject("ctxPaste.Image")));
            this.ctxPaste.Name = "ctxPaste";
            this.ctxPaste.Size = new Size(180, 22);
            this.ctxPaste.Text = "Paste";
            this.ctxPaste.ShortcutKeyDisplayString = "Ctrl+V";
            this.ctxPaste.Click += new EventHandler(this.Paste_Click);
            // 
            // ctxUp
            // 
            this.ctxUp.Image = ((Image)(resources.GetObject("ctxUp.Image")));
            this.ctxUp.Name = "ctxUp";
            this.ctxUp.Size = new Size(180, 22);
            this.ctxUp.Text = "Move up";
            this.ctxUp.Click += new EventHandler(this.Up_Click);
            // 
            // ctxDown
            // 
            this.ctxDown.Image = ((Image)(resources.GetObject("ctxDown.Image")));
            this.ctxDown.Name = "ctxDown";
            this.ctxDown.Size = new Size(180, 22);
            this.ctxDown.Text = "Move down";
            this.ctxDown.Click += new EventHandler(this.Down_Click);
            // 
            // ctxTop
            // 
            this.ctxTop.Image = ((Image)(resources.GetObject("ctxTop.Image")));
            this.ctxTop.Name = "ctxTop";
            this.ctxTop.Size = new Size(180, 22);
            this.ctxTop.Text = "Move to top";
            this.ctxTop.Click += new EventHandler(this.Top_Click);
            // 
            // ctxBottom
            // 
            this.ctxBottom.Image = ((Image)(resources.GetObject("ctxBottom.Image")));
            this.ctxBottom.Name = "ctxBottom";
            this.ctxBottom.Size = new Size(180, 22);
            this.ctxBottom.Text = "Move to bottom";
            this.ctxBottom.Click += new EventHandler(this.Bottom_Click);
            // 
            // ctxRemove
            // 
            this.ctxRemove.Image = ((Image)(resources.GetObject("ctxRemove.Image")));
            this.ctxRemove.Name = "ctxRemove";
            this.ctxRemove.Size = new Size(180, 22);
            this.ctxRemove.Text = "Remove";
            this.ctxRemove.Click += new EventHandler(this.Remove_Click);
            // 
            // sepToolbar1
            // 
            this.sepToolbar1.Name = "sepToolbar1";
            // 
            // sepToolbar2
            // 
            this.sepToolbar2.Name = "sepToolbar2";
            // 
            // sepContext1
            // 
            this.sepContext1.Name = "sepContext1";
            // 
            // sepContext2
            // 
            this.sepContext2.Name = "sepContext2";
            // 
            // sepContext3
            // 
            this.sepContext3.Name = "sepContext3";
            // 
            // dgvVariables
            // 
            this.dgvVariables.AllowUserToAddRows = false;
            this.dgvVariables.AllowUserToDeleteRows = false;
            this.dgvVariables.AllowUserToResizeRows = false;
            this.dgvVariables.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvVariables.ClipboardCopyMode = DataGridViewClipboardCopyMode.Disable;
            this.dgvVariables.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvVariables.Columns.AddRange(new DataGridViewColumn[] {
            this.colEnabled,
            this.colName,
            this.colValue,
            this.colLoadWhenActive});
            this.dgvVariables.ContextMenuStrip = this.ctxVariables;
            this.dgvVariables.Dock = DockStyle.Fill;
            this.dgvVariables.Location = new Point(0, 25);
            this.dgvVariables.Name = "dgvVariables";
            this.dgvVariables.RowHeadersVisible = false;
            this.dgvVariables.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dgvVariables.Size = new Size(700, 315);
            this.dgvVariables.TabIndex = 1;
            this.dgvVariables.CellDoubleClick += new DataGridViewCellEventHandler(this.dgvVariables_CellDoubleClick);
            this.dgvVariables.CellFormatting += new DataGridViewCellFormattingEventHandler(this.dgvVariables_CellFormatting);
            this.dgvVariables.CellValueChanged += new DataGridViewCellEventHandler(this.dgvVariables_CellValueChanged);
            this.dgvVariables.CurrentCellDirtyStateChanged += new EventHandler(this.dgvVariables_CurrentCellDirtyStateChanged);
            this.dgvVariables.SelectionChanged += new EventHandler(this.dgvVariables_SelectionChanged);
            this.dgvVariables.RowPostPaint += new DataGridViewRowPostPaintEventHandler(this.dgvVariables_RowPostPaint);
            this.dgvVariables.MouseDown += new MouseEventHandler(this.dgvVariables_MouseDown);
            this.dgvVariables.MouseUp += new MouseEventHandler(this.dgvVariables_MouseUp);
            this.dgvVariables.KeyDown += new KeyEventHandler(this.dgvVariables_KeyDown);
            // 
            // colEnabled
            // 
            this.colEnabled.HeaderText = "";
            this.colEnabled.Name = "colEnabled";
            this.colEnabled.SortMode = DataGridViewColumnSortMode.NotSortable;
            this.colEnabled.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            this.colEnabled.Width = 28;
            this.colEnabled.MinimumWidth = 28;
            // 
            // colName
            // 
            this.colName.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            this.colName.HeaderText = "Key";
            this.colName.Name = "colName";
            this.colName.SortMode = DataGridViewColumnSortMode.NotSortable;
            this.colName.MinimumWidth = 80;
            this.colName.FillWeight = 35F;
            // 
            // colValue
            // 
            this.colValue.HeaderText = "Value";
            this.colValue.Name = "colValue";
            this.colValue.SortMode = DataGridViewColumnSortMode.NotSortable;
            this.colValue.MinimumWidth = 80;
            this.colValue.FillWeight = 65F;
            // 
            // colLoadWhenActive
            // 
            this.colLoadWhenActive.HeaderText = "Activation policy";
            this.colLoadWhenActive.Name = "colLoadWhenActive";
            this.colLoadWhenActive.SortMode = DataGridViewColumnSortMode.NotSortable;
            this.colLoadWhenActive.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            this.colLoadWhenActive.Width = 110;
            this.colLoadWhenActive.MinimumWidth = 80;
            // 
            // ctxVariables
            // 
            this.ctxVariables.Items.AddRange(new ToolStripItem[] {
            this.ctxApply,
            this.ctxApplyAll,
            this.sepContext1,
            this.ctxAddVariable,
            this.ctxAddComment,
            this.ctxExpandEdit,
            this.ctxCopy,
            this.ctxPaste,
            this.sepContext2,
            this.ctxUp,
            this.ctxDown,
            this.ctxTop,
            this.ctxBottom,
            this.sepContext3,
            this.ctxRemove});
            this.ctxVariables.Name = "ctxVariables";
            this.ctxVariables.Opening += new CancelEventHandler(this.ctxVariables_Opening);
            // 
            // RegisteredVariableViewer
            // 
            this.AutoScaleDimensions = new SizeF(6F, 13F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Controls.Add(this.dgvVariables);
            this.Controls.Add(this.toolStrip);
            this.Name = "RegisteredVariableViewer";
            this.Size = new Size(700, 340);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            ((ISupportInitialize)(this.dgvVariables)).EndInit();
            this.ctxVariables.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private ToolStrip toolStrip;
        private ToolStripButton btnAddVariable;
        private ToolStripButton btnAddComment;
        private ToolStripButton btnExpandEdit;
        private ToolStripButton btnApply;
        private ToolStripButton btnUp;
        private ToolStripButton btnDown;
        private ToolStripButton btnTop;
        private ToolStripButton btnBottom;
        private ToolStripButton btnRemove;
        private ToolStripButton btnApplyAll;
        private ToolStripSeparator sepToolbar1;
        private ToolStripSeparator sepToolbar2;
        private CustomControls.DataGridViewEx dgvVariables;
        private DataGridViewCheckBoxColumn colEnabled;
        private DataGridViewTextBoxColumn colName;
        private DataGridViewTextBoxColumn colValue;
        private DataGridViewComboBoxColumn colLoadWhenActive;
        private ContextMenuStrip ctxVariables;
        private ToolStripMenuItem ctxApply;
        private ToolStripMenuItem ctxApplyAll;
        private ToolStripMenuItem ctxAddVariable;
        private ToolStripMenuItem ctxAddComment;
        private ToolStripMenuItem ctxExpandEdit;
        private ToolStripMenuItem ctxCopy;
        private ToolStripMenuItem ctxPaste;
        private ToolStripMenuItem ctxUp;
        private ToolStripMenuItem ctxDown;
        private ToolStripMenuItem ctxTop;
        private ToolStripMenuItem ctxBottom;
        private ToolStripMenuItem ctxRemove;
        private ToolStripSeparator sepContext1;
        private ToolStripSeparator sepContext2;
        private ToolStripSeparator sepContext3;
    }
}
