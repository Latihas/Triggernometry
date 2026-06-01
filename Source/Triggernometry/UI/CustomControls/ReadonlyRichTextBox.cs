using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Triggernometry.Expressions.String.Utils;
using Triggernometry.Localization;

namespace Triggernometry.UI.CustomControls;

public class RichTextBoxHelper : ReadonlyRichTextBox // to do: change to CustomControls
{
	private readonly Form ParentForm;
	private readonly TableLayoutPanel ParentTable;

	internal static bool _Expanded; // all textboxes share the same state
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool Expanded {
		get => _Expanded;
		set {
			_Expanded = value;
			UpdateText();
		}
	}

	/// <summary> Append the RichTextBoxHelper to the end of the given TableLayoutPanel in the given form.</summary>
	public RichTextBoxHelper(string name, Form parentForm, TableLayoutPanel table, int colIndex = 1) {
		Name = name;
		Tag = I18n.DoNotTranslate;
		Dock = DockStyle.Left;
		Margin = new Padding(3, 15, 15, 7);
		ScrollBars = RichTextBoxScrollBars.Vertical;

		ParentForm = parentForm;
		ParentTable = table;
		// append to the last row and span to the last grid of the row
		ParentTable.RowCount += 1;
		ParentTable.RowStyles.Add(new RowStyle());
		ParentTable.Controls.Add(this, colIndex, ParentTable.RowCount - 1);
		if (ParentTable.ColumnCount - colIndex > 1) {
			table.SetColumnSpan(this, ParentTable.ColumnCount - colIndex);
		}

		Cursor = Cursors.Help;
		Expanded = _Expanded;
	}

	internal void UpdateText() {
		Lock();
		if (_Expanded) {
			Dock = DockStyle.Fill;
			// var key = "";
			// switch (Name)
			// {
			//     case "rtbVariableHelper":
			//         key = "rtbHelperVar" + Enum.GetName(typeof(VariableOpEnum), ((ActionForm)ParentForm).cbxVariableOp.SelectedIndex); break;
			//     case "rtbLvarHelper":
			//         key = "rtbHelperLvar" + Enum.GetName(typeof(ListVariableOpEnum), ((ActionForm)ParentForm).cbxLvarOperation.SelectedIndex); break;
			//     case "rtbTvarHelper":
			//         key = "rtbHelperTvar" + Enum.GetName(typeof(TableVariableOpEnum), ((ActionForm)ParentForm).cbxTvarOpType.SelectedIndex); break;
			//     case "rtbDictHelper":
			//         key = "rtbHelperDict" + Enum.GetName(typeof(DictVariableOpEnum), ((ActionForm)ParentForm).cbxDictOpType.SelectedIndex); break;
			//     case "rtbSendKeysHelper":
			//         key = "rtbHelperSendKeys" + Enum.GetName(typeof(KeypressTypeEnum), ((ActionForm)ParentForm).cbxKeypressMethod.SelectedIndex); break;
			//     case "rtbCallbackHelper": key = "rtbHelperCallback"; break;
			//     case "rtbWmsgHelper": key = "rtbHelperWmsg"; break;
			//     case "rtbJsonHelper": key = "rtbHelperJson"; break;
			// }
			// var resources = new System.ComponentModel.ComponentResourceManager(typeof(ActionForm));
			// Text = I18n.Translate($"ActionForm/{key}", resources.GetString($"{key}.Text") ?? "");
		} else {
			Dock = DockStyle.Left;
			Text = I18n.Translate("ActionForm/rtbHelper", "[Show Help]");
		}
		SetStyles();
		SetHeight();
		Unlock();
	}

	private const int WM_SETCURSOR = 0x0020;
	private const int WM_LBUTTONUP = 0x202;

	[DllImport("user32.dll")]
	public static extern IntPtr SetCursor(IntPtr hCursor);

	protected override void WndProc(ref Message m) {
		if (m.Msg == WM_LBUTTONUP) {
			Expanded = !Expanded;
		} else if (m.Msg == WM_SETCURSOR) {
			SetCursor(Cursors.Help.Handle);
		} else {
			base.WndProc(ref m);
		}
	}

	private void SetHeight() {
		int residueHeight;
		if (ParentTable.Parent is TabPage tabPage) {
			var rtbLocation = ParentTable.PointToScreen(Location);
			var tabControlBottom = tabPage.PointToScreen(new Point(0, tabPage.Height)).Y;
			residueHeight = tabControlBottom - rtbLocation.Y - 10;
		} else {
			residueHeight = 200;
		}

		var totalLines = GetLineFromCharIndex(TextLength) + 1;
		var lineHeight = (int)(Font.Height * 1.5);
		var textHeight = totalLines * lineHeight;

		Height = Math.Min(residueHeight, textHeight);
	}

	internal static readonly Color[] expressionColors = [
		Color.FromArgb(0, 85, 221), Color.FromArgb(140, 0, 0)
	];
	internal static Color monospaceColor = Color.FromArgb(32, 64, 144);
	internal static Color stringColor = Color.FromArgb(34, 153, 0);
	internal static Color separatorColor = Color.FromArgb(204, 102, 0);

	public void SetStyles() {
		// this is currently a very rough implementation for setting colors for formatted text
		SelectAll();
		if (!_Expanded) {
			SelectionColor = Color.FromArgb(160, 160, 160);
		} else {
			// initial color
			SelectionColor = Color.Black;

			// set color for ${...}
			var depth = 0;
			var totalLength = Text.Length;
			for (var i = 0; i < totalLength; i++) {
				if (i < totalLength - 1 && Text.Substring(i, 2) == "${") {
					depth++;
				}
				if (depth > 0) {
					Select(i, 1);
					SelectionColor = expressionColors[(depth - 1) % expressionColors.Count()];
					if (Text[i] == '}') {
						depth--;
					}
				}
			}

			// set color and style for `...`
			var monospaceFont = new Font("Consolas", Font.Size);
			var start = 0;
			while (start < Text.Length) {
				var openIndex = Find("`", start, RichTextBoxFinds.None);
				if (openIndex == -1) break;

				var closeIndex = Find("`", openIndex + 1, RichTextBoxFinds.None);
				if (closeIndex == -1) break;

				Select(openIndex, closeIndex - openIndex + 1);
				SelectionFont = monospaceFont;

				for (var i = openIndex; i <= closeIndex; i++) {
					Select(i, 1);
					if (SelectionColor == Color.Black) {
						SelectionColor = monospaceColor;
					}
				}
				Select(closeIndex, 1);
				SelectedText = "";
				Select(openIndex, 1);
				SelectedText = "";
				start = closeIndex - 1;
			}

			// set color for '...'
			start = 0;
			while (start < Text.Length) {
				var openIndex = Find("'", start, RichTextBoxFinds.None);
				if (openIndex == -1) break;

				var closeIndex = Find("'", openIndex + 1, RichTextBoxFinds.None);
				if (closeIndex == -1) break;

				Select(openIndex, closeIndex - openIndex + 1);
				if (!SelectedText.Contains('$')) {
					SelectionColor = stringColor;
				}
				Select(openIndex, 1);
				SelectionColor = stringColor;
				Select(closeIndex, 1);
				SelectionColor = stringColor;
				start = closeIndex + 1;
			}

			// set color for "..."
			start = 0;
			while (start < Text.Length) {
				var openIndex = Find("\"", start, RichTextBoxFinds.None);
				if (openIndex == -1) break;

				var closeIndex = Find("\"", openIndex + 1, RichTextBoxFinds.None);
				if (closeIndex == -1) break;

				Select(openIndex, closeIndex - openIndex + 1);
				if (!SelectedText.Contains('$')) {
					SelectionColor = stringColor;
				}
				Select(openIndex, 1);
				SelectionColor = stringColor;
				Select(closeIndex, 1);
				SelectionColor = stringColor;
				start = closeIndex + 1;
			}

			// set color for separators
			for (start = 0; start < Text.Length; start++) {
				Select(start, 1);
				var ch = Text[start];
				if (SelectionColor == Color.Black) {
					continue;
				} // plain text
				if (ch == ',' && SelectionColor == stringColor) {
					continue;
				}
				if (ch == ',' || ch == '|' || ch == '=' || ch == ';' || ch == ':' || ch == ParserCommon.LINEBREAK) {
					SelectionColor = separatorColor;
				}
			}
		}
		Select(0, 0);
	}
}

public class ReadonlyRichTextBox : RichTextBox {
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool AllowWheelThrough { get; set; } = false;

	public ReadonlyRichTextBox() {
		ReadOnly = true;
		TabStop = false;
		BorderStyle = BorderStyle.None;

		if (I18n.CurrentLanguage != null) {
			if (I18n.CurrentLanguage.LanguageName.Contains("CN")) // will switch to a more general implementation
			{
				Font = new Font("Microsoft YaHei", Font.Size);
			}
		}
		DoubleBuffered = true;
	}

	private const int WM_SETFOCUS = 0x7;
	private const int WM_SETREDRAW = 0x000B;
	private const int WM_SETCURSOR = 0x0020;
	private const int WM_MOUSEMOVE = 0x0200;
	private const int WM_LBUTTONDOWN = 0x201;
	private const int WM_LBUTTONUP = 0x202;
	private const int WM_LBUTTONDBLCLK = 0x203;
	private const int WM_RBUTTONDOWN = 0x204;
	private const int WM_RBUTTONUP = 0x205;
	private const int WM_RBUTTONDBLCLK = 0x206;
	private const int WM_KEYDOWN = 0x0100;
	private const int WM_KEYUP = 0x0101;
	private const int WM_MOUSEWHEEL = 0x020A;

	[DllImport("user32.dll")]
	private static extern bool HideCaret(IntPtr hWnd);

	protected override void WndProc(ref Message m) {
		HideCaret(Handle);
		if (m.Msg == WM_SETFOCUS || m.Msg == WM_KEYDOWN || m.Msg == WM_KEYUP ||
		    m.Msg == WM_LBUTTONDOWN || m.Msg == WM_LBUTTONUP || m.Msg == WM_MOUSEMOVE || m.Msg == WM_LBUTTONDBLCLK ||
		    m.Msg == WM_RBUTTONDOWN || m.Msg == WM_RBUTTONUP || m.Msg == WM_RBUTTONDBLCLK ||
		    m.Msg == WM_SETCURSOR) {
		} else if (m.Msg == WM_MOUSEWHEEL && AllowWheelThrough) {
			SendMessage(Parent.Handle, m.Msg, m.WParam, m.LParam);
		} else {
			base.WndProc(ref m);
		}
	}

	[DllImport("user32.dll")]
	private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

	public void Lock() {
		SendMessage(Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
		ReadOnly = false;
	}

	public void Unlock() {
		SendMessage(Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
		ReadOnly = true;
		Refresh();
	}

	protected override void OnParentChanged(EventArgs e) {
		base.OnParentChanged(e);
		if (Parent != null) {
			BackColor = Parent.BackColor;
		}
	}
}