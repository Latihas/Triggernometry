using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Triggernometry.Core;
using Triggernometry.Core.Variables;
using Triggernometry.Expressions.Maths;
using Triggernometry.UI.CustomControls;

namespace Triggernometry.UI.Forms;

public partial class GameConfigForm : Form {
	public readonly ConfigInfo Info;

	/// <summary> 储存表单中所有 Option 控件的列表。 </summary>
	private readonly List<Option> _options = [];
	/// <summary> （可选）表单绑定的小队列表控件。 </summary>
	private PartyListPanel _partyListPanel;
	/// <summary> 表单下方用于放置按钮等控件的 TableLayoutPanel。 </summary>
	private readonly TableLayoutPanel bottomPanel = new BottomTableLayoutPanel {
		RowCount = 1,
		ColumnCount = 1
	};

	public Button btnSave = new MyButton {
		Text = "保存配置"
	};
	/// <summary> 用于储存用户配置的触发器字典变量。 </summary>
	public VariableDictionary Config = new();

	/// <summary> 表单上方用于放置所有选项组的 Panel，可滚动。 </summary>
	private readonly Panel mainPanel = new BackgroundPanel();
	public Font UserFont = new("微软雅黑", 10);

	public GameConfigForm(ConfigInfo info) {
		// suspend until run
		SuspendLayout();
		Info = info;
		// load config
		Config = RealPlugin.Instance.GetVariableStore(true).Dict.TryGetValue(Info.ConfigName, out var cfg)
			? (VariableDictionary)cfg.Duplicate()
			: new VariableDictionary();

		// basic props
		Text = Info.Description;
		Font = UserFont;
		StartPosition = FormStartPosition.CenterScreen;
		var width = TextRenderer.MeasureText("AAAA", UserFont).Width * 16;
		MinimumSize = new Size(width, width); // To-do：autoadjust by minimum height
		Controls.Add(mainPanel);
		Controls.Add(bottomPanel);
		bottomPanel.Controls.Add(btnSave);
		bottomPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		// events
		Shown += (_, _) => {
			_options.ForEach(o => o.InitializeData());
			mainPanel.AutoScrollPosition = new Point(0, 0);
			TopMost = true;
			BringToFront();
			Activate();
			TopMost = false;
		};
		btnSave.Click += btnSave_Click;
	}

	/// <summary>
	///     在表单上方的 mainPanel 区域添加一个 Panel - GroupBox - OptionsTableLayoutPanel 的结构，并返回这个 OptionsTableLayoutPanel。
	/// </summary>
	/// <param name="groupName">GroupBox 上方显示的名称，建议首尾添加空格。</param>
	/// <returns>生成的 OptionsTableLayoutPanel，用于填充该分组的选项。</returns>
	public OptionsTableLayoutPanel AddOptionGroup(string groupName) {
		var table = new OptionsTableLayoutPanel();
		var group = new MyGroupBox(groupName);
		var panel = new GroupPanel();

		mainPanel.Controls.Add(panel);
		panel.Controls.Add(group);
		group.Controls.Add(table);

		table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
		table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

		return table;
	}

	/// <summary>
	///     在表单上方的 mainPanel 区域添加一个 Panel - GroupBox - PartyListPanel 的结构，并返回这个 PartyListPanel。
	/// </summary>
	/// <param name="groupName">GroupBox 上方显示的名称，建议首尾添加空格。</param>
	/// <param name="playerDescriptions">包含每个队员职能描述的 string[]，如 { "MT", "ST", ... }。小队人数由 Array 长度决定。</param>
	/// <returns>生成的 PartyListPanel，用于显示当前队员并调整顺序。</returns>
	public void AddPartyGroup(string groupName, PartyListPanel pListPanel) {
		_partyListPanel = pListPanel;
		var group = new MyGroupBox(groupName);
		var panel = new GroupPanel();

		mainPanel.Controls.Add(panel);
		panel.Controls.Add(group);
		group.Controls.Add(_partyListPanel);
	}

	/// <summary> 将选项添加至表单，并放置在 GroupBox 中的 Table 末尾。 </summary>
	public void AddOption(Option option, TableLayoutPanel table) {
		_options.Add(option);
		option.AppendToTable(table);
	}

	/// <summary> Add a separator line at the end of the GroupBox. </summary>
	public void AddSeparatorLine(TableLayoutPanel table) {
		table.RowCount++;
		Panel separator = new SeperatorPanel();
		table.Controls.Add(separator, 0, table.RowCount - 1);
		table.SetColumnSpan(separator, 2);
	}

	/// <summary> 在 GroupBox 中的 Table 末尾添加一个文本 Label。 </summary>
	public Label AddLabel(string desc, TableLayoutPanel table) {
		table.RowCount++;
		var lbl = new MyLabel {
			Text = desc
		};
		table.Controls.Add(lbl, 0, table.RowCount - 1);
		table.SetColumnSpan(lbl, 2);
		return lbl;
	}

	public Control AddControl(Control ctrl, TableLayoutPanel table) {
		table.RowCount++;
		table.Controls.Add(ctrl, 0, table.RowCount - 1);
		table.SetColumnSpan(ctrl, 2);
		return ctrl;
	}

	public Option GetOption(string configKey) => _options.Where(o => o.ConfigKey == configKey).FirstOrDefault();
	public IReadOnlyList<Option> GetOptions() => _options;

	/// <summary> 从触发器变量中读取全部已保存配置，若校验合法则设置到表单。 </summary>
	public void LoadFromConfig() {
		_partyListPanel?.LoadFromConfig(); // 设置了小队列表控件
		foreach (var option in _options) {
			option.LoadFromConfig(Config);
		}
	}

	/// <summary>
	///     从预设字典中加载配置。若预设字典中有对应的配置项，则将其应用到表单。
	/// </summary>
	public void ApplyPreset(Dictionary<string, string> preset)
		=> ApplyPreset(new VariableDictionary(preset));

	/// <summary>
	///     从预设字典中加载配置。若预设字典中有对应的配置项，则将其应用到表单。
	/// </summary>
	public void ApplyPreset(VariableDictionary preset) {
		foreach (var pair in preset.Values) {
			GetOption(pair.Key)?.LoadFromConfig(preset);
		}
	}

	public bool TryGetPreset(int index, out VariableDictionary preset) {
		preset = RealPlugin.Instance.GetVariableStore(true).Dict.TryGetValue($"{Info.ConfigName}{index}", out var currentPreset)
			? (VariableDictionary)currentPreset.Duplicate()
			: null;
		return preset != null;
	}

	public void SaveToConfig() {
		_partyListPanel?.SaveToConfig();

		foreach (var option in _options) {
			option.SaveToConfig(Config);
		}
		Config.SetValue("env", "${_env[COMPUTERNAME]} ${_env[USERNAME]}"); // 储存系统环境变量以保证用户不是 copy 了别人的配置
		Config.SetValue("author", Info.Author);
		Config.SetValue("version", Info.Version);

		RealPlugin.Instance.GetVariableStore(true).Dict[Info.ConfigName] = Config;
		RealPlugin.Instance.InvokeNamedCallback("command", "/e <se.10>");
		RealPlugin.Instance.InvokeNamedCallback("command", "/e 已保存配置。");
		Close();
	}

	public void SaveToPreset(int presetIdx, string presetName) {
		var preset = new VariableDictionary();
		foreach (var option in _options) {
			option.SaveToConfig(preset);
		}
		preset.SetValue("version", Info.Version);
		preset.SetValue("PresetName", presetName);
		RealPlugin.Instance.GetVariableStore(true).Dict[$"{Info.ConfigName}{presetIdx}"] = preset;
	}

	private void btnSave_Click(object? sender, EventArgs e) => SaveToConfig();

	/// <summary> 读取配置，恢复表单布局，显示表单。</summary>
	public void Run() {
		LoadFromConfig();
		ResumeLayout();
		ShowDialog();
		Dispose();
	}

	public struct ConfigInfo {
		public readonly string Name;
		public readonly string Version;
		public readonly string Author;
		public readonly string ConfigName; // 保存配置的触发器永久变量名
		public string Description {
			get {
				var value = Name;
				if (Version != null) value += $"  v{Version}";
				if (Author != null) value += $"  by {Author}";
				return value;
			}
		}

		public ConfigInfo(string name, string version, string author, string configName) {
			Name = name;
			Version = version;
			Author = author;
			ConfigName = configName;
		}
	}

	#region 其它控件类定义（格式调整）

	private class MyGroupBox : GroupBox {
		public MyGroupBox(string text) {
			Dock = DockStyle.Top;
			AutoSize = true;
			AutoSizeMode = AutoSizeMode.GrowAndShrink;
			Margin = new Padding(20);
			Text = text;
		}
	}

	private class MyCheckBox : CheckBox {
		public MyCheckBox() {
			AutoSize = true;
			Dock = DockStyle.Fill;
			Margin = new Padding(10);
		}
	}

	private class MyTextBox : TextBox {
		public MyTextBox() {
			AutoSize = true;
			Dock = DockStyle.Fill;
			Margin = new Padding(10);
		}
	}

	private class MyComboBox : ComboBox {
		public MyComboBox() {
			AutoSize = true;
			Dock = DockStyle.Fill;
			Margin = new Padding(10);
		}

		protected override void WndProc(ref Message m) {
			if (m.Msg == 0x020A) // WM_MOUSEWHEEL
			{
				return; // No-scroll
			}
			base.WndProc(ref m);
		}
	}

	private class MyLabel : Label {
		public MyLabel() {
			AutoSize = true;
			Dock = DockStyle.Fill;
			Margin = new Padding(10);
		}
	}

	private class MyButton : Button {
		public MyButton() {
			Anchor = AnchorStyles.None;
			AutoSize = true;
			Margin = new Padding(10);
			Padding = new Padding(5);
		}
	}

	private class SeperatorPanel : Panel {
		public SeperatorPanel() {
			Height = 2;
			BackColor = Color.DarkGray;
			Dock = DockStyle.Fill;
			AutoSize = true;
			Margin = new Padding(10);
		}
	}

	private class BackgroundPanel : Panel {
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

	private class GroupPanel : Panel {
		public GroupPanel() {
			AutoSize = true;
			AutoSizeMode = AutoSizeMode.GrowAndShrink;
			Dock = DockStyle.Top;
			Padding = new Padding(20, 20, 20, 0);
		}
	}

	public class OptionsTableLayoutPanel : TableLayoutPanel {
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

	private class BottomTableLayoutPanel : TableLayoutPanel {
		public BottomTableLayoutPanel() {
			Dock = DockStyle.Bottom;
			ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		}
	}

	private class MyToolTip : ToolTip {
		public MyToolTip() {
			InitialDelay = 0;
			AutoPopDelay = 30000;
			ReshowDelay = 0;
			ShowAlways = true;
		}
	}

	#endregion

	#region Options

	public abstract class Option {
		private readonly ToolTip _tip = new MyToolTip(); // 鼠标悬停时显示提示文本
		private bool _isUpdatingData;
		public Control Ctrl; // 控件，如 ComboBox
		public Label Lbl; // 左侧的描述标签（如果控件不自带文本描述）

		/// <summary> 选项对应的触发器配置字典键名。 </summary>
		public string ConfigKey { get; set; }

		public bool Enabled {
			get => Ctrl?.Enabled ?? Lbl?.Enabled ?? true;
			set {
				if (Ctrl != null)
					Ctrl.Enabled = value;
				if (Lbl != null)
					Lbl.Enabled = value;
			}
		}

		public bool Visible {
			get => Ctrl?.Visible ?? Lbl?.Visible ?? true;
			set {
				if (Ctrl != null)
					Ctrl.Visible = value;
				if (Lbl != null)
					Lbl.Visible = value;
			}
		}

		// 子类需要实现从 string 到控件数据的转换
		public abstract string Data { get; set; }

		public event EventHandler DataChanged;

		protected virtual void OnDataChanged() {
			if (_isUpdatingData) return;
			try {
				_isUpdatingData = true;
				DataChanged?.Invoke(this, EventArgs.Empty);
			} finally {
				_isUpdatingData = false;
			}
		}

		public void InitializeData() => OnDataChanged();

		/// <summary>
		///     在 TableLayoutPanel 末尾添加空行，并将该选项置于这一行。
		/// </summary>
		/// <param name="table">选项所处的父对象 TableLayoutPanel。</param>
		internal virtual void AppendToTable(TableLayoutPanel table) {
			table.RowCount++;
			table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

			table.Controls.Add(Lbl, 0, table.RowCount - 1);
			if (Ctrl != null)
				table.Controls.Add(Ctrl, 1, table.RowCount - 1);
			else
				table.SetColumnSpan(Lbl, 2);
		}

		protected virtual void SetHint(string hint) {
			if (!string.IsNullOrWhiteSpace(hint)) {
				if (Lbl != null) {
					_tip.SetToolTip(Lbl, hint);
					Lbl.Cursor = Cursors.Help;
				}
				if (Ctrl != null) {
					_tip.SetToolTip(Ctrl, hint);
					Ctrl.Cursor = Cursors.Help;
				}
			}
		}

		public virtual void LoadFromConfig(VariableDictionary cfg) {
			if (ConfigKey == null) return;
			if (cfg != null && cfg.ContainsKey(ConfigKey)) {
				Data = cfg.GetValue(ConfigKey).ToString().Trim();
			}
		}

		public virtual void SaveToConfig(VariableDictionary cfg) {
			if (ConfigKey == null) return;
			cfg.SetValue(ConfigKey, Data);
		}
	}

	public class OptionTxt : Option {
		public OptionTxt(string desc, string configKey, string defaultText = "", string hint = null) {
			Lbl = new MyLabel {
				Text = desc
			};
			Ctrl = new MyTextBox {
				Text = defaultText
			};
			Txt.TextChanged += (_, _) => OnDataChanged();
			ConfigKey = configKey;
			SetHint(hint);
		}

		public TextBox Txt => (TextBox)Ctrl;

		public override string Data {
			get => Txt.Text.Trim();
			set => Txt.Text = value.Trim();
		}
	}

	public class OptionChk : Option {
		public OptionChk(string desc, string configKey, bool defaultChecked = false, string hint = null) {
			Lbl = new MyLabel {
				Text = desc
			};
			Ctrl = new MyCheckBox {
				Checked = defaultChecked
			};
			Chk.CheckedChanged += (_, _) => OnDataChanged();
			ConfigKey = configKey;
			SetHint(hint);
		}

		public CheckBox Chk => (CheckBox)Ctrl;

		public override string Data {
			get => Chk.Checked ? "1" : "0";
			set => Chk.Checked = !MathParser.IsZero(MathParser.Parse(value));
		}
	}

	public class OptionCbx : Option {
		private readonly BijectDictionary<string, string> _data;

		/// <summary>
		///     根据双向字典 <paramref name="data" /> 生成一个 Label 和 ComboBox 的组合。
		/// </summary>
		/// <param name="desc">Description of the label (left side).</param>
		/// <param name="configKey">The key saved into config dictionary.</param>
		/// <param name="data">BijectDictionary containing the keys and values of the options.</param>
		public OptionCbx(string desc, string configKey, BijectDictionary<string, string> data, string defaultKey, string hint = null)
			: this(desc, configKey, data, data.Keys.IndexOf(defaultKey), hint) {
		}

		/// <summary>
		///     根据双向字典 <paramref name="data" /> 生成一个 Label 和 ComboBox 的组合。
		/// </summary>
		/// <param name="desc">左侧 label 描述</param>
		/// <param name="configKey">存储到永久字典变量中的键</param>
		/// <param name="data">字典键与选项文本描述的双向字典</param>
		public OptionCbx(string desc, string configKey, BijectDictionary<string, string> data, int defaultIndex = 0, string hint = null) {
			Lbl = new MyLabel {
				Text = desc
			};
			_data = data;
			Ctrl = new MyComboBox();
			Cbx.Items.AddRange(data.Values.ToArray());
			Cbx.SelectedIndex = defaultIndex >= 0 && defaultIndex < Cbx.Items.Count ? defaultIndex : 0;
			Cbx.DropDownStyle = ComboBoxStyle.DropDownList;
			Cbx.SelectedIndexChanged += (_, _) => OnDataChanged();
			ConfigKey = configKey;
			SetHint(hint);
		}

		public ComboBox Cbx => (ComboBox)Ctrl;

		public override string Data {
			get {
				var selection = Cbx.SelectedItem?.ToString() ?? Cbx.SelectedText;
				return _data.GetKey(selection) ?? selection;
			}
			set {
				var option = value.Trim();
				Cbx.SelectedItem = _data[option] ?? option;
			}
		}
	}

	public class OptionCustom : Option {
		private readonly Func<Control, string> _getter;
		private readonly Action<Control, string> _setter;

		public OptionCustom(
			string desc, string configKey, Control ctrl,
			Func<Control, string> getter,
			Action<Control, string> setter,
			string defaultData, string hint = null) {
			Lbl = new MyLabel {
				Text = desc
			};
			Ctrl = ctrl;
			ConfigKey = configKey;
			_getter = getter;
			_setter = setter;
			_setter(Ctrl, defaultData);
			SetHint(hint);
		}

		public override string Data {
			get => _getter(Ctrl);
			set => _setter(Ctrl, value);
		}
	}

	#endregion
}

/// <summary> 可以从值检索键的双射字典结构，可以用于将 ComboBox 选项和触发器内存储的键相互映射。</summary>
public class BijectDictionary<TKey, TValue> {
	private readonly Dictionary<TKey, TValue> _dict = new();
	private readonly List<TKey> _keys = [];
	private readonly Dictionary<TValue, TKey> _revDict = new();
	private readonly List<TValue> _values = [];

	public BijectDictionary() : this([]) {
	}

	public BijectDictionary(params (TKey, TValue)[] items) {
		foreach (var (key, value) in items) {
			if (_dict.ContainsKey(key))
				throw new Exception($"Key \"{key}\" is duplicated in the bijective dictionary.");
			if (_revDict.ContainsKey(value))
				throw new Exception($"Value \"{value}\" is duplicated in the bijective dictionary.");

			_dict[key] = value;
			_revDict[value] = key;
			_keys.Add(key);
			_values.Add(value);
		}
	}

	public ReadOnlyCollection<TKey> Keys => _keys.AsReadOnly();
	public ReadOnlyCollection<TValue> Values => _values.AsReadOnly();
	public int Count => _dict.Count;

	public TValue this[TKey key] => _dict.GetValueOrDefault(key);
	public bool ContainsKey(TKey key) => _dict.ContainsKey(key);
	public bool ContainsValue(TValue value) => _revDict.ContainsKey(value);

	public TKey GetKey(TValue value) => _revDict.GetValueOrDefault(value);

	public bool RemoveKey(TKey key) {
		lock (this) {
			var index = _keys.IndexOf(key);
			if (index < 0)
				return false;
			Remove(key, _values[index], index);
			return true;
		}
	}

	public bool RemoveValue(TValue value) {
		lock (this) {
			var index = _values.IndexOf(value);
			if (index < 0)
				return false;
			Remove(_keys[index], value, index);
			return true;
		}
	}

	private void Remove(TKey key, TValue value, int index) {
		_keys.RemoveAt(index);
		_values.RemoveAt(index);
		_dict.Remove(key);
		_revDict.Remove(value);
	}

	public BijectDictionary<TKey, TValue> ShallowCopy() {
		var duplicate = new BijectDictionary<TKey, TValue>();
		foreach (var kvp in _dict) {
			duplicate._dict.Add(kvp.Key, kvp.Value);
			duplicate._revDict.Add(kvp.Value, kvp.Key);
			duplicate._keys.Add(kvp.Key);
			duplicate._values.Add(kvp.Value);
		}
		return duplicate;
	}
}