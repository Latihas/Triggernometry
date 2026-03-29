using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;
using Triggernometry.Localization;
using Triggernometry.UI.CustomControls;

namespace Triggernometry.Core;

public partial class RealPlugin {
	private void FixConfigurationOnStartCN() {
		cfg.ShowWelcome = false;
		cfg.TestLiveByDefault = true;
		cfg.TestIgnoreConditionsByDefault = true;
		cfg.TtsMethod = Configuration.AudioRoutingMethodEnum.ACT;
		cfg.AutosaveEnabled = true;
		cfg.UpdateNotifications = Configuration.UpdateNotificationsEnum.Yes;
		cfg.UpdateCheckMethod = Configuration.UpdateCheckMethodEnum.External;
		cfg.UpdateExternalChannelUrl = "https://1824544011.v.123pan.cn/1824544011/Triggernometry_Release_CN/UpdateManifest.xml";
		cfg.AutoUpdate = true;
		var apis = cfg._APIUsages;
		var utilities = apis?.FirstOrDefault(a => a.Name == "Triggernometry.Utilities");
		if (utilities != null) {
			utilities.AllowLocal = true;
			utilities.AllowRemote = true;
			utilities.AllowAdmin = true;
		}
	}

	public static void CopyMissingTranslations() {
		var entries = I18n.CurrentLanguage.ExportMissingTranslations();

		var serializer = new XmlSerializer(typeof(List<Language.TranslationEntry>), new XmlRootAttribute("Translations"));
		using (var writer = new StringWriter()) {
			serializer.Serialize(writer, entries);
			var serializedEntries = writer.ToString();
			ActionOld.ClipboardSetText(serializedEntries);
			MessageBox.Show("Missing translations copied to clipboard.");
		}
	}

	private static readonly List<string> _legalRepoPrefixes = new() {
		"https://github.com/paissaheavyindustries/Triggernometry",
		"https://vip.123pan.cn/1824544011/",
		"https://1824544011.v.123pan.cn/"
	};

	public void AddRepo(Repository r, bool shouldUpdate) {
		UserInterface.AddRepo(r, shouldUpdate);
	}

	public void AddRepos(IEnumerable<Repository> repos, bool shouldUpdate) {
		foreach (var r in repos) {
			AddRepo(r, shouldUpdate);
		}
	}

	public void RemoveRepo(string partialUrl) {
		UserInterface.RemoveRepo(partialUrl);
	}

	public Repository DefaultRepoCN(string address, string name, int updateIntervalMinutes) => new() {
		Enabled = true,
		Address = address,
		AllowProcessLaunch = true,
		AllowScriptExecution = true,
		KeepLocalBackup = true,
		Name = name,
		NewBehavior = Repository.NewBehaviorEnum.AsDefined,
		UpdatePolicy = Repository.UpdatePolicyEnum.Startup,
		AudioOutput = Repository.AudioOutputEnum.NeverOverride,
		AutoUpdate = true,
		UpdateInterval = updateIntervalMinutes
	};

	public void AddDefaultRepoCN(bool shouldUpdate = false) {
		var now = DateTime.Now;
		var isVCPeriod = new DateTime(2026, 3, 3) < now && now < new DateTime(2026, 3, 17);
		var repos = new List<Repository> {
			DefaultRepoCN("https://1824544011.v.123pan.cn/1824544011/Remote_Triggers/SelfTest.xml",
				"[工具] 问题自检工具箱 + 使用教程　　有问题请自行在此解决", 60 * 6),
			DefaultRepoCN("https://1824544011.v.123pan.cn/1824544011/Remote_Triggers/Utils.xml",
				"[工具] 运行支持库（必需）", 60 * 6),
			DefaultRepoCN("https://1824544011.v.123pan.cn/1824544011/Remote_Triggers/S7a.xml",
				"7.0 M1-4 阿卡狄亚轻量级", 60 * 24),
			DefaultRepoCN("https://1824544011.v.123pan.cn/1824544011/Remote_Triggers/S7b.xml",
				"7.2 M5-8 阿卡狄亚中量级", 60 * 24),
			DefaultRepoCN("https://1824544011.v.123pan.cn/1824544011/Remote_Triggers/S7c.xml",
				"7.4 M9-12 阿卡狄亚重量级", 60 * 24),
			DefaultRepoCN("https://1824544011.v.123pan.cn/1824544011/Remote_Triggers/Ex7.xml",
				"7.X 极神", 60 * 24),
			DefaultRepoCN("https://1824544011.v.123pan.cn/1824544011/Remote_Triggers/temp.xml",
				"临时推送", 60 * 24),
			DefaultRepoCN("https://1824544011.v.123pan.cn/1824544011/Remote_Triggers/U7a.xml",
				"7.1 绝伊甸", 60 * 24),
			DefaultRepoCN("https://1824544011.v.123pan.cn/1824544011/Remote_Triggers/field.xml",
				"特殊场景探索", 60 * 24),
			DefaultRepoCN("https://1824544011.v.123pan.cn/1824544011/Remote_Triggers/dungeon.xml",
				"深宫", 60 * 24),
			DefaultRepoCN("https://1824544011.v.123pan.cn/1824544011/Remote_Triggers/vc.xml",
				"异闻迷宫", isVCPeriod ? 60 : 60 * 24)
		};
		RemoveRepo("vip.123pan.cn/1824544011"); // old
		AddRepos(repos, shouldUpdate);
	}
}