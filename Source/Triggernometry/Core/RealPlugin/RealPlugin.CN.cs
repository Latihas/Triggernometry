using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;
using Triggernometry.Localization;

namespace Triggernometry.Core;

public partial class RealPlugin {
	internal const string UpdateRemotePathCN = "https://1824544011.cdn.123clouddisk.com/1824544011/Triggernometry_Release_CN/";

	private void FixConfigurationOnStartCN() {
		cfg.ShowWelcome = false;
		cfg.TestLiveByDefault = true;
		cfg.TestIgnoreConditionsByDefault = true;
		cfg.TtsMethod = Configuration.AudioRoutingMethodEnum.ACT;
		cfg.AutosaveEnabled = true;
		cfg.UpdateNotifications = Configuration.UpdateNotificationsEnum.Yes;
		cfg.UpdateCheckMethod = Configuration.UpdateCheckMethodEnum.External;
		cfg.UpdateExternalChannelUrl = UpdateRemotePathCN + "UpdateManifest.xml";
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
		using var writer = new StringWriter();
		serializer.Serialize(writer, entries);
		var serializedEntries = writer.ToString();
		ActionOld.ClipboardSetText(serializedEntries);
		MessageBox.Show("Missing translations copied to clipboard.");
	}
}