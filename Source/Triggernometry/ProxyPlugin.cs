using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using Dalamud.Bindings.ImGui;
using Dalamud.Hooking;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Triggernometry.Core;
using Triggernometry.PluginBridges;
using Triggernometry.PluginBridges.BridgeNamazu.Vfx;
using static Triggernometry.PluginBridges.BridgeNamazu.Modules.VfxModule;
using static Triggernometry.PScript.ScriptUtils;

// ReSharper disable once CheckNamespace
namespace TriggernometryProxy;

public class ProxyPlugin : IActPluginV1 {
	public delegate void CustomCallbackDelegate(object o, string param);

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

	public RealPlugin Instance;

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
		Framework.Update -= VfxManager.WorkerLoop;
		ClientState.Logout -= OnLogout;
		StaticVfxRemoveHook?.Disable();
		StaticVfxRemoveHook?.Dispose();
		ActorVfxRemoveHook?.Disable();
		ActorVfxRemoveHook?.Dispose();
		RealPlugin.Instance.DeInitAura();
		Instance.DeInitPlugin();
	}

	public int RegisterNamedCallback(string name, CustomCallbackDelegate callback, object o, string registrant) {
		lock (this)
			return Instance.RegisterNamedCallback(name, callback, o, true, registrant);
	}

	public int RegisterNamedCallback(string name, CustomCallbackDelegate callback, object o) {
		var registrant = "";
		var callingFrame = new StackTrace().GetFrame(1);
		if (callingFrame != null) {
			var callingMethod = callingFrame.GetMethod();
			registrant = $"{callingMethod?.DeclaringType?.FullName}.{callingMethod?.Name}";
		}
		return RegisterNamedCallback(name, callback, o, registrant);
	}

	public void UnregisterNamedCallback(int id) {
		lock (this) Instance.UnregisterNamedCallback(id);
	}

	public void InitPlugin(dynamic dalamudPlugin, IDalamudPluginInterface dalamudPluginInterface, IPluginLog log, IClientState clientState, IFramework framework, IGameInteropProvider gameInteropProvider, IObjectTable objectTable, IGameGui gameGui,
		ISigScanner sigScanner, Action<string> logTick) {
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
		RealPlugin.Instance.InCombatHook = InCombat;
		RealPlugin.Instance.SetCombatStateHook = SetCombatState;
		RealPlugin.Instance.CurrentZoneHook = GetCurrentZone;
		RealPlugin.Instance.ActiveEncounterHook = ExportActiveEncounter;
		RealPlugin.Instance.LastEncounterHook = ExportLastEncounter;
		RealPlugin.Instance.EncounterDurationHook = GetEncounterDuration;
		RealPlugin.Instance.TtsPlaybackHook = InvokeTtsMethod;
		RealPlugin.Instance.SoundPlaybackHook = InvokeSoundMethod;
		RealPlugin.Instance.CustomTriggerCheckHook = HasCustomTriggers;
		RealPlugin.Instance.CustomTriggerHook = GetCustomTriggers;
		RealPlugin.InstanceHook = GetInstance;
		RealPlugin.Instance.CheckUpdateHook = CheckForUpdates;
		RealPlugin.Instance.ActInitedHook = ActInited;
		RealPlugin.Instance.ACTEncounterLogHook = ACTEncounterLog;
		GetPluginNameAndPath();
		ActGlobals.oFormActMain.BeforeLogLineRead += OFormActMain_BeforeLogLineRead;
		ActGlobals.oFormActMain.OnLogLineRead += OFormActMain_OnLogLineRead;
		// ActGlobals.oFormActMain.OnCombatStart += OFormActMain_OnCombatStart;
		// ActGlobals.oFormActMain.OnCombatEnd += OFormActMain_OnCombatEnd;
		PluginInterface.UiBuilder.Draw += DrawScriptBdl;
		ClientState.Logout += OnLogout;
		Framework.Update += VfxManager.WorkerLoop;
		Instance.InitPlugin(logTick);
		RealPlugin.Instance.InitAura();
	}

	private static void OnLogout(int type, int code) => VfxManager.Clear();

	private static void DrawScriptBdl() {
		if (ObjectTable.LocalPlayer == null) return;
		var bdl = ImGui.GetBackgroundDrawList(ImGui.GetMainViewport());
		var now = DateTime.Now.Ticks / 10000;
		lock (ScriptDrawList) {
			foreach (var shape in ScriptDrawList.Where(shape => !shape.toRecycle)) {
				if (now > shape.EndTime || shape.toRemove) {
					shape.toRemove = false;
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

	private void OFormActMain_BeforeLogLineRead(bool isImport, LogLineEventArgs logInfo) =>
		Instance.BeforeLogLineRead(isImport, logInfo.originalLogLine, logInfo.detectedZone);

	private void OFormActMain_OnLogLineRead(bool isImport, LogLineEventArgs logInfo) =>
		Instance.OnLogLineRead(isImport, logInfo.logLine, logInfo.detectedZone);

	public void GetPluginNameAndPath() {
		Instance.ConfigPath = PluginInterface.ConfigDirectory.ToString();
		Instance.pluginPath = Instance.ConfigPath;
		Instance.pluginName = "Triggernometry";
	}

	[SuppressMessage("Performance", "CA1822")]
	public bool InCombat() => ActGlobals.oFormActMain.InCombat;

	[SuppressMessage("Performance", "CA1822")]
	public void EndCombat() => ActGlobals.oFormActMain.EndCombat(false);

	[SuppressMessage("Performance", "CA1822")]
	public void SetCombatState(bool inCombat) {
		if (inCombat) {
			var myName = BridgeFFXIV.GetMyself().GetValue("name").ToString() ?? "Player";
			ActGlobals.oFormActMain.SetEncounter(DateTime.Now, myName, myName);
		} else {
			ActGlobals.oFormActMain.EndCombat(false);
		}
	}

	[SuppressMessage("Performance", "CA1822")]
	public string GetCurrentZone() => ActGlobals.oFormActMain.CurrentZone;

	[SuppressMessage("Performance", "CA1822")]
	public bool ActInited() =>
		// return ActGlobals.oFormActMain.InitActDone;
		true;

	[SuppressMessage("Performance", "CA1822")]
	public string ExportLastEncounter() => "";

	[SuppressMessage("Performance", "CA1822")]
	public string ExportActiveEncounter() => "";

	[SuppressMessage("Performance", "CA1822")]
	public double GetEncounterDuration() => ActGlobals.oFormActMain.ActiveZone == null
		? 0
		: ActGlobals.oFormActMain.ActiveZone.ActiveEncounter.Duration.TotalSeconds;

	[SuppressMessage("Performance", "CA1822")]
	public void InvokeTtsMethod(string tts) {
		ActGlobals.oFormActMain.TTS(tts);
	}

	[SuppressMessage("Performance", "CA1822")]
	public void InvokeSoundMethod(string filename, int volume) {
		// if (ActGlobals.oFormActMain.PlaySoundMethod != null)
		// {
		//     ActGlobals.oFormActMain.PlaySoundMethod(filename, volume);
		// }
	}

	[SuppressMessage("Performance", "CA1822")]
	public bool HasCustomTriggers() => false;

	[SuppressMessage("Performance", "CA1822")]
	public List<RealPlugin.CustomTriggerCategoryProxy> GetCustomTriggers() => [];

	[SuppressMessage("Performance", "CA1822")]
	public RealPlugin.PluginWrapper GetInstance(string ActPluginName, string ActPluginType) => new() {
		pluginObj = ActGlobals.oFormActMain.FfxivPlugin
	};

	[SuppressMessage("Performance", "CA1822")]
	public void CheckForUpdates() {
	}

	[SuppressMessage("Performance", "CA1822")]
	public void ACTEncounterLog(string message) {
		var mainform = ActGlobals.oFormActMain;
		// var text = $"00|{DateTime.Now:O}|0|{type}:{message}|";
		// ActGlobals.oFormActMain.ParseRawLogLine(false, DateTime.Now, $"{text}");
		if (mainform.InCombat)
			mainform.ActiveZone?.ActiveEncounter.LogLines.Add(new LogLineEntry(DateTime.Now, message, 0xFFF, mainform.GlobalTimeSorter));
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