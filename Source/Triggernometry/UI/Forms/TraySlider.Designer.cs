using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Triggernometry.UI.Forms
{
    partial class TraySliderForm
    {
        private IContainer components = null;
        private Timer tmrShow;
        private Timer tmrFade;
        private Timer tmrSlideIn;
        private Timer tmrFadeOut;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                    components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new Container();
            this.tmrShow = new Timer(this.components);
            this.tmrFade = new Timer(this.components);
            this.tmrSlideIn = new Timer(this.components);
            this.tmrFadeOut = new Timer(this.components);
            this.tlpMain = new TableLayoutPanel();
            this.rtbText = new RichTextBox();
            this.lblTitle = new Label();
            this.tlpButtons = new TableLayoutPanel();
            this.Button3 = new Button();
            this.Button2 = new Button();
            this.Button1 = new Button();
            this.tlpMain.SuspendLayout();
            this.tlpButtons.SuspendLayout();
            this.SuspendLayout();
            // 
            // tmrShow
            // 
            this.tmrShow.Enabled = true;
            this.tmrShow.Interval = 250;
            this.tmrShow.Tick += new EventHandler(this.tmrShow_Tick);
            // 
            // tmrFade
            // 
            this.tmrFade.Interval = 15000;
            this.tmrFade.Tick += new EventHandler(this.tmrFade_Tick);
            // 
            // tmrSlideIn
            // 
            this.tmrSlideIn.Interval = 15;
            this.tmrSlideIn.Tick += new EventHandler(this.tmrSlideIn_Tick);
            // 
            // tmrFadeOut
            // 
            this.tmrFadeOut.Interval = 25;
            this.tmrFadeOut.Tick += new EventHandler(this.tmrFadeOut_Tick);
            // 
            // tlpMain
            // 
            this.tlpMain.AutoSize = true;
            this.tlpMain.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.tlpMain.BackColor = Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(245)))), ((int)(((byte)(250)))));
            this.tlpMain.ColumnCount = 1;
            this.tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.tlpMain.Controls.Add(this.rtbText, 0, 1);
            this.tlpMain.Controls.Add(this.lblTitle, 0, 0);
            this.tlpMain.Controls.Add(this.tlpButtons, 0, 2);
            this.tlpMain.Dock = DockStyle.Fill;
            this.tlpMain.ForeColor = Color.Black;
            this.tlpMain.Location = new Point(0, 0);
            this.tlpMain.Margin = new Padding(0);
            this.tlpMain.Name = "tlpMain";
            this.tlpMain.RowCount = 3;
            this.tlpMain.RowStyles.Add(new RowStyle());
            this.tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.tlpMain.RowStyles.Add(new RowStyle());
            this.tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            this.tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            this.tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            this.tlpMain.Size = new Size(320, 280);
            this.tlpMain.TabIndex = 0;
            // 
            // rtbText
            // 
            this.rtbText.BackColor = Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(245)))), ((int)(((byte)(250)))));
            this.rtbText.BorderStyle = BorderStyle.None;
            this.rtbText.DetectUrls = false;
            this.rtbText.Dock = DockStyle.Fill;
            this.rtbText.ForeColor = Color.Black;
            this.rtbText.Location = new Point(20, 41);
            this.rtbText.Margin = new Padding(20, 10, 20, 10);
            this.rtbText.Name = "rtbText";
            this.rtbText.ReadOnly = true;
            this.rtbText.ScrollBars = RichTextBoxScrollBars.Vertical;
            this.rtbText.Size = new Size(280, 188);
            this.rtbText.TabIndex = 0;
            this.rtbText.TabStop = false;
            this.rtbText.Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor in" +
    "cididunt ut labore et dolore magna aliqua.";
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.BackColor = Color.FromArgb(((int)(((byte)(135)))), ((int)(((byte)(180)))), ((int)(((byte)(225)))));
            this.lblTitle.Dock = DockStyle.Fill;
            this.lblTitle.ForeColor = Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(30)))), ((int)(((byte)(60)))));
            this.lblTitle.Location = new Point(3, 3);
            this.lblTitle.Margin = new Padding(3);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Padding = new Padding(3, 5, 3, 5);
            this.lblTitle.Size = new Size(314, 25);
            this.lblTitle.TabIndex = 1;
            this.lblTitle.Text = "Title";
            this.lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tlpButtons
            // 
            this.tlpButtons.AutoSize = true;
            this.tlpButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.tlpButtons.ColumnCount = 3;
            this.tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
            this.tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
            this.tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
            this.tlpButtons.Controls.Add(this.Button3, 2, 0);
            this.tlpButtons.Controls.Add(this.Button2, 1, 0);
            this.tlpButtons.Controls.Add(this.Button1, 0, 0);
            this.tlpButtons.Dock = DockStyle.Fill;
            this.tlpButtons.Location = new Point(3, 242);
            this.tlpButtons.Name = "tlpButtons";
            this.tlpButtons.RowCount = 1;
            this.tlpButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.tlpButtons.Size = new Size(314, 35);
            this.tlpButtons.TabIndex = 2;
            // 
            // Button3
            // 
            this.Button3.AutoSize = true;
            this.Button3.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.Button3.BackColor = Color.Gainsboro;
            this.Button3.Dock = DockStyle.Fill;
            this.Button3.FlatStyle = FlatStyle.Flat;
            this.Button3.ForeColor = Color.Black;
            this.Button3.Location = new Point(211, 3);
            this.Button3.Name = "Button3";
            this.Button3.Padding = new Padding(0, 1, 0, 1);
            this.Button3.Size = new Size(100, 29);
            this.Button3.TabIndex = 0;
            this.Button3.TabStop = false;
            this.Button3.Text = "Button3";
            this.Button3.UseVisualStyleBackColor = false;
            this.Button3.Click += new EventHandler(this.Button_Click);
            // 
            // Button2
            // 
            this.Button2.AutoSize = true;
            this.Button2.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.Button2.BackColor = Color.MistyRose;
            this.Button2.Dock = DockStyle.Fill;
            this.Button2.FlatStyle = FlatStyle.Flat;
            this.Button2.ForeColor = Color.Black;
            this.Button2.Location = new Point(107, 3);
            this.Button2.Name = "Button2";
            this.Button2.Padding = new Padding(0, 1, 0, 1);
            this.Button2.Size = new Size(98, 29);
            this.Button2.TabIndex = 1;
            this.Button2.TabStop = false;
            this.Button2.Text = "Button2";
            this.Button2.UseVisualStyleBackColor = false;
            this.Button2.Click += new EventHandler(this.Button_Click);
            // 
            // Button1
            // 
            this.Button1.AutoSize = true;
            this.Button1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.Button1.BackColor = Color.PaleTurquoise;
            this.Button1.Dock = DockStyle.Fill;
            this.Button1.FlatStyle = FlatStyle.Flat;
            this.Button1.ForeColor = Color.Black;
            this.Button1.Location = new Point(3, 3);
            this.Button1.Name = "Button1";
            this.Button1.Padding = new Padding(0, 1, 0, 1);
            this.Button1.Size = new Size(98, 29);
            this.Button1.TabIndex = 2;
            this.Button1.TabStop = false;
            this.Button1.Text = "Button1";
            this.Button1.UseVisualStyleBackColor = false;
            this.Button1.Click += new EventHandler(this.Button_Click);
            // 
            // TraySliderForm
            // 
            this.AutoScaleMode = AutoScaleMode.None;
            this.BackColor = Color.White;
            this.ClientSize = new Size(320, 280);
            this.ControlBox = false;
            this.Controls.Add(this.tlpMain);
            this.DoubleBuffered = true;
            this.ForeColor = Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.FormBorderStyle = FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "TraySliderForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = SizeGripStyle.Hide;
            this.StartPosition = FormStartPosition.Manual;
            this.Text = "TraySlider";
            this.FormClosing += new FormClosingEventHandler(this.TraySlider_FormClosing);
            this.MouseEnter += new EventHandler(this.TraySlider_MouseEnter);
            this.MouseLeave += new EventHandler(this.TraySlider_MouseLeave);
            this.tlpMain.ResumeLayout(false);
            this.tlpMain.PerformLayout();
            this.tlpButtons.ResumeLayout(false);
            this.tlpButtons.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private TableLayoutPanel tlpMain;
        private TableLayoutPanel tlpButtons;
        public Button Button1;
        public Button Button3;
        public Button Button2;
        private RichTextBox rtbText;
        public Label lblTitle;
    }
}