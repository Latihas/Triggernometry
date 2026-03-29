using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Triggernometry.UI.Forms;

public partial class AutoCompleteForm : Form {
	protected override bool ShowWithoutActivation => true;

	public AutoCompleteForm() {
		InitializeComponent();
		DoubleBuffered = true;
	}


	public string GetChosenAutocomplete() {
		if (listBox1.SelectedItems.Count == 1) {
			return listBox1.SelectedItem.ToString();
		}
		return "";
	}

	public void NextAutocompleteItem() {
		var i = listBox1.SelectedIndex + 1;
		if (i > listBox1.Items.Count - 1) {
			i = 0;
		}
		listBox1.SelectedIndex = i;
	}

	public void PreviousAutocompleteItem() {
		var i = listBox1.SelectedIndex - 1;
		if (i < 0) {
			i = listBox1.Items.Count - 1;
		}
		listBox1.SelectedIndex = i;
	}

	public void NextAutocompletePage() {
		var i = listBox1.SelectedIndex + 5;
		if (i > listBox1.Items.Count - 1) {
			i = listBox1.Items.Count - 1;
		}
		listBox1.SelectedIndex = i;
	}

	public void PreviousAutocompletePage() {
		var i = listBox1.SelectedIndex - 5;
		if (i < 0) {
			i = 0;
		}
		listBox1.SelectedIndex = i;
	}

	public void BuildList(IEnumerable<string> strs) {
		SuspendLayout();
		var hadold = false;
		var str = "";
		var calcheight = 1 + Math.Min(10, strs.Count());
		calcheight *= listBox1.ItemHeight;
		var longeststr = 0.0f;
		using (var g = CreateGraphics()) {
			foreach (var st in strs) {
				longeststr = Math.Max(longeststr, g.MeasureString(st, listBox1.Font).Width);
			}
		}
		var strw = (int)Math.Ceiling(longeststr) + 20;
		listBox1.Width = strw;
		listBox1.Height = calcheight;
		if (listBox1.Items.Count > 0 && listBox1.SelectedItem != null) {
			str = listBox1.SelectedItem.ToString();
			hadold = true;
		}
		listBox1.Items.Clear();
		listBox1.Items.AddRange(strs.ToArray());
		if (hadold) {
			var i = 0;
			foreach (var s in strs) {
				if (string.Compare(s, str, true) == 0) {
					break;
				}
				i++;
			}
			if (i > strs.Count() - 1) {
				i = 0;
			}
			listBox1.SelectedIndex = i;
		} else {
			listBox1.SelectedIndex = 0;
		}
		ResumeLayout();
	}

	protected override void WndProc(ref Message m) {
		if (m.Msg == 0x84) {
			m.Result = -1;
		} else {
			base.WndProc(ref m);
		}
	}
}