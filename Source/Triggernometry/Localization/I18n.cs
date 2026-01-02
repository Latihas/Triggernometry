using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using Triggernometry.Core;

namespace Triggernometry.Localization;

public static class I18n {
    internal static Dictionary<string, Language> RegisteredLanguages = new();

    internal static Language BuiltInLanguage;
    internal static Language DefaultLanguage;
    internal static Language CurrentLanguage;

    internal static object DoNotTranslate = new();

    internal static string ThingToString(int d) =>
        // minus signs could be different: "-" or "−"
        d.ToString(CultureInfo.InvariantCulture);

    internal static string ThingToString(float d) => ((decimal)d).ToString(CultureInfo.InvariantCulture);

    internal static string ThingToString(double d) => ((decimal)d).ToString(CultureInfo.InvariantCulture);

    internal static void AddLanguage(Language ld) {
        if (ld.IsDefault) {
            DefaultLanguage = ld;
        }
        if (RegisteredLanguages.ContainsKey(ld.LanguageName)) {
            var basename = ld.LanguageName;
            for (var i = 2;; i++) {
                var curname = basename + " #" + i;
                if (RegisteredLanguages.ContainsKey(curname)) {
                    continue;
                }
                ld.LanguageName = curname;
                RegisteredLanguages[curname] = ld;
                break;
            }
        }
        else {
            RegisteredLanguages[ld.LanguageName] = ld;
        }
        if (CurrentLanguage == null) {
            CurrentLanguage = ld;
        }
    }

    internal static string Lookup(string key, string defValue) {
        if (CurrentLanguage != null) {
            var ex = CurrentLanguage.Lookup(key);
            if (ex != null) {
                return ex;
            }
        }
        return defValue;
    }

    public static string Translate(string key, string text, params object[] args) {
        if (BuiltInLanguage != null) {
            if (!BuiltInLanguage.TranslationsLookup.ContainsKey(key)) {
                BuiltInLanguage.TranslationsLookup[key] = text;
            }
        }
        if (CurrentLanguage != null) {
            try {
                return CurrentLanguage.Translate(key, text, args);
            }
            catch (FormatException) {
                RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Error, Translate("internal/I18n/formatex",
                    "You might need to update your translation file (your_language_name.triglations.xml). \nFormatException occured during translating \"{0}\".", key));
                return string.Format(text, args);
            }
        }
        return string.Format(text, args);
    }

    internal static void TranslateSecondaryControl(string path, ToolStripItem tsi) {
        if (tsi.Text != null && tsi.Text != "" && tsi.Tag != DoNotTranslate) {
            tsi.Text = GetLocalizationFor(path + "/" + tsi.Name, tsi.Text);
        }
        if (tsi is ToolStripMenuItem) {
            var x = (ToolStripMenuItem)tsi;
            foreach (ToolStripItem tsic in x.DropDownItems) {
                TranslateSecondaryControl(path, tsic);
            }
        }
        else if (tsi is ToolStripDropDownButton) {
            var x = (ToolStripDropDownButton)tsi;
            foreach (ToolStripItem tsic in x.DropDownItems) {
                TranslateSecondaryControl(path, tsic);
            }
        }
        else if (tsi is ToolStripComboBox cbx) {
            for (var i = 0; i < cbx.Items.Count; i++) {
                var o = cbx.Items[i].ToString();
                cbx.Items[i] = GetLocalizationFor(path + "/" + cbx.Name + "[" + o + "]", o);
            }
        }
    }

    internal static string GetLocalizationFor(string path, string current) => Translate(path, current);

    internal static bool ChangeLanguage(string langname) {
        if (langname == null) {
            if (BuiltInLanguage == null) {
                BuiltInLanguage = DefaultLanguage;
            }
            CurrentLanguage = DefaultLanguage;
            return true;
        }
        if (RegisteredLanguages.TryGetValue(langname, out var language)) {
            CurrentLanguage = language;
            return true;
        }
        CurrentLanguage = DefaultLanguage;
        return false;
    }

    internal static void TranslateControl(string path, Control c) {
        try {
            if (c.Tag == DoNotTranslate) {
                return;
            }
            if (c is UserControl) {
                path += "/" + c.Name;
                foreach (Control cc in c.Controls) {
                    TranslateControl(path, cc);
                }
                return;
            }
            if (c is NumericUpDown) {
                return;
            }
            if (c.ContextMenuStrip != null) {
                var ctx = c.ContextMenuStrip;
                foreach (ToolStripItem tsi in ctx.Items) {
                    TranslateSecondaryControl(path, tsi);
                }
            }
            if (c.Text != null && c.Text != "") {
                if (c is TabPage) {
                    if (((TabControl)((TabPage)c).Parent).Appearance != TabAppearance.FlatButtons) {
                        c.Text = GetLocalizationFor(path + "/" + c.Name, c.Text);
                    }
                }
                else {
                    c.Text = GetLocalizationFor(path + "/" + c.Name, c.Text);
                }
            }
            if (c is CheckedListBox) {
                //D
            }
            else if (c is ListBox) {
                var x = (ListBox)c;
                for (var i = 0; i < x.Items.Count; i++) {
                    var o = x.Items[i].ToString();
                    x.Items[i] = GetLocalizationFor(path + "/" + c.Name + "[" + o + "]", o);
                }
            }
            else if (c is ComboBox) {
                var x = (ComboBox)c;
                for (var i = 0; i < x.Items.Count; i++) {
                    var o = x.Items[i].ToString();
                    x.Items[i] = GetLocalizationFor(path + "/" + c.Name + "[" + o + "]", o);
                }
            }
            else if (c is DataGridView) {
                var x = (DataGridView)c;
                for (var i = 0; i < x.Columns.Count; i++) {
                    var hd = x.Columns[i].HeaderText.Trim();
                    if (hd.Length > 0) {
                        x.Columns[i].HeaderText = GetLocalizationFor(path + "/" + x.Columns[i].Name, x.Columns[i].HeaderText);
                    }
                }
            }
            if (c is ToolStrip) {
                var ts = (ToolStrip)c;
                foreach (ToolStripItem tsi in ts.Items) {
                    TranslateSecondaryControl(path, tsi);
                }
            }
            else {
                foreach (Control cc in c.Controls) {
                    TranslateControl(path, cc);
                }
            }
        }
        catch (Exception ex) {
            // Some welcome labels might throw exceptions after successfully translated.
            // Reason not examined yet.
            // Reproduce: Set language to Chinese, open and save configuration form, set language to English.
            RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Warning,
                $"Error occurs when translating {c.Name} ({c.Text ?? "null"}): {ex.Message}");
        }
    }

    internal static void TranslateForm(Form f) {
        var basePath = f.GetType().Name;
        if (f.Text != null && f.Text != "") {
            f.Text = GetLocalizationFor(basePath, f.Text);
        }
        foreach (Control c in f.Controls) {
            TranslateControl(basePath, c);
        }
    }

    private static readonly HashSet<string> _wordsToTranslate = [
        "bool", "char", "charcode", "double", "float", "hex", "index", "int", "key",
        "length", "property", "slice", "startindex", "string", "time", "times", "type", "version"
    ];

    internal static string TranslateWord(string key) {
        key = key.ToLower();
        if (_wordsToTranslate.Contains(key)) {
            var path = $"internal/I18n/{key}";
            return Translate(path, key);
        }
        throw new Exception(Translate("internal/I18n/translatewordmissingkey",
            "The key {0} is not in I18n._wordsToTranslate. Please report the bug if you see this error.", key));
    }

    internal static string TrlVarPersist(bool isPersist) =>
        isPersist
            ? Translate("internal/I18n/descpersistent", "persistent ")
            : "";

    internal static string TrlExprType(bool isStringExpr) =>
        isStringExpr
            ? Translate("internal/I18n/descexprtypestring", "string")
            : Translate("internal/I18n/descexprtypenumeric", "numeric");

    internal static string TrlTableColOrRow(bool isCol) =>
        isCol
            ? Translate("internal/I18n/desctablelineopcol", "column")
            : Translate("internal/I18n/desctablelineoprow", "row");

    internal static string TrlCacheFile(bool cache) => cache ? Translate("internal/I18n/desccachefile", ", caching the file on disk") : "";

    internal static string TrlSortAscOrDesc(bool isAsc) =>
        isAsc
            ? Translate("internal/I18n/descsortasc", "ascending")
            : Translate("internal/I18n/descsortdesc", "descending");

    internal static string TrlAsync(bool isAsync) =>
        isAsync
            ? Translate("internal/I18n/descasynctrue", "")
            : Translate("internal/I18n/descasyncfalse", "[Sync] ");

    internal static string TranslateEnable(bool enable) =>
        enable
            ? Translate("internal/I18n/descenabletrue", "enable")
            : Translate("internal/I18n/descenablefalse", "disable");

    public static string TrlTriggerDescTime(double ms) {
        ms = Math.Round(ms);
        var s = ms / 1000;
        if (Math.Abs(s) >= 300 || s == Math.Round(s)) // > 5 min   or is integer
        {
            return Translate("internal/I18n/desctimesec", "{0} s", (int)s);
        }
        if (Math.Abs(s) >= 10 || s == Math.Round(s, 1)) // > 10 s   or 1-digit decimal
        {
            return Translate("internal/I18n/desctimesec", "{0} s", s.ToString("F1", CultureInfo.InvariantCulture));
        }
        if (Math.Abs(s) >= 0.1 || s == Math.Round(s, 2)) // > 0.1 s   or 2-digit decimal
        {
            return Translate("internal/I18n/desctimesec", "{0} s", s.ToString("F2", CultureInfo.InvariantCulture));
        }
        // < 0.1 s
        return Translate("internal/I18n/desctimems", "{0} ms", ms);
    }

    public static bool IsChineseEnvironment => (RealPlugin.Instance.cfg.Language ?? "").Contains("zh") || CultureInfo.CurrentCulture.TwoLetterISOLanguageName == "zh";
}