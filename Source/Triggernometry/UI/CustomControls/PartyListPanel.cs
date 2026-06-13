using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Triggernometry.Core;
using Triggernometry.Core.Variables;
using Triggernometry.FFXIV;
using Triggernometry.PluginBridges;

namespace Triggernometry.UI.CustomControls;

public class PartyListPanel : TableLayoutPanel {
	private static readonly string[] jobOrder = [
		"WAR", "MRD", "PLD", "GLA", "DRK", "GNB",
		"WHM", "CNJ", "AST", "SGE", "SCH",
		"SAM", "MNK", "PGL", "DRG", "LNC", "NIN", "ROG", "RPR", "VPR",
		"BRD", "ARC", "MCH", "DNC", "BLM", "THM", "PCT", "RDM", "SMN", "ACN", "BLU"
	];
	public readonly int PlayerCount;
	public readonly string[] PlayerDescriptions;
	public readonly string PlayerIdsLvarName;
	public readonly string PlayerIdxVarName;

	public readonly string PlayerNamesLvarName;
	private List<PlayerLabel> _players = [];

	public PartyListPanel(
		string[] playerDescriptions,
		string playerNamesLvarName = "pname",
		string playerIdsLvarName = "party",
		string PplayerIdxVarName = "myIdx") {
		SuspendLayout();

		PlayerNamesLvarName = playerNamesLvarName;
		PlayerIdsLvarName = playerIdsLvarName;
		PlayerIdxVarName = PplayerIdxVarName;

		if ((playerDescriptions?.Length ?? 0) == 0)
			playerDescriptions = ["[Undefined]"];
		PlayerCount = playerDescriptions.Length;
		PlayerDescriptions = playerDescriptions;

		Dock = DockStyle.Fill;
		AutoSize = true;
		AutoSizeMode = AutoSizeMode.GrowAndShrink;

		// Set DragDrop Events
		DragEnter += PartyListPanel_DragEnter;
		DragDrop += PartyListPanel_DragDrop;
		AllowDrop = true;

		// Deternime Row and Column count based on PlayerCount
		var rowCount = PlayerCount <= 4 ? 1 : 2;
		var colCount = Math.Min(PlayerCount, 4);
		for (var i = 0; i < rowCount; i++) {
			RowStyles.Add(new RowStyle(SizeType.Percent, 100F / rowCount));
		}
		for (var i = 0; i < colCount; i++) {
			ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / colCount));
		}

		// Read Current Entities and Create PlayerLabels
		var entities = GetSortedPartyMembers();
		for (var i = 0; i < PlayerCount; i++) {
			var player = new PlayerLabel(this, entities[i], i);
			_players.Add(player);
		}

		// Adjust Label Size after the correct font is applied
		_players[0].HandleCreated += (_, _) => AdjustLabelSizes();
		ResumeLayout(true);
	}

	private void AdjustLabelSizes() {
		double lblWidth = 50;
		double lblHeight = 30;
		using (var g = CreateGraphics()) {
			var font = _players[0].Font;
			foreach (var label in _players) {
				var size = g.MeasureString(label.Text, font);
				lblWidth = Math.Max(lblWidth, size.Width);
				lblHeight = Math.Max(lblHeight, size.Height);
			}
		}
		lblWidth += 20;
		lblHeight += 20;
		foreach (var label in _players) {
			label.Width = (int)lblWidth;
			label.Height = (int)lblHeight;
		}
	}

	private List<Entity?> GetSortedPartyMembers() {
		var entities = Entity.GetEntities()
			.Where(e => e.HexID.StartsWith("10")) // is player
			.OrderByDescending(e => e.InParty) // is party member
			.ThenBy(e => e.Job.SubRole == Job.RoleType.None ? 99 : (int)e.Job.SubRole) // sort by subrole id
			.ThenBy(e => Array.IndexOf(jobOrder, e.Job.NameEN3)) // customized job order
			.ThenBy(e => e.Job.JobID) // unknown jobs: sort by job id
			.ThenBy(e => e.Name)
			.Take(PlayerCount)
			.Cast<Entity?>()
			.ToList();

		while (entities.Count < PlayerCount) {
			entities.Add(null);
		}

		if (entities.Count == 8 // Double Caster => D2 / D4
		    && entities[5]?.Job.SubRole == Job.RoleType.PhysicalRanged
		    && entities[6]?.Job.SubRole == Job.RoleType.MagicalRanged
		    && entities[7]?.Job.SubRole == Job.RoleType.MagicalRanged) {
			(entities[5], entities[6]) = (entities[6], entities[5]);
			if (entities[7]?.Job.NameEN3 == "BLM") // with BLM: BLM D2
				(entities[5], entities[7]) = (entities[7], entities[5]);
		}
		return entities;
	}

	private void PartyListPanel_DragEnter(object? sender, DragEventArgs e) {
		if (e.Data.GetDataPresent(typeof(PlayerLabel))) {
			e.Effect = DragDropEffects.Move;
		}
	}

	/// <summary> Get dragged label and target label, then update Order.</summary>
	private void PartyListPanel_DragDrop(object? sender, DragEventArgs e) {
		var draggedLabel = (PlayerLabel)e.Data.GetData(typeof(PlayerLabel));
		var clientPoint = PointToClient(new Point(e.X, e.Y));
		var control = GetChildAtPoint(clientPoint);
		if (control is PlayerLabel targetLabel && draggedLabel != targetLabel) {
			Parent?.SuspendLayout();
			SwapLabels(draggedLabel, targetLabel);
			Parent?.ResumeLayout(false);
		}
	}

	private void SwapLabels(PlayerLabel draggedLabel, PlayerLabel targetLabel) {
		(draggedLabel.Order, targetLabel.Order) = (targetLabel.Order, draggedLabel.Order);
	}

	public void LoadFromConfig() {
		if (!RealPlugin.Instance.GetVariableStore(false).List.TryGetValue(PlayerIdsLvarName, out var savedList) || savedList.Size != PlayerCount)
			return;

		List<string> storedPlayerIDs = savedList.Values.Select(var => var.ToString()).ToList();

		var indices = new List<int>();
		foreach (var playerLabel in _players) {
			var index = storedPlayerIDs.IndexOf(playerLabel.HexID);
			if (index >= 0 && index < PlayerCount) {
				indices.Add(index);
			} else return;
		}
		var expectedIndices = new HashSet<int>(Enumerable.Range(0, PlayerCount));
		if (new HashSet<int>(indices).SetEquals(expectedIndices)) {
			for (var i = 0; i < PlayerCount; i++) {
				_players[i].Order = indices[i];
			}
		}
	}

	public void SaveToConfig() {
		if (_players.Count <= 1) return;

		_players = _players.OrderBy(p => p.Order).ToList();

		var hexIDList = new VariableList();
		var nameList = new VariableList();
		var hexIDDict = new VariableDictionary();
		var nameDict = new VariableDictionary();
		var changer = "PartyList";

		foreach (var label in _players) {
			Variable hexID = new VariableScalar(label.HexID);
			Variable name = new VariableScalar(label.PlayerName);
			var description = PlayerDescriptions[label.Order];

			hexIDList.Push(hexID, changer);
			nameList.Push(name, changer);
			hexIDDict.SetValue(description, hexID, changer);
			nameDict.SetValue(description, name, changer);

			if (BridgeFFXIV.PlayerHexId == label.HexID) // var:myIdx
			{
				var idx = label.Order + 1;
				RealPlugin.Instance.GetVariableStore(false).Scalar[PlayerIdxVarName] = new VariableScalar(idx);
			}
		}
		RealPlugin.Instance.GetVariableStore(false).List[PlayerIdsLvarName] = hexIDList;
		RealPlugin.Instance.GetVariableStore(false).List[PlayerNamesLvarName] = nameList;
	}

	public class PlayerLabel : Label {
		public readonly string? HexID;
		public readonly string? JobName;
		public readonly PartyListPanel ParentTable;
		public readonly string? PlayerName;
		private Label _draggingClone;
		public Job.RoleType? SubRole;

		public PlayerLabel(PartyListPanel parent, Entity? entity, int order) {
			ParentTable = parent;
			if (entity != null) {
				PlayerName = entity.Name;
				SubRole = entity.Job.SubRole;
				JobName = CultureInfo.CurrentCulture.Name.StartsWith("zh-")
					? entity.Job.NameCN2
					: entity.Job.NameEN3;
				HexID = entity.HexID;
			}
			Order = order;
			ForeColor = GetForeColorByRole();
			Margin = new Padding(10);
			AutoSize = false;
			Anchor = AnchorStyles.None;
			TextAlign = ContentAlignment.MiddleCenter;
			Cursor = Cursors.SizeAll;
			MouseDown += PlayerLabel_MouseDown;
			MouseMove += PlayerLabel_MouseMove;
			MouseUp += PlayerLabel_MouseUp;
		}

		/// <summary> Start from 0. </summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int Order {
			get;
			set {
				field = value;
				Text = $"[{ParentTable.PlayerDescriptions[field]}] {JobName ?? ""}\n" + PlayerName?.Replace(" ", "\n");
				RefreshLocation();
			}
		}

		private Color GetForeColorByRole() {
			switch (SubRole & Job.RoleType.MainRole) {
				case Job.RoleType.Tank: return Color.FromArgb(16, 72, 144);
				case Job.RoleType.Healer: return Color.FromArgb(16, 144, 72);
				case Job.RoleType.DPS:
					switch (SubRole) {
						case Job.RoleType.StrengthMelee:
						case Job.RoleType.DexterityMelee: return Color.FromArgb(160, 64, 0);
						case Job.RoleType.PhysicalRanged: return Color.FromArgb(160, 0, 0);
						case Job.RoleType.MagicalRanged: return Color.FromArgb(160, 0, 96);
						default: return Color.FromArgb(128, 128, 128);
					}
				default: return Color.FromArgb(128, 128, 128);
			}
		}

		/// <summary> Set the label to the correct position in the parent table according to Order.  </summary>
		public void RefreshLocation() {
			var colCount = Math.Min(ParentTable.PlayerCount, 4);
			var row = Order / colCount;
			var col = Order % colCount;
			ParentTable.Controls.Add(this, col, row);
		}

		private void PlayerLabel_MouseDown(object? sender, MouseEventArgs e) {
			if (e.Button == MouseButtons.Left) {
				/*  To-Do
				_draggingClone = new Label
				{
				    Text = this.Text,
				    Size = this.Size,
				    BackColor = Color.FromArgb(128, this.BackColor),
				    ForeColor = Color.FromArgb(128, this.ForeColor),
				    Font = this.Font,
				    TextAlign = this.TextAlign,
				};
				ParentTable.Parent.Controls.Add(_draggingClone);
				_draggingClone.BringToFront();
				_draggingClone.Location = this.Location;
				*/
				DoDragDrop(this, DragDropEffects.Move);
			}
		}

		private void PlayerLabel_MouseMove(object? sender, MouseEventArgs e) {
			if (e.Button == MouseButtons.Left && _draggingClone != null) {
				var newLocation = ParentTable.PointToClient(Cursor.Position);
				newLocation.Offset(-_draggingClone.Width / 2, -_draggingClone.Height / 2);
				_draggingClone.Location = newLocation;
			}
		}

		private void PlayerLabel_MouseUp(object? sender, MouseEventArgs e) {
			if (_draggingClone != null) {
				ParentTable.Controls.Remove(_draggingClone);
				_draggingClone.Dispose();
				_draggingClone = null;
			}
		}
	}
}