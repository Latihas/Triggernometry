using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml.Serialization;
using Triggernometry.Core.Conditions;
using Triggernometry.Core.Serialization;
using Triggernometry.Core.Variables;
using Triggernometry.Localization;

namespace Triggernometry.Core;

public class Configuration {
	// Configuration Form

	#region General

	// Logging

	[XmlAttribute] public RealPlugin.DebugLevelEnum DebugLevel { get; set; } = RealPlugin.DebugLevelEnum.Info;

	[XmlAttribute] public string VerboseDebug // Old version
	{
		get => null;
		set {
			if (value == "true") {
				DebugLevel = RealPlugin.DebugLevelEnum.Verbose;
			} else {
				DebugLevel = RealPlugin.DebugLevelEnum.Info;
			}
		}
	}

	[XmlAttribute] public bool LogNormalEvents { get; set; } = true;

	[XmlAttribute] public bool LogVariableExpansions { get; set; }

	// Startup

	internal bool _ShowWelcomeHasBeenSet;
	private bool _ShowWelcome { get; set; } = true;

	[XmlAttribute] public bool ShowWelcome {
		get => _ShowWelcome;
		set {
			_ShowWelcome = value;
			_ShowWelcomeHasBeenSet = true;
		}
	}

	[XmlAttribute] public bool WarnAdmin { get; set; } = true;

	public enum UpdateCheckMethodEnum {
		Builtin,
		ACT,
		External
	}

	[XmlAttribute] public UpdateNotificationsEnum UpdateNotifications { get; set; } = UpdateNotificationsEnum.Undefined;

	[XmlAttribute] public UpdateCheckMethodEnum UpdateCheckMethod { get; set; } = UpdateCheckMethodEnum.ACT;

	[XmlAttribute] public bool AutoUpdate { get; set; }

	[XmlAttribute] public string UpdateExternalChannelUrl { get; set; } = "";

	// Startup Trigger/Folder

	public enum StartupTriggerTypeEnum {
		Trigger,
		Folder
	}

	[XmlAttribute] public StartupTriggerTypeEnum StartupTriggerType { get; set; } = StartupTriggerTypeEnum.Trigger;

	[XmlAttribute] public Guid StartupTriggerId { get; set; } = Guid.Empty;

	#endregion

	#region Audio

	/// <summary>
	///     Use simulated wave generator instead of Console.Beep() for ActionBeep. <br />
	///     Console.Beep() might cause lagging on some PC. <br />
	///     The simulated method also supports volume adjustment.
	/// </summary>
	[XmlIgnore] public bool UseSimulatedBeep { get; set; } = true;

	[XmlAttribute("UseSimulatedBeep")] public string Xml_UseSimulatedBeep {
		get => XmlAttr.Bool(UseSimulatedBeep, true);
		set => UseSimulatedBeep = XmlAttr.Bool(value);
	}

	/// <summary>
	///     Volume of the simulated beep (1-100). Only effective when using simulated beep instead of Console.Beep(). <br />
	/// </summary>
	[XmlIgnore] public int SimulatedBeepVolume { get; set; } = 100;

	[XmlAttribute("SimulatedBeepVolume")] public string Xml_SimulatedBeepVolume {
		get => XmlAttr.Int(SimulatedBeepVolume, 100);
		set => SimulatedBeepVolume = XmlAttr.Int(value);
	}

	public enum AudioRoutingMethodEnum {
		None,
		Triggernometry,
		ACT,
		ExternalApplication
	}

	// Global volume adjustment

	[XmlAttribute] public int SfxVolumeAdjustment { get; set; } = 100;

	[XmlAttribute] public int TtsVolumeAdjustment { get; set; } = 100;

	// ACT hooks

	[XmlAttribute] public string UseACTForSound {
		get => null;
		set {
			var temp = bool.Parse(value);
			if (temp) {
				SoundMethod = AudioRoutingMethodEnum.ACT;
			} else {
				SoundMethod = AudioRoutingMethodEnum.Triggernometry;
			}
		}
	}

	[XmlAttribute] public string UseACTForTTS {
		get => null;
		set {
			var temp = bool.Parse(value);
			if (temp) {
				TtsMethod = AudioRoutingMethodEnum.ACT;
			} else {
				TtsMethod = AudioRoutingMethodEnum.Triggernometry;
			}
		}
	}

	[XmlAttribute] public int SoundRepCooldown { get; set; } = 500;

	[XmlAttribute] public int TtsRepCooldown { get; set; } = 500;

	[XmlAttribute] public AudioRoutingMethodEnum SoundMethod { get; set; } = AudioRoutingMethodEnum.Triggernometry;

	[XmlAttribute] public string SoundExternalApp { get; set; } = "";

	[XmlAttribute] public string SoundExternalAppArgs { get; set; } = "";

	[XmlAttribute] public AudioRoutingMethodEnum TtsMethod { get; set; } = AudioRoutingMethodEnum.Triggernometry;

	[XmlAttribute] public string TtsExternalApp { get; set; } = "";

	[XmlAttribute] public string TtsExternalAppArgs { get; set; } = "";

	#endregion

	#region Shortcuts

	[XmlAttribute] public bool EnableShortcutTemplates { get; set; } = true;
	public bool UseAbbrevInTemplates { get; set; } = true;
	public bool WrapTextWhenSelected { get; set; } = true;

	#endregion

	#region Caching

	[XmlAttribute] public int CacheImageExpiry { get; set; } = 518400;

	[XmlAttribute] public int CacheSoundExpiry { get; set; } = 518400;

	[XmlAttribute] public int CacheJsonExpiry { get; set; } = 10080;

	[XmlAttribute] public int CacheRepoExpiry { get; set; } = 518400;

	[XmlAttribute] public int CacheFileExpiry { get; set; } = 518400;

	#endregion

	#region Endpoint

	[XmlAttribute] public string HttpEndpoint { get; set; } = "http://localhost:51423/";

	[XmlAttribute] public bool StartEndpointOnLaunch { get; set; } = true;

	[XmlAttribute] public bool LogEndpoint { get; set; } = true;

	#endregion

	#region FFXIV

	[XmlAttribute] public bool FfxivLogNetwork { get; set; }

	public enum FfxivPartyOrderingEnum {
		Legacy,
		CustomSelfFirst,
		CustomFull
	}

	[XmlAttribute] public FfxivPartyOrderingEnum FfxivPartyOrdering { get; set; } = FfxivPartyOrderingEnum.CustomSelfFirst;
	private string _FfxivCustomPartyOrder;
	[XmlAttribute] public string FfxivCustomPartyOrder {
		get => _FfxivCustomPartyOrder;
		set {
			_FfxivCustomPartyOrder = value;
			_FfxivCustomPartyOrderLookup.Clear();
			var ex = value.Split(",".ToArray(), StringSplitOptions.RemoveEmptyEntries);
			var subn = 1;
			foreach (var e in ex) {
				var et = e.Trim();
				var valn = 0;
				if (int.TryParse(et, out valn)) {
					if (!_FfxivCustomPartyOrderLookup.ContainsKey(valn)) {
						_FfxivCustomPartyOrderLookup[valn] = subn;
						subn++;
					}
				}
			}
		}
	}

	internal int GetPartyOrderValue(int job) {
		if (_FfxivCustomPartyOrderLookup.TryGetValue(job, out var value)) {
			return value;
		}
		return 9999;
	}

	internal int GetPartyOrderValue(string job) {
		var ex = 0;
		if (int.TryParse(job, out ex)) {
			return GetPartyOrderValue(ex);
		}
		return 999;
	}

	private Dictionary<int, int> _FfxivCustomPartyOrderLookup { get; set; } = new();

	#endregion

	#region Substitutions

	public List<Substitution> Substitutions { get; set; } = [];

	public class Substitution : IComparable {
		[Flags]
		public enum SubstitutionScopeEnum {
			CaptureGroup = 1,
			NumericExpression = 2,
			StringExpression = 4,
			TextToSpeech = 8
		}

		[XmlAttribute] public string SearchFor { get; set; }

		[XmlAttribute] public string ReplaceWith { get; set; }

		[XmlAttribute] public SubstitutionScopeEnum Scope { get; set; }

		public string Replace(string input) => input.Replace(SearchFor, ReplaceWith);

		public int CompareTo(object obj) {
			var b = (Substitution)obj;
			var x = SearchFor.CompareTo(b.SearchFor);
			if (x != 0) {
				return x;
			}
			x = ReplaceWith.CompareTo(b.ReplaceWith);
			if (x != 0) {
				return x;
			}
			return Scope.CompareTo(b.Scope);
		}
	}

	public string PerformSubstitution(string input, Substitution.SubstitutionScopeEnum scope) {
		if (Substitutions.Count > 0) {
			var reps = from ix in Substitutions where (ix.Scope & scope) == scope select ix;
			if (reps.Count() > 0) {
				foreach (var rep in reps) {
					input = rep.Replace(input);
				}
			}
		}
		return input;
	}

	#endregion

	#region Constants

	public SerializableDictionary<string, VariableScalar> Constants { get; set; } = new();

	#endregion

	#region Security

	public class APIUsage {
		[XmlAttribute] public string Name { get; set; }

		[XmlAttribute] public bool AllowLocal { get; set; }

		[XmlAttribute] public bool AllowRemote { get; set; }

		[XmlAttribute] public bool AllowAdmin { get; set; }
	}

	[Flags]
	public enum ScriptUsageEnum {
		None = 0,
		AllowLocal = 1,
		AllowRemote = 2,
		AllowAdmin = 4
	}

	[XmlIgnore] public bool SecuritySettingsLocked { get; set; } = false;

	[XmlIgnore] public List<APIUsage> _APIUsages { get; set; } = [];
	public List<APIUsage> APIUsages {
		get => !SecuritySettingsLocked ? _APIUsages : null;
		set {
			if (!SecuritySettingsLocked) _APIUsages = value;
		}
	}


	private ScriptUsageEnum _UnsafeUsage = ScriptUsageEnum.None;
	public ScriptUsageEnum UnsafeUsage {
		get => _UnsafeUsage;
		set {
			if (!SecuritySettingsLocked) _UnsafeUsage = value;
		}
	}

	private ScriptUsageEnum _DynamicUsage = ScriptUsageEnum.None;
	public ScriptUsageEnum DynamicUsage {
		get => _DynamicUsage;
		set {
			if (!SecuritySettingsLocked) _DynamicUsage = value;
		}
	}

	internal List<APIUsage> GetAPIUsages() {
		var l = new List<APIUsage>();
		foreach (var a in _APIUsages) {
			l.Add(new APIUsage {
				Name = a.Name,
				AllowLocal = a.AllowLocal,
				AllowRemote = a.AllowRemote,
				AllowAdmin = a.AllowAdmin
			});
		}
		return l;
	}

	public void AddAPIUsage(APIUsage au, bool overwrite) {
		var ax = (from aus in _APIUsages where aus.Name.CompareTo(au.Name) == 0 select aus).FirstOrDefault();
		if (ax == null) {
			_APIUsages.Add(au);
		} else if (overwrite) {
			ax.AllowLocal = au.AllowLocal;
			ax.AllowRemote = au.AllowRemote;
			ax.AllowAdmin = au.AllowAdmin;
		}
	}

	private void SetUnsafeUsage(ScriptUsageEnum us) {
		_UnsafeUsage = us;
	}

	private void SetDynamicUsage(ScriptUsageEnum us) {
		_DynamicUsage = us;
	}

	#endregion

	#region Miscellaneous

	// Default Settings

	public Trigger TemplateTrigger = new() {
		Enabled = true,
		Condition = new ConditionGroup {
			Grouping = ConditionGroup.CndGroupingEnum.Or,
			Enabled = false
		}
	};

	[XmlAttribute] public bool UseTemplateTrigger { get; set; }

	// User Interface

	[XmlAttribute] public bool UiFontDefault { get; set; } = true;

	[XmlAttribute] public string UiFontName { get; set; }

	[XmlAttribute] public float UiFontSize { get; set; } = 10.0f;

	[XmlAttribute] public ActionOld.TextAuraEffectEnum UiFontEffect { get; set; } = ActionOld.TextAuraEffectEnum.None;

	[XmlAttribute] public bool TestLiveByDefault { get; set; } = true;

	[XmlAttribute] public bool TestIgnoreConditionsByDefault { get; set; } = true;

	[XmlAttribute] public bool ActionAsyncByDefault { get; set; } = true;

	[XmlAttribute] public bool DeveloperMode { get; set; }

	[XmlAttribute] public bool AutoComplete { get; set; } = true;

	[XmlAttribute] public bool AutosaveEnabled { get; set; }

	[XmlAttribute] public int AutosaveInterval { get; set; } = 5;

	// Aura control

	[XmlAttribute] public string WindowToMonitor { get; set; } = "FINAL FANTASY XIV";

	[XmlAttribute] public bool UseScarborough { get; set; } = true;

	#endregion

	// Others

	#region Test Input

	[XmlAttribute] public int TestInputDestination { get; set; } = -1;

	[XmlAttribute] public int TestInputZoneType { get; set; } = -1;

	#endregion

	#region Main UI / Others

	public Folder Root = new() {
		Name = I18n.Translate("internal/Configuration/local", "Local triggers")
	};

	public RepositoryFolder RepositoryRoot = new() {
		Name = I18n.Translate("internal/Configuration/remote", "Remote triggers")
	};

	public VariableStore PersistentVariables { get; set; } = new();

	public enum UpdateNotificationsEnum {
		Undefined,
		Yes,
		No
	}

	[XmlAttribute] public UpdateNotificationsEnum DefaultRepository { get; set; } = UpdateNotificationsEnum.Undefined;

	[XmlAttribute] public string Language { get; set; }

	#endregion

	[XmlAttribute] public int Version { get; set; } = 1;

	[XmlAttribute] public string PluginVersion { get; set; } = Assembly.GetExecutingAssembly().GetName().Version.ToString();

	[XmlIgnore] public string PrevPluginVersion { get; set; }

	internal bool isnew = true;
	internal DateTime lastWrite = DateTime.Now;
	internal string corruptRecoveryError = "";

	/// <summary>
	///     Unique identifier for the current user, used in custom configurations.
	/// </summary>
	[XmlIgnore] public Guid Id => _id.Value;
	private static readonly Lazy<Guid> _id = new(() => LoadOrCreateUserId());

	public Configuration() {
		FfxivCustomPartyOrder = "19, 1, 21, 3, 32, 37, 24, 6, 28, 33, 40, 20, 2, 22, 4, 30, 29, 34, 39, 23, 5, 31, 38, 25, 7, 27, 26, 35, 36";
		Constants["TelestoEndpoint"] = new VariableScalar("localhost");
		Constants["TelestoPort"] = new VariableScalar("45678");
		Constants["OBSWebsocketEndpoint"] = new VariableScalar("localhost");
		Constants["OBSWebsocketPort"] = new VariableScalar("4455");
		Constants["OBSWebsocketPassword"] = new VariableScalar("");
		Constants["TriggernometryEndpoint"] = new VariableScalar("http://localhost:51423/");
		Constants["XivExtractedCsvPath"] = new VariableScalar("");
	}

	/*
	Paladin		19
	Gladiator	1
	Warrior		21
	Marauder	3
	Dark Knight	32
	Gunbreaker  37
	White Mage	24
	Conjurer	6
	Scholar		28
	Astrologian	33
	Sage        40
	Monk		20
	Pugilist	2
	Dragoon		22
	Lancer		4
	Ninja		30
	Rogue		29
	Samurai		34
	Reaper      39
	Viper       41
	Bard		23
	Archer		5
	Machinist	31
	Dancer      38
	Black Mage	25
	Pictomancer 42
	Thaumaturge	7
	Summoner	27
	Arcanist	26
	Red Mage	35
	Blue Mage	36
	 */

	// in case the ACT folder is given by another user
	private static Guid LoadOrCreateUserId() {
		// location: LocalAppData\Triggernometry\user_guid.txt

		var dir = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"Triggernometry"
		);

		var file = Path.Combine(dir, "user_guid.txt");

		try {
			Directory.CreateDirectory(dir);

			if (File.Exists(file)) {
				var text = File.ReadAllText(file).Trim();

				if (Guid.TryParse(text, out var g)) {
					return g;
				}
			}

			var newId = Guid.NewGuid();
			File.WriteAllText(file, newId.ToString());
			return newId;
		} catch (Exception ex) {
			MessageBox.Show(ex.ToString(), "Triggernometry config", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return Guid.Empty;
		}
	}

	[XmlAttribute] public bool EnableModuleBase { get; set; } = true;
	[XmlAttribute] public bool UseImGui4VfxModule { get; set; } = true;
	[XmlAttribute] public List<string> CompileFailedScripts { get; set; } = [];
	[XmlAttribute] public int LogFlattenMaxCount { get; set; } = 114514;
	[XmlAttribute] public List<string> PostnamazuModuleDisabled { get; set; } = [];
}