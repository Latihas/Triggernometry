using System.Windows.Forms;

namespace Triggernometry.UI.CustomControls;

public partial class DataGridViewEx : DataGridView {
	private DataGridViewCell CellEdited { get; set; }

	public DataGridViewEx() {
		InitializeComponent();
		DoubleBuffered = true;
		CellBeginEdit += DataGridViewEx_CellBeginEdit;
		CellEndEdit += DataGridViewEx_CellEndEdit;
	}

	private void DataGridViewEx_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e) {
		CellEdited = Rows[e.RowIndex].Cells[e.ColumnIndex];
	}

	private void DataGridViewEx_CellEndEdit(object? sender, DataGridViewCellEventArgs e) {
		CellEdited = null;
	}

	protected override bool ProcessDialogKey(Keys keyData) {
		if (keyData == Keys.Enter) {
			EndEdit();
			ClearSelection();
			return true;
		}
		return base.ProcessDialogKey(keyData);
	}
}