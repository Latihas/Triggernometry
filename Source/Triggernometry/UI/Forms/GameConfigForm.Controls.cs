using System.Drawing;
using System.Windows.Forms;

namespace Triggernometry.UI.Forms
{
    public partial class GameConfigForm
    {
        private class MyGroupBox : GroupBox
        {
	        public MyGroupBox(string text) {
                Dock = DockStyle.Top;
                AutoSize = true;
                AutoSizeMode = AutoSizeMode.GrowAndShrink;
                Margin = new Padding(20);
                Text = text;
            }
        }

        private class MyCheckBox : CheckBox
        {
	        public MyCheckBox() {
                AutoSize = true;
                Dock = DockStyle.Fill;
                Margin = new Padding(10);
            }
        }

        private class MyTextBox : TextBox
        {
	        public MyTextBox() {
                AutoSize = true;
                Dock = DockStyle.Fill;
                Margin = new Padding(10);
            }
        }

        private class MyComboBox : ComboBox
        {
	        public MyComboBox() {
                AutoSize = true;
                Dock = DockStyle.Fill;
                Margin = new Padding(10);
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == 0x020A)  // WM_MOUSEWHEEL
                {
                    return;  // No-scroll
                }
                base.WndProc(ref m);
            }
        }

        private class MyNumericUpDown : NumericUpDown
        {
	        public MyNumericUpDown() {
                AutoSize = true;
                Dock = DockStyle.Fill;
                Margin = new Padding(10);
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == 0x020A)  // WM_MOUSEWHEEL
                {
                    return;  // No-scroll
                }
                base.WndProc(ref m);
            }
        }

        private class MyLabel : Label
        {
	        public MyLabel() {
                AutoSize = true;
                Dock = DockStyle.Fill;
                Margin = new Padding(10);
            }
        }

        private class MyButton : Button
        {
	        public MyButton() {
                Anchor = AnchorStyles.None;
                AutoSize = true;
                Margin = new Padding(10);
                Padding = new Padding(5);
            }
        }

        private class SeperatorPanel : Panel
        {
	        public SeperatorPanel() {
                Height = 2;
                BackColor = Color.DarkGray;
                Dock = DockStyle.Fill;
                AutoSize = true;
                Margin = new Padding(10);
            }
        }

        private class BackgroundPanel : Panel
        {
	        public BackgroundPanel() {
                AutoSize = true;
                AutoSizeMode = AutoSizeMode.GrowAndShrink;
                Dock = DockStyle.Fill;
                AutoScroll = true;
            }

            protected override Point ScrollToControl(Control activeControl) =>
	            // 防止自动滚动，使页面突然跳转到窗口范围外的 txtbox 等
	            DisplayRectangle.Location;
        }

        private class GroupPanel : Panel
        {
	        public GroupPanel() {
                AutoSize = true;
                AutoSizeMode = AutoSizeMode.GrowAndShrink;
                Dock = DockStyle.Top;
                Padding = new Padding(20, 20, 20, 0);
            }
        }

        public class OptionsTableLayoutPanel : TableLayoutPanel
        {
	        public OptionsTableLayoutPanel() {
                AutoSize = true;
                AutoSizeMode = AutoSizeMode.GrowAndShrink;
                Dock = DockStyle.Fill;
                RowCount = 0;
                ColumnCount = 2;
                ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
                ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            }
        }

        private class BottomTableLayoutPanel : TableLayoutPanel
        {
	        public BottomTableLayoutPanel() {
                Dock = DockStyle.Bottom;
                ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            }
        }

        private class MyToolTip : ToolTip
        {
	        public MyToolTip() {
                InitialDelay = 500;
                AutoPopDelay = 60000;
                ReshowDelay = 100;
                ShowAlways = true;
                IsBalloon = true;
                ToolTipIcon = ToolTipIcon.Info;
                ToolTipTitle = "提示";
            }
        }
    }
}