using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using Dalamud.Bindings.ImGui;
using Dalamud.Hooking;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Triggernometry.Core;
using Triggernometry.PluginBridges;
using static Triggernometry.PluginBridges.BridgeNamazu.Modules.VfxModule;
using static Triggernometry.PScript.ScriptUtils;

// ReSharper disable once CheckNamespace
namespace TriggernometryProxy;

public class ProxyPlugin : IActPluginV1 {
	public RealPlugin Instance;
	private int callbackIdCounter;

	public delegate void CustomCallbackDelegate(object o, string param);

	public int RegisterNamedCallback(string name, CustomCallbackDelegate callback, object o, string registrant) {
		lock (this) {
			return Instance.RegisterNamedCallback(name, callback, o, true, registrant);
		}
	}

	// for backward compatibility: auto-detect the registrant
	public int RegisterNamedCallback(string name, CustomCallbackDelegate callback, object o) {
		var registrant = "";

		var callingFrame = new StackTrace().GetFrame(1);
		if (callingFrame != null) {
			var callingMethod = callingFrame.GetMethod();
			var callingMethodName = callingMethod.Name;
			var callingClassName = callingMethod.DeclaringType.FullName;
			registrant = $"{callingClassName}.{callingMethodName}";
		}

		return RegisterNamedCallback(name, callback, o, registrant);
	}

	public void UnregisterNamedCallback(int id) {
		lock (this) {
			Instance.UnregisterNamedCallback(id);
		}
	}

	public void FailsafeRegisterHook(string hookname, string methodname) {
		// this is to prevent errors when users don't shut down ACT in between updates, and the old realplugin is still loaded in
		// (and might not expose the hooks that are expected by a newer version of the proxy)
		try {
			var mi = GetType().GetMethod(methodname);
			var pi = Instance.GetType().GetProperty(hookname);
			var dob = Delegate.CreateDelegate(pi.PropertyType, this, mi);
			pi.SetValue(Instance, dob);
		} catch (Exception) {
			RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Warning, $"FailsafeRegisterHook Failed: {hookname} {methodname}");
		}
	}

	public static dynamic DalamudPlugin;
	public static IDalamudPluginInterface PluginInterface;
	public static IClientState ClientState;
	public static IObjectTable ObjectTable;
	public static IGameGui GameGui;
	public static IFramework Framework;
	public static IGameInteropProvider GameInteropProvider;
	public static ISigScanner SigScanner;
	public static Hook<StaticVfxRemoveDelegate>? StaticVfxRemoveHook;
	public static Hook<ActorVfxRemoveDelegate>? ActorVfxRemoveHook;

	public void InitPlugin(dynamic dalamudPlugin, IDalamudPluginInterface dalamudPluginInterface, IPluginLog log, IClientState clientState, IFramework framework, IGameInteropProvider gameInteropProvider, IObjectTable objectTable, IGameGui gameGui,
		ISigScanner sigScanner, int latestVer) {
		DalamudPlugin = dalamudPlugin;
		PluginInterface = dalamudPluginInterface;
		ClientState = clientState;
		Framework = framework;
		GameInteropProvider = gameInteropProvider;
		ObjectTable = objectTable;
		GameGui = gameGui;
		SigScanner = sigScanner;
		RealPlugin.ResetPlugin(log);
		Instance = RealPlugin.Instance;

		FailsafeRegisterHook("InCombatHook", "InCombat");
		FailsafeRegisterHook("SetCombatStateHook", "SetCombatState");
		FailsafeRegisterHook("CurrentZoneHook", "GetCurrentZone");
		FailsafeRegisterHook("ActiveEncounterHook", "ExportActiveEncounter");
		FailsafeRegisterHook("LastEncounterHook", "ExportLastEncounter");
		FailsafeRegisterHook("EncounterDurationHook", "GetEncounterDuration");
		FailsafeRegisterHook("TtsPlaybackHook", "InvokeTtsMethod");
		FailsafeRegisterHook("SoundPlaybackHook", "InvokeSoundMethod");
		FailsafeRegisterHook("CustomTriggerCheckHook", "HasCustomTriggers");
		FailsafeRegisterHook("CustomTriggerHook", "GetCustomTriggers");
		// FailsafeRegisterHook("CornerShowHook", "ShowCornerNotification");
		// FailsafeRegisterHook("CornerHideHook", "HideCornerNotification");
		// FailsafeRegisterHook("TabLocateHook", "LocateTab");
		FailsafeRegisterHook("InstanceHook", "GetInstance");
		FailsafeRegisterHook("CheckUpdateHook", "CheckForUpdates");
		FailsafeRegisterHook("ActInitedHook", "ActInited");
		FailsafeRegisterHook("ACTEncounterLogHook", "ACTEncounterLog");
		GetPluginNameAndPath();
		ActGlobals.oFormActMain.BeforeLogLineRead += OFormActMain_BeforeLogLineRead;
		ActGlobals.oFormActMain.OnLogLineRead += OFormActMain_OnLogLineRead;
		// ActGlobals.oFormActMain.OnCombatStart += OFormActMain_OnCombatStart;
		// ActGlobals.oFormActMain.OnCombatEnd += OFormActMain_OnCombatEnd;
		PluginInterface.UiBuilder.Draw += DrawScriptBdl;
		ClientState.Logout += OnLogout;
		if (dalamudPlugin.ConfigurationInstance.Version != latestVer) {
			try {
				RealPlugin.Instance.cfg.CompileFailedScripts.Clear();
				Directory.Delete(Path.Combine(RealPlugin.Instance.ConfigPath, "Scripts"), true);
			} catch (Exception ex) {
				log.Warning($"Error Updating Configuration: {ex}");
			}
			framework.RunOnFrameworkThread(() => {
				dalamudPlugin.ConfigurationInstance.Version = latestVer;
				dalamudPlugin.ConfigurationInstance.Save();
			}).Wait();
		}
		Instance.InitPlugin();
		RealPlugin.Instance.InitAura();
	}

	private static void OnLogout(int type, int code) => ClearVfxCache();

	private void DrawScriptBdl() {
		var bdl = ImGui.GetBackgroundDrawList(ImGui.GetMainViewport());
		var now = DateTime.Now.Ticks / 10000;
		lock (ScriptDrawList) {
			foreach (var shape in ScriptDrawList.Where(shape => !shape.toRecycle)) {
				if (now > shape.EndTime) {
					shape.toRecycle = true;
					BDLClearCount++;
					continue;
				}
				bdl.DrawIGShape(shape);
			}
			if (BDLClearCount > 100) {
				ScriptDrawList = ScriptDrawList.Where(i => !i.toRecycle).ToList();
				BDLClearCount = 0;
			}
		}
	}

	// private void OFormActMain_OnCombatStart(bool isImport, CombatToggleEventArgs encounterInfo)
	// {
	//     ExtendedACTEvents(new string[] { "OnCombatStart" });
	// }
	//
	// private void OFormActMain_OnCombatEnd(bool isImport, CombatToggleEventArgs encounterInfo)
	// {
	//     ExtendedACTEvents(new string[] { "OnCombatEnd" });
	// }

	public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText) {
	}

	public void DeInitPlugin() {
		// ActGlobals.oFormActMain.OnCombatEnd -= OFormActMain_OnCombatEnd;
		// ActGlobals.oFormActMain.OnCombatStart -= OFormActMain_OnCombatStart;
		ActGlobals.oFormActMain.OnLogLineRead -= OFormActMain_OnLogLineRead;
		ActGlobals.oFormActMain.BeforeLogLineRead -= OFormActMain_BeforeLogLineRead;
		PluginInterface.UiBuilder.Draw -= DrawScriptBdl;
		ClientState.Logout -= OnLogout;
		StaticVfxRemoveHook.Disable();
		StaticVfxRemoveHook.Dispose();
		ActorVfxRemoveHook.Disable();
		ActorVfxRemoveHook.Dispose();
		RealPlugin.Instance.DeInitAura();
		Instance.DeInitPlugin();
	}

	private void OFormActMain_BeforeLogLineRead(bool isImport, LogLineEventArgs logInfo) {
		Instance.BeforeLogLineRead(isImport, logInfo.originalLogLine, logInfo.detectedZone);
	}

	private void OFormActMain_OnLogLineRead(bool isImport, LogLineEventArgs logInfo) {
		Instance.OnLogLineRead(isImport, logInfo.logLine, logInfo.detectedZone);
	}

	public void GetPluginNameAndPath() {
		Instance.ConfigPath = PluginInterface.ConfigDirectory.ToString();
		Instance.pluginPath = Instance.ConfigPath;
		Instance.pluginName = "Triggernometry";
	}

	public bool InCombat() => ActGlobals.oFormActMain.InCombat;

	public void EndCombat() {
		ActGlobals.oFormActMain.EndCombat(false);
	}

	public void SetCombatState(bool inCombat) {
		if (inCombat) {
			var myName = BridgeFFXIV.GetMyself().GetValue("name").ToString() ?? "Player";
			ActGlobals.oFormActMain.SetEncounter(DateTime.Now, myName, myName);
		} else {
			ActGlobals.oFormActMain.EndCombat(false);
		}
	}

	public string GetCurrentZone() => ActGlobals.oFormActMain.CurrentZone;

	public bool ActInited() =>
		// return ActGlobals.oFormActMain.InitActDone;
		true;

	public string ExportLastEncounter() =>
		// Advanced_Combat_Tracker.FormActMain act = Advanced_Combat_Tracker.ActGlobals.oFormActMain;
		// FieldInfo fi = act.GetType().GetField("defaultTextFormat", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
		// dynamic texf = fi.GetValue(act);
		// if (texf != null)
		// {
		//     int zones = act.ZoneList.Count;
		//     for (int ii = zones - 1; ii >= 0; ii--)
		//     {
		//         int encs = act.ZoneList[ii].Items.Count;
		//         for (int jj = encs - 1; jj >= 1; jj--)
		//         {
		//             if (act.ZoneList[ii].Items[jj] != act.ActiveZone.ActiveEncounter)
		//             {
		//                 return act.GetTextExport(act.ZoneList[ii].Items[jj], texf);
		//             }
		//         }
		//     }
		// }
		"";

	public string ExportActiveEncounter() =>
		// Advanced_Combat_Tracker.FormActMain act = Advanced_Combat_Tracker.ActGlobals.oFormActMain;
		// FieldInfo fi = act.GetType().GetField("defaultTextFormat", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
		// dynamic texf = fi.GetValue(act);
		// return act.GetTextExport(act.ActiveZone.ActiveEncounter, texf);
		"";

	public double GetEncounterDuration() => ActGlobals.oFormActMain.ActiveZone.ActiveEncounter.Duration.TotalSeconds;

	public void InvokeTtsMethod(string tts) {
		// if (ActGlobals.oFormActMain.PlayTtsMethod != null)
		// {
		ActGlobals.oFormActMain.TTS(tts);
		// }
	}

	public void InvokeSoundMethod(string filename, int volume) {
		// if (ActGlobals.oFormActMain.PlaySoundMethod != null)
		// {
		//     ActGlobals.oFormActMain.PlaySoundMethod(filename, volume);
		// }
	}

	public bool HasCustomTriggers() => false;

	public List<RealPlugin.CustomTriggerCategoryProxy> GetCustomTriggers() => [];


	public RealPlugin.PluginWrapper GetInstance(string ActPluginName, string ActPluginType) {
		if (!(ActPluginName == "FFXIV_ACT_Plugin.dll" && ActPluginType == "FFXIV_ACT_Plugin.FFXIV_ACT_Plugin")) {
			// RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Warning, $"PluginWrapper GetInstance Not Implemented: {ActPluginName}|{ActPluginType}");
			return new RealPlugin.PluginWrapper {
				pluginObj = null
			};
		}
		return new RealPlugin.PluginWrapper {
			pluginObj = ActGlobals.oFormActMain.FfxivPlugin
			// PnlInfo = pluginData.pPluginInfo,
			// TabPage = pluginData.tpPluginSpace,
			// PluginFile = pluginData.pluginFile,
			// LblTitle = pluginData.lblPluginTitle,
			// LblStatus = pluginData.lblPluginStatus,
			// BtnX = pluginData.btnXButton,
			// CbxEnabled = pluginData.cbEnabled,
			// FileVersion = pluginData.pluginObj.GetType().Assembly.GetName().Version.ToString(),
			// PluginType = pluginData.pluginObj.GetType().ToString()
		};
		// ActPluginData pluginData = GetPluginDataByType(ActPluginType) ?? GetPluginDataByFileName(ActPluginName);
		// if (pluginData == null)
		// {
		//     return new Triggernometry.RealPlugin.PluginWrapper() { pluginObj = null };
		// }
		// return new Triggernometry.RealPlugin.PluginWrapper() {
		//     pluginObj = pluginData.pluginObj,
		//     PnlInfo = pluginData.pPluginInfo,
		//     TabPage = pluginData.tpPluginSpace,
		//     PluginFile = pluginData.pluginFile,
		//     LblTitle = pluginData.lblPluginTitle,
		//     LblStatus = pluginData.lblPluginStatus,
		//     BtnX = pluginData.btnXButton,
		//     CbxEnabled = pluginData.cbEnabled,
		//     FileVersion = pluginData.pluginObj.GetType().Assembly.GetName().Version.ToString(),
		//     PluginType = pluginData.pluginObj.GetType().ToString()
		// };
	}

	public void CheckForUpdates() {
	}

	/// <summary>
	///     If there is a current active ACT encounter, log the message into the encounter log. <br />
	///     This would only generate a logline in the encounter and would not trigger anything.
	/// </summary>
	/// <param name="message">The message to be logged.</param>
	public void ACTEncounterLog(string message) {
		var mainform = ActGlobals.oFormActMain;
		// var text = $"00|{DateTime.Now:O}|0|{type}:{message}|";
		// ActGlobals.oFormActMain.ParseRawLogLine(false, DateTime.Now, $"{text}");
		if (mainform.InCombat) {
			mainform.ActiveZone.ActiveEncounter.LogLines.Add(new LogLineEntry(DateTime.Now, message, 0xFFF, mainform.GlobalTimeSorter));
		}
	}

	// [Obsolete("Use GetPluginDataByType instead.")]
	// public static ActPluginData GetPluginDataByName(string name) => GetPluginDataByType(name);
	// public static ActPluginData GetPluginDataByType(string name)
	// {
	//     foreach (var plugin in ActGlobals.oFormActMain.ActPlugins)
	//     {
	//         if (plugin.cbEnabled.Checked && plugin.pluginObj?.GetType()?.ToString() == name)
	//         {
	//             return plugin;
	//         }
	//     }
	//     return null;
	// }

	// public static ActPluginData GetPluginDataByFileName(string name)
	// {
	//     foreach (var plugin in ActGlobals.oFormActMain.ActPlugins)
	//     {
	//         if (plugin.cbEnabled.Checked && plugin.pluginFile?.Name == name)
	//         {
	//             return plugin;
	//         }
	//     }
	//     return null;
	// }
}