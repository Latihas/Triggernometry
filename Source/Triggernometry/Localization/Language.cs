using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Triggernometry.Localization;

public sealed class Language {
	public enum MissingHandlingEnum {
		DefaultLanguage,
		DefaultString,
		OutputKey
	}

	[XmlAttribute] public string LanguageName { get; set; }
	[XmlAttribute] public MissingHandlingEnum MissingKeyHandling { get; set; }

	internal Dictionary<string, string> TranslationsLookup { get; set; }

	internal bool IsDefault { get; set; }

	public class TranslationEntry {
		[XmlAttribute] public string Key { get; set; }
		[XmlAttribute] public string Translation { get; set; }
	}

	public List<TranslationEntry> Translations = [];

	public Language() {
		IsDefault = true;
		LanguageName = "(undefined)";
		TranslationsLookup = new Dictionary<string, string>();
		MissingKeyHandling = MissingHandlingEnum.DefaultLanguage;
	}

	internal void BuildLookup() {
		foreach (var te in Translations) {
			TranslationsLookup[te.Key] = te.Translation;
		}
		Translations.Clear();
	}

	internal void BuildList() {
		Translations.Clear();
		foreach (var kp in TranslationsLookup) {
			Translations.Add(new TranslationEntry {
				Key = kp.Key,
				Translation = kp.Value
			});
		}
		Translations.Sort((a, b) => { return a.Key.CompareTo(b.Key); });
	}

	public string Lookup(string key) {
		if (TranslationsLookup.TryGetValue(key, out var lookup)) {
			return lookup;
		}
		return null;
	}

	private Dictionary<string, string> _missingTranslations = new();

	public string Translate(string key, string text, params object[] args) {
		if (IsDefault) { TranslationsLookup.TryAdd(key, text); }
		if (TranslationsLookup.TryGetValue(key, out var data)) {
			return string.Format(data, args);
		}
		//start
		if (!string.IsNullOrEmpty(text)) {
			_missingTranslations.TryAdd(key, text);
			//RealPlugin.plug.UnfilteredAddToLog(RealPlugin.DebugLevelEnum.Warning, $"Missing translation recorded: \"{key}\" => \"{text}\"");
		}
		//end
		switch (MissingKeyHandling) {
			case MissingHandlingEnum.DefaultString: {
				if (TranslationsLookup.TryGetValue("internal/default", out var value)) {
					return string.Format(value, key);
				}
				return string.Format(text, args);
			}
			case MissingHandlingEnum.OutputKey:
				return key;
		}
		return string.Format(text, args);
	}

	internal List<TranslationEntry> ExportMissingTranslations() {
		var sortedKeys = _missingTranslations.Keys.ToList();
		sortedKeys.Sort();

		return sortedKeys.Select(key => new TranslationEntry {
			Key = key,
			Translation = _missingTranslations[key]
		}).ToList();
	}
}