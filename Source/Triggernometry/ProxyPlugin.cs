using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using Dalamud.Hooking;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Triggernometry.Core;
using Triggernometry.PluginBridges.BridgeNamazu.Modules;
using Triggernometry.PScript;

// using Costura;

namespace Triggernometry;

public class ProxyPlugin
{
    public RealPlugin Instance;

    // private ActPluginData ActPluginPrevious = null;

    private readonly object CornerLock = new object();
    private bool CornerPopupVisible = false;
    // private Control CornerPopup = null;
    private bool complained;
    private int callbackIdCounter;
    private List<Tuple<int, string, CustomCallbackDelegate, object, string>> queuedRegs = new List<Tuple<int, string, CustomCallbackDelegate, object, string>>();

    public delegate void CustomCallbackDelegate(object o, string param);

    public ProxyPlugin()
    {
        // CosturaUtility.Initialize();
    }

    public int RegisterNamedCallback(string name, CustomCallbackDelegate callback, object o, string registrant)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }
        if (callback == null)
        {
            throw new ArgumentNullException("callback");
        }
        lock (this)
        {
            if (Instance != null)
            {
                return Instance.RegisterNamedCallback(name, callback, o, true, registrant);
            }
            else
            {
                int newid = Interlocked.Decrement(ref callbackIdCounter); // negative IDs for queued registrations to avoid conflict
                queuedRegs.Add(new Tuple<int, string, CustomCallbackDelegate, object, string>(newid, name, callback, o, registrant));
                return newid;
            }
        }
    }

    // for backward compatibility: auto-detect the registrant
    public int RegisterNamedCallback(string name, CustomCallbackDelegate callback, object o)
    {
        string registrant = "";

        StackFrame callingFrame = new StackTrace().GetFrame(1);
        if (callingFrame != null)
        {
            MethodBase callingMethod = callingFrame.GetMethod();
            string callingMethodName = callingMethod.Name;
            string callingClassName = callingMethod.DeclaringType.FullName;
            registrant = $"{callingClassName}.{callingMethodName}";
        }

        return RegisterNamedCallback(name, callback, o, registrant);
    }

    public void UnregisterNamedCallback(int id)
    {
        lock (this)
        {
            if (Instance != null)
            {
                Instance.UnregisterNamedCallback(id);
            }
            else
            {
                foreach (var tuple in queuedRegs.Where(tuple => tuple.Item1 == id).ToList())
                {
                    queuedRegs.Remove(tuple);
                }
            }
        }
    }

    public void FailsafeRegisterHook(string hookname, string methodname)
    {
        // this is to prevent errors when users don't shut down ACT in between updates, and the old realplugin is still loaded in
        // (and might not expose the hooks that are expected by a newer version of the proxy)
        try
        {
            MethodInfo mi = GetType().GetMethod(methodname);
            PropertyInfo pi = Instance.GetType().GetProperty(hookname);
            Delegate dob = Delegate.CreateDelegate(pi.PropertyType, this, mi);
            pi.SetValue(Instance, dob);
            return;
        }
        catch (Exception)
        {
            RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Warning, $"FailsafeRegisterHook Failed: {hookname} {methodname}");
        }
        ComplainAboutReload();
    }

    private void ComplainAboutReload()
    {
        if (complained == true)
        {
            return;
        }
        complained = true;
        Instance.IfYouSeeThisErrorYouNeedToRestartACT();
    }

    public static dynamic DalamudPlugin;
    public static IDalamudPluginInterface PluginInterface;
    public static IClientState ClientState;
    public static IFramework Framework;
    public static IGameInteropProvider GameInteropProvider;
    public static Hook<VfxModule.StaticVfxRemoveDelegate> StaticVfxRemoveHook;
    public static Hook<VfxModule.ActorVfxRemoveDelegate> ActorVfxRemoveHook;

    public void InitPlugin(dynamic dalamudPlugin, IDalamudPluginInterface dalamudPluginInterface, IPluginLog log, IClientState clientState, IFramework framework, IGameInteropProvider gameInteropProvider)
    {
        RealPlugin.ResetPlugin(log);
        DalamudPlugin = dalamudPlugin;
        PluginInterface = dalamudPluginInterface;
        ClientState = clientState;
        Framework = framework;
        GameInteropProvider = gameInteropProvider;
        lock (this)
        {
            Instance = RealPlugin.Instance;
            // register any queued callbacks if the RealPlugin instance was not ready to register previously
            if (queuedRegs.Count > 0)
            {
                // private void RegisterNamedCallback(int id, string name, Delegate callback, object o, string registrant)
                var registerNamedCallbackMethod = Instance.GetType().GetMethod(
                                                      "RegisterNamedCallback",
                                                      BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null,
                                                      new Type[] { typeof(int), typeof(string), typeof(Delegate), typeof(object), typeof(string) },
                                                      null
                                                  ) ?? throw new MissingMethodException("RealPlugin", "RegisterNamedCallback(int, string, Delegate, object, string)");
                foreach (Tuple<int, string, CustomCallbackDelegate, object, string> t in queuedRegs)
                {
                    registerNamedCallbackMethod.Invoke(Instance, new object[] { t.Item1, t.Item2, t.Item3, t.Item4, t.Item5 });
                }
                queuedRegs.Clear();
            }
        }
        // Instance.mainform = ActGlobals.oFormActMain;
        Version iv = typeof(RealPlugin).Assembly.GetName().Version;
        Version ip = typeof(ProxyPlugin).Assembly.GetName().Version;
        if (iv.CompareTo(ip) != 0)
        {
            ComplainAboutReload();
        }
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
        FailsafeRegisterHook("CornerShowHook", "ShowCornerNotification");
        FailsafeRegisterHook("CornerHideHook", "HideCornerNotification");
        FailsafeRegisterHook("TabLocateHook", "LocateTab");
        FailsafeRegisterHook("InstanceHook", "GetInstance");
        FailsafeRegisterHook("CheckUpdateHook", "CheckForUpdates");
        FailsafeRegisterHook("ActInitedHook", "ActInited");
        FailsafeRegisterHook("ACTEncounterLogHook", "ACTEncounterLog");
        GetPluginNameAndPath();
        ActGlobals.oFormActMain.BeforeLogLineRead += OFormActMain_BeforeLogLineRead;
        ActGlobals.oFormActMain.OnLogLineRead += OFormActMain_OnLogLineRead;
        // ActGlobals.oFormActMain.OnCombatStart += OFormActMain_OnCombatStart;
        // ActGlobals.oFormActMain.OnCombatEnd += OFormActMain_OnCombatEnd;
        Instance.InitPlugin();
        foreach (string rt in Directory.GetFiles(Path.Combine(PluginInterface.ConfigDirectory.ToString(), "PScript"), "*.cs", SearchOption.TopDirectoryOnly))
            LoadPScript(Path.GetFileNameWithoutExtension(rt));
    }

    public static void LoadPScript(string t)
    {
        try
        {
            string rs = File.ReadAllText(Path.Combine(PluginInterface.ConfigDirectory.ToString(), "PScript", t + ".cs"));
            if (CSharpScriptCompiler.CompileScript(rs, []))
            {
                Assembly asm;
                using (var memoryStream = new MemoryStream(File.ReadAllBytes(CSharpScriptCompiler.GetScriptDllPath(rs))))
                    asm = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())!.LoadFromStream(memoryStream);
                Type targetInterface = typeof(IScriptBase);
                var scriptTypes = asm.GetTypes()
                                     .Where(type => type is { IsAbstract: false, IsInterface: false } && targetInterface.IsAssignableFrom(type))
                                     .ToList();
                foreach (var type in scriptTypes)
                {
                    var instance = Activator.CreateInstance(type);
                    if (instance is IScriptBase scriptInstance)
                    {
                        scriptInstance.Enabled = !RealPlugin.Instance.cfg.PScriptsDisabled.Contains(t);
                        RealPlugin.LoadedScripts[t] = scriptInstance;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Warning, $"PScript {t} 载入失败: {ex}");
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

    public void LocateTab(TabPage tp)
    {
        // try
        // {
        //     FieldInfo fi = ActGlobals.oFormActMain.GetType().GetField("tc1", BindingFlags.NonPublic | BindingFlags.Instance);
        //     if (fi == null)
        //     {
        //         return;
        //     }
        //     TabControl tc1 = (TabControl)fi.GetValue(ActGlobals.oFormActMain);
        //     foreach (TabPage tp1 in tc1.TabPages)
        //     {
        //         if (tp1.Text == "Plugins")
        //         {
        //             foreach (Control c in tp1.Controls)
        //             {
        //                 if (c.Name == "tcPlugins")
        //                 {
        //                     TabControl tc2 = (TabControl)c;
        //                     foreach (TabPage tp2 in tc2.TabPages)
        //                     {
        //                         if (tp2 == tp)
        //                         {
        //                             tc2.SelectedTab = tp;
        //                         }
        //                     }
        //                 }
        //             }
        //             tc1.SelectedTab = tp1;
        //             return;
        //         }
        //     }
        // }
        // catch (Exception)
        // {
        // }
    }

    public void ShowCornerNotification()
    {
        // if (ActGlobals.oFormActMain.InvokeRequired == true)
        // {
        //     ActGlobals.oFormActMain.Invoke((MethodInvoker)delegate { ShowCornerNotification(); });
        //     return;
        // }
        // lock (CornerLock)
        // {
        //     if (CornerPopupVisible == true)
        //     {
        //         return;
        //     }         
        //     MethodInfo mi = ActGlobals.oFormActMain.GetType().GetMethod("CornerControlAdd");
        //     if (mi != null)
        //     {
        //         CornerPopup = Instance.GetCornerControl();
        //         mi.Invoke(ActGlobals.oFormActMain, new object[] { CornerPopup });
        //         CornerPopupVisible = true;
        //     }
        // }
    }

    public void HideCornerNotification()
    {
        // if (ActGlobals.oFormActMain.InvokeRequired == true)
        // {
        //     ActGlobals.oFormActMain.Invoke((MethodInvoker)delegate { HideCornerNotification(); });
        //     return;
        // }
        // lock (CornerLock)
        // {
        //     if (CornerPopupVisible == false)
        //     {
        //         return;
        //     }
        //     MethodInfo mi = ActGlobals.oFormActMain.GetType().GetMethod("CornerControlRemove");
        //     if (mi != null)
        //     {
        //         mi.Invoke(ActGlobals.oFormActMain, new object[] { CornerPopup });
        //         CornerPopup = null;
        //         CornerPopupVisible = false;
        //     }
        // }
    }

    public void DeInitPlugin()
    {
        // ActGlobals.oFormActMain.OnCombatEnd -= OFormActMain_OnCombatEnd;
        // ActGlobals.oFormActMain.OnCombatStart -= OFormActMain_OnCombatStart;
        ActGlobals.oFormActMain.OnLogLineRead -= OFormActMain_OnLogLineRead;
        ActGlobals.oFormActMain.BeforeLogLineRead -= OFormActMain_BeforeLogLineRead;
        StaticVfxRemoveHook?.Disable();
        StaticVfxRemoveHook?.Dispose();
        ActorVfxRemoveHook?.Disable();
        ActorVfxRemoveHook?.Dispose();
        Instance?.DeInitPlugin();
        Instance = null;
        // HideCornerNotification();
    }

    private void ExtendedACTEvents(string[] data)
    {
        Instance.ExtendedACTEvents(data);
    }

    private void OFormActMain_BeforeLogLineRead(bool isImport, LogLineEventArgs logInfo)
    {
        Instance.BeforeLogLineRead(isImport, logInfo.originalLogLine, logInfo.detectedZone);
    }

    private void OFormActMain_OnLogLineRead(bool isImport, LogLineEventArgs logInfo)
    {
        Instance.OnLogLineRead(isImport, logInfo.logLine, logInfo.detectedZone);
    }

    public void GetPluginNameAndPath()
    {
        Instance.ConfigPath = PluginInterface.ConfigDirectory.ToString();
        Instance.pluginPath = Instance.ConfigPath;
        string name = null;
        // foreach (ActPluginData p in ActGlobals.oFormActMain.ActPlugins)
        // {
        //     if (p.pluginObj == this)
        //     {
        //         name = p.pluginFile.Name;
        //         Instance.pluginPath = p.pluginFile.Directory.FullName;
        //         break;
        //     }
        // }
        if (name == null || name.Trim().Length == 0)
        {
            name = "Triggernometry";
        }
        Instance.pluginName = name;
    }

    public bool InCombat()
    {
        return ActGlobals.oFormActMain.InCombat;
    }

    public void EndCombat()
    {
        ActGlobals.oFormActMain.EndCombat(false);
    }

    public void SetCombatState(bool inCombat)
    {
        if (inCombat)
        {
            string myName = Triggernometry.PluginBridges.BridgeFFXIV.GetMyself()?.GetValue("name").ToString() ?? "Player";
            ActGlobals.oFormActMain.SetEncounter(DateTime.Now, myName, myName);
        }
        else
        {
            ActGlobals.oFormActMain.EndCombat(false);
        }
    }

    public string GetCurrentZone()
    {
        return ActGlobals.oFormActMain.CurrentZone;
    }

    public bool ActInited()
    {
        // return ActGlobals.oFormActMain.InitActDone;
        return true;
    }

    public string ExportLastEncounter()
    {
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
        return "";
    }

    public string ExportActiveEncounter()
    {
        // Advanced_Combat_Tracker.FormActMain act = Advanced_Combat_Tracker.ActGlobals.oFormActMain;
        // FieldInfo fi = act.GetType().GetField("defaultTextFormat", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Instance);
        // dynamic texf = fi.GetValue(act);
        // return act.GetTextExport(act.ActiveZone.ActiveEncounter, texf);
        return "";
    }

    public double GetEncounterDuration()
    {
        return ActGlobals.oFormActMain.ActiveZone.ActiveEncounter.Duration.TotalSeconds;
    }

    public void InvokeTtsMethod(string tts)
    {
        // if (ActGlobals.oFormActMain.PlayTtsMethod != null)
        // {
        ActGlobals.oFormActMain.TTS(tts);
        // }
    }

    public void InvokeSoundMethod(string filename, int volume)
    {
        // if (ActGlobals.oFormActMain.PlaySoundMethod != null)
        // {
        //     ActGlobals.oFormActMain.PlaySoundMethod(filename, volume);
        // }
    }

    public bool HasCustomTriggers()
    {
        // return (ActGlobals.oFormActMain.CustomTriggers.Count > 0);
        return false;
    }

    public List<RealPlugin.CustomTriggerCategoryProxy> GetCustomTriggers()
    {
        List<RealPlugin.CustomTriggerCategoryProxy> alltrigs = new List<RealPlugin.CustomTriggerCategoryProxy>();
        ;
        // var trigs = from ix in ActGlobals.oFormActMain.CustomTriggers
        //             group ix by new { ix.Value.Category, ix.Value.RestrictToCategoryZone } into ixs
        //             select new { Key = ixs.Key, Items = ixs.ToList() };
        // foreach (var trig in trigs)
        // {
        //     Triggernometry.RealPlugin.CustomTriggerCategoryProxy ctp = new Triggernometry.RealPlugin.CustomTriggerCategoryProxy();
        //     ctp.Category = trig.Key.Category;
        //     ctp.RestrictToCategoryZone = trig.Key.RestrictToCategoryZone;
        //     foreach (var tx in trig.Items)
        //     {
        //         Triggernometry.RealPlugin.CustomTriggerProxy ct = new Triggernometry.RealPlugin.CustomTriggerProxy();
        //         ct.Active = tx.Value.Active;
        //         ct.ShortRegexString = tx.Value.ShortRegexString;
        //         ct.SoundData = tx.Value.SoundData;
        //         ct.SoundType = tx.Value.SoundType;
        //         ct.TimerName = tx.Value.TimerName;
        //         ct.Tabbed = tx.Value.Tabbed;
        //         ct.Timer = tx.Value.Timer;
        //         ctp.Items.Add(ct);
        //     }
        //     alltrigs.Add(ctp);
        // }
        return alltrigs;
    }

    public RealPlugin.PluginWrapper GetInstance(string ActPluginName, string ActPluginType)
    {
        if (!(ActPluginName == "FFXIV_ACT_Plugin.dll" && ActPluginType == "FFXIV_ACT_Plugin.FFXIV_ACT_Plugin"))
        {
            RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Warning, $"PluginWrapper GetInstance Not Implemented: {ActPluginName}|{ActPluginType}");
            return new RealPlugin.PluginWrapper() { pluginObj = null };
        }
        return new RealPlugin.PluginWrapper()
        {
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

    public void CheckForUpdates()
    {
        //
    }

    /// <summary>
    /// If there is a current active ACT encounter, log the message into the encounter log. <br />
    /// This would only generate a logline in the encounter and would not trigger anything.
    /// </summary>
    /// <param name="message">The message to be logged.</param>
    public void ACTEncounterLog(string message)
    {
        FormActMain mainform = ActGlobals.oFormActMain;
        // var text = $"00|{DateTime.Now:O}|0|{type}:{message}|";
        // ActGlobals.oFormActMain.ParseRawLogLine(false, DateTime.Now, $"{text}");
        if (mainform.InCombat)
        {
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
