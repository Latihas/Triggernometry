using System;
using System.IO;
using System.Xml.Serialization;
using Triggernometry.Localization;
using TriggernometryProxy;

namespace Triggernometry.Core;

public partial class RealPlugin {
	private void InitLanguage() {
		var ld = new Language {
			LanguageName = "English (default)",
			MissingKeyHandling = Language.MissingHandlingEnum.OutputKey
		};
		I18n.DefaultLanguage = ld;
		I18n.AddLanguage(ld);
		ChangeLanguage(null);
	}

	internal void ChangeLanguage(string? langname) {
		FilteredAddToLog(DebugLevelEnum.Info, I18n.Translate("internal/Plugin/langchange", "Changing language from '{0}' to '{1}'",
			I18n.CurrentLanguage != null ? I18n.CurrentLanguage.LanguageName : "(not set)",
			langname ?? "(default)"
		));
		if (I18n.ChangeLanguage(langname)) {
			FilteredAddToLog(DebugLevelEnum.Info, I18n.Translate("internal/Plugin/langchangeok", "Language is now '{0}'",
				I18n.CurrentLanguage != null ? I18n.CurrentLanguage.LanguageName : "(not set)"
			));
			if (cfg != null && I18n.CurrentLanguage != null) 
				cfg.Language = langname == null ? null : I18n.CurrentLanguage.LanguageName;
		} else {
			FilteredAddToLog(DebugLevelEnum.Info, I18n.Translate("internal/Plugin/langchangefail", "Couldn't change language from '{0}' to '{1}'",
				I18n.CurrentLanguage != null ? I18n.CurrentLanguage.LanguageName : "(not set)",
				langname ?? "(default)"
			));
		}
	}

	private Language? LoadLanguage(string filename) {
		try {
			var x = I18n.Translate("internal/Plugin/langload", "Loading language from '{0}'", filename);
			FilteredAddToLog(DebugLevelEnum.Info, x);
			var fi = new FileInfo(filename);
			if (!fi.Exists) {
				FilteredAddToLog(DebugLevelEnum.Warning, I18n.Translate("internal/Plugin/langfilenotfound", "Language file '{0}' does not exist", filename));
				return null;
			}
			var xs = new XmlSerializer(typeof(Language));
			var l = ProxyPlugin.Framework.RunOnFrameworkThread(() => {
				using var fs = File.Open(filename, FileMode.Open, FileAccess.Read);
				return (Language)xs.Deserialize(fs)!;
			}).Result;
			l.IsDefault = false;
			l.BuildLookup();
			return l;
		} catch (Exception ex) {
			FilteredAddToLog(DebugLevelEnum.Error, I18n.Translate("internal/Plugin/langloadfail", "Loading language file failed, make sure you are running the latest version"));
			GenericExceptionHandler(I18n.Translate("internal/Plugin/langloadex", "Loading the language file '{0}' failed due to an exception", filename), ex);
		}
		return null;
	}

	private void LoadLanguages() {
		foreach (var file in Directory.GetFiles(ProxyPlugin.PluginInterface.AssemblyLocation.Directory!.ToString(), "*.triglations.xml", SearchOption.TopDirectoryOnly)) {
			var l = LoadLanguage(file);
			if (l != null) I18n.AddLanguage(l);
		}
	}

	public string Translate(string key, string text, params object[] args) => I18n.Translate(key, text, args);
}