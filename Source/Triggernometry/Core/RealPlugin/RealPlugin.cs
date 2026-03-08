using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Advanced_Combat_Tracker;
using Dalamud.Plugin.Services;
using Triggernometry.Core.Variables;
using Triggernometry.FFXIV;
using Triggernometry.Localization;
using Triggernometry.PluginBridges;
using Triggernometry.PluginBridges.ExternalTools;
using Triggernometry.PScript;
using Triggernometry.UI.CustomControls;
using Triggernometry.Utilities;
using Font = System.Drawing.Font;

// ReSharper disable once CheckNamespace
namespace Triggernometry.Core;

public partial class RealPlugin {
    public class CustomTriggerProxy {
        public bool Active { get; set; }
        public string ShortRegexString { get; set; }
        public string SoundData { get; set; }
        public int SoundType { get; set; }
        public string TimerName { get; set; }
        public bool Tabbed { get; set; }
        public bool Timer { get; set; }
    }

    public class CustomTriggerCategoryProxy {
        public string Category { get; set; }
        public bool RestrictToCategoryZone { get; set; }
        public List<CustomTriggerProxy> Items = [];
    }

    public class PluginWrapper {
        public object pluginObj { get; set; }
        // public Panel PnlInfo { get; set; }
        // public TabPage TabPage { get; set; }
        // public FileInfo  PluginFile { get; set; }
        // public Label LblTitle { get; set; }
        // public Label LblStatus { get; set; }
        // public Button BtnX { get; set; }
        // public CheckBox CbxEnabled { get; set; }
        public string FileVersion { get; set; } = "99.99.99.99";
        // public string PluginType { get; set; }
    }

    public IntPtr XivProcHandle => Memory.XivProcHandle;

    private delegate void LogLineProcDelegate(LogEvent le);

    public delegate bool SimpleBoolDelegate();

    public delegate void SimpleVoidDelegate();

    public delegate double SimpleDoubleDelegate();

    public delegate string SimpleStringDelegate();

    public delegate void BoolDelegate(bool boolParam);

    public delegate void TabPageDelegate(TabPage tp);

    public delegate void TtsDelegate(string text);

    public delegate void SoundDelegate(string filename, int volume);

    public delegate List<CustomTriggerCategoryProxy> CustomTriggerDelegate();

    public delegate PluginWrapper InstanceDelegate(string ActPluginName, string ActPluginType);

    public delegate void ACTEncounterLogDelegate(string message);

    private Queue<LogEvent> EventQueue = new();
    private ManualResetEvent QueueWakeupEvent;
    public UserInterface ui = UserInterface.Instance;
    [Obsolete("Use ConfigPath")] public string path => ConfigPath;
    public string ConfigPath { get; set; }
    private bool isInitialized { get; set; }
    internal Task EventQueueTask;
    // private TabPage mytp;
    private bool complainAboutReload;
    public string pluginName { get; set; }
    public string pluginPath { get; set; }
    internal Endpoint _ep;
    private bool firstevent = true;
    internal bool isRunningAsAdmin;
    internal string currentZone;
    internal DateTime LastDelayWarning = DateTime.Now;
    public VariableStore sessionvars = new();
    internal ObsController _obs;
    internal LiveSplitController _livesplit;
    internal CancellationTokenSource cts;
    internal object ctslock = new();
    // public Form mainform { get; set; }
    internal int MinX = int.MaxValue, MinY = int.MaxValue, MaxX = int.MinValue, MaxY = int.MinValue;

    public SimpleBoolDelegate InCombatHook { get; set; }
    public SimpleBoolDelegate CustomTriggerCheckHook { get; set; }
    public BoolDelegate SetCombatStateHook { get; set; }
    public SimpleStringDelegate CurrentZoneHook { get; set; }
    public SimpleStringDelegate ActiveEncounterHook { get; set; }
    public SimpleStringDelegate LastEncounterHook { get; set; }
    public SimpleDoubleDelegate EncounterDurationHook { get; set; }
    public TtsDelegate TtsPlaybackHook { get; set; }
    public SoundDelegate SoundPlaybackHook { get; set; }
    public CustomTriggerDelegate CustomTriggerHook { get; set; }
    public static InstanceDelegate InstanceHook { get; set; }
    public SimpleVoidDelegate CornerShowHook { get; set; }
    public SimpleVoidDelegate CornerHideHook { get; set; }
    public TabPageDelegate TabLocateHook { get; set; }
    public SimpleVoidDelegate CheckUpdateHook { get; set; }
    public SimpleBoolDelegate ActInitedHook { get; set; }
    public ACTEncounterLogDelegate ACTEncounterLogHook { get; set; }

    public static RealPlugin _instance;
    public static RealPlugin Instance
    {
        get
        {
            if (_instance == null)
                _instance = new RealPlugin();
            return _instance;
        }
    }

    private static IPluginLog Log;

    public static void ResetPlugin(IPluginLog log) {
        _instance = new RealPlugin();
        Log = log;
    }

    private RealPlugin() {
        ThreadPool.SetMinThreads(10, 10);
        BridgeFFXIV.OnLogEvent += BridgeFFXIV_OnLogEvent;
        _ep = new Endpoint();
        _ep.OnStatusChange += _ep_OnStatusChange;
    }

    internal static Font CreateFontFromDefinition(string name, float size, ActionOld.TextAuraEffectEnum effect) {
        var fs = FontStyle.Regular;
        if ((effect & ActionOld.TextAuraEffectEnum.Bold) == ActionOld.TextAuraEffectEnum.Bold) {
            fs |= FontStyle.Bold;
        }
        if ((effect & ActionOld.TextAuraEffectEnum.Italic) == ActionOld.TextAuraEffectEnum.Italic) {
            fs |= FontStyle.Italic;
        }
        if ((effect & ActionOld.TextAuraEffectEnum.Underline) == ActionOld.TextAuraEffectEnum.Underline) {
            fs |= FontStyle.Underline;
        }
        if ((effect & ActionOld.TextAuraEffectEnum.Strikeout) == ActionOld.TextAuraEffectEnum.Strikeout) {
            fs |= FontStyle.Strikeout;
        }
        return new Font(name, size, fs);
    }

    internal static void ApplyFontOverrideToForm(Form f, Font fnt) {
        foreach (Control c in f.Controls) {
            ApplyFontOverrideToControl(c, fnt);
        }
    }

    internal static void ApplyFontOverrideToControl(Control c, Font fnt) {
        // D
    }

    private void _ep_OnStatusChange(Endpoint.StatusEnum newStatus, string statusDesc) {
        FilteredAddToLog(DebugLevelEnum.Verbose, string.Format("Endpoint ({0}) {1}", newStatus, statusDesc));
    }

    private void BridgeFFXIV_OnLogEvent(DebugLevelEnum level, string text) {
        FilteredAddToLog(level, text);
    }

    public void GenericExceptionHandler(string msg, Exception ex) {
        Log.Error(msg + ex);
        // string text = msg + ": " + Environment.NewLine + Environment.NewLine + ex.FullMessage();
        // MessageBox.Show(ui, text, I18n.Translate("internal/Plugin/exception", "Exception"), MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void FixDuplicateFolderReferences(Dictionary<Guid, List<Folder>> references, Configuration c, Folder f) {
        if (f == null) {
            var existing = new Dictionary<Guid, List<Folder>>();
            foreach (var sf in c.Root.Folders) {
                FixDuplicateFolderReferences(existing, c, sf);
            }
            foreach (var kp in existing) {
                if (kp.Value.Count <= 1) {
                    continue;
                }
                var ori = kp.Value[0];
                foreach (var refe in kp.Value) {
                    if (refe == ori) {
                        continue;
                    }
                    refe.Id = Guid.NewGuid();
                    FilteredAddToLog(DebugLevelEnum.Warning, I18n.Translate("internal/UserInterface/folderidreassign", "Reassigning new id ({0}) for folder ({1}) due to already assigned id ({2}) on folder ({3})", refe.Id, refe.Name, ori.Id, ori.Name));
                }
            }
        }
        else {
            if (!references.ContainsKey(f.Id)) {
                references[f.Id] = [];
            }
            references[f.Id].Add(f);
            foreach (var sf in f.Folders) {
                FixDuplicateFolderReferences(references, null, sf);
            }
        }
    }

    public void InitPlugin() {
        InitLanguage();
        var exwhere = I18n.Translate("internal/Plugin/initseek", "seeking plugin instance");
        try {
            FilteredAddToLog(DebugLevelEnum.Verbose, I18n.Translate("internal/Plugin/initing", "Initializing"));
            //CombobulateTranslations();
            exwhere = I18n.Translate("internal/Plugin/inifilename", "determining filename");
            pluginName = Path.GetFileNameWithoutExtension(pluginName);
            FilteredAddToLog(DebugLevelEnum.Verbose, I18n.Translate("internal/Plugin/filenameis", "Plugin filename is '{0}' at '{1}'", pluginName, pluginPath));
            exwhere = I18n.Translate("internal/Plugin/inilanguages", "loading languages");
            LoadLanguages();
            exwhere = I18n.Translate("internal/Plugin/inicfg", "loading configuration");
            _cfg = LoadConfigFromFile(Path.Combine(ConfigPath, pluginName + ".config.xml"));
            SetupDefaultSecurity();
            AutofixConfiguration();
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            BackupConfiguration();
            FixDuplicateFolderReferences(null, cfg, null);
            BridgeFFXIV.cfg = cfg;
            // start
            /*
            if (cfg.Language != null)
            {
                ChangeLanguage(cfg.Language);
            }
            */
            ChangeLanguage("简体中文 (zh-CN)");
            // end
            exwhere = I18n.Translate("internal/Plugin/iniactui", "setting up ACT ui");
            // mytp = pluginScreenSpace;
            // pluginScreenSpace.Text = "Triggernometry";
            exwhere = I18n.Translate("internal/Plugin/inievents", "creating events");
            QueueWakeupEvent = new ManualResetEvent(false);
            // ActionUpdateEvent = new AutoResetEvent(false);
            exwhere = I18n.Translate("internal/Plugin/iniui", "creating user interface");
            // ui = new CustomControls.UserInterface();
            // ui.btnCornerPopup.Tag = pluginScreenSpace;
            // ui.cfg = cfg;
            // I18n.TranslateControl("Plugin", ui);
            // ui.UpdateUiFont();
            // ui.Dock = DockStyle.Fill;
            // ui.plug = this;
            // pluginScreenSpace.Controls.Add(ui);
            if (cfg.corruptRecoveryError != "") {
                FilteredAddToLog(DebugLevelEnum.Error, cfg.corruptRecoveryError);
            }
            exwhere = I18n.Translate("internal/Plugin/inicache", "performing cache cleanup");
            ClearCache();
            InitAudio(ref exwhere);
            exwhere = I18n.Translate("internal/Plugin/iniwelcome", "preparing welcome");
            // ui.pnlWelcome.Dock = DockStyle.Fill;
            // ui.pnlUi.Dock = DockStyle.Fill;
            FixConfigurationOnStartCN(); // start
            // if (cfg != null && cfg.ShowWelcome == true)
            // {
            //     ui.pnlUi.Visible = false;
            //     ui.pnlWelcome.Visible = true;
            //     ui.btnOptions.Enabled = false;
            // }
            // else
            // {
            //     ui.pnlUi.Visible = true;
            //     ui.pnlWelcome.Visible = false;
            //     ui.btnOptions.Enabled = true;
            // }
            if (cfg.UpdateNotifications == Configuration.UpdateNotificationsEnum.Yes) {
                exwhere = I18n.Translate("internal/Plugin/iniupdates", "checking for updates");
                // CheckForUpdates();
            }
            exwhere = I18n.Translate("internal/Plugin/initoasts", "setting up toasts");
            if (complainAboutReload) {
                // ui.ComplainAboutReload();
            }
            // ui.SetupToasts();
            // ui.SetupLanguageMenu();
            isRunningAsAdmin = CheckIfAdministrator(cfg.WarnAdmin);
            exwhere = I18n.Translate("internal/Plugin/initree", "building internal data");
            // ui.BuildFullTreeFromConfiguration();
            int PrimaryX = 0, PrimaryY = 0;
            foreach (var s in Screen.AllScreens) {
                FilteredAddToLog(DebugLevelEnum.Info, string.Format("{0}{1}: {2},{3} - {4},{5}", s.DeviceName, s.Primary ? " (*)" : "", s.Bounds.Left, s.Bounds.Top, s.Bounds.Left + s.Bounds.Width, s.Bounds.Top + s.Bounds.Height));
                if (s.WorkingArea.Left < MinX) {
                    MinX = s.WorkingArea.Left;
                }
                if (s.WorkingArea.Top < MinY) {
                    MinY = s.WorkingArea.Top;
                }
                if (s.WorkingArea.Left + s.WorkingArea.Width > MaxX) {
                    MaxX = s.WorkingArea.Left + s.WorkingArea.Width;
                }
                if (s.WorkingArea.Top + s.WorkingArea.Height > MaxY) {
                    MaxY = s.WorkingArea.Top + s.WorkingArea.Height;
                }
                if (s.Primary) {
                    PrimaryX = s.WorkingArea.Left;
                    PrimaryY = s.WorkingArea.Top;
                }
            }
            FilteredAddToLog(DebugLevelEnum.Info, string.Format("*: {0},{1} - {2},{3}", MinX, MinY, MaxX, MaxY));
            InitActionQueue();
            var cancellationToken = GetCancellationToken();
            EventQueueTask = Task.Run(() => LogLineProcessorAsync(cancellationToken), cancellationToken);
            // InitAura();
            _obs = new ObsController();
            _livesplit = new LiveSplitController();
            InitScripting();
            exwhere = I18n.Translate("internal/Plugin/iniendpoint", "starting endpoint");
            if (cfg.StartEndpointOnLaunch) {
                _ep.Start();
            }
            // pluginStatusText.Text = I18n.Translate("internal/Plugin/iniready", "Ready");
            FilteredAddToLog(DebugLevelEnum.Info, I18n.Translate("internal/Plugin/inited", "Initialized"));
            // start
            if (I18n.IsChineseEnvironment) AddDefaultRepoCN();
            RegisterDefaultNamedCallbacks();
            // end
            _ = Task.Run(() => UpdateAllRepositoriesAsync(true));
            isInitialized = true;
        }
        catch (Exception ex) {
            Log.Error(I18n.Translate("internal/Plugin/inierror", "Error while {0} ({1})", exwhere, ex.ToString()));
        }
        UserInterface.BuildTriggerTreeFromConfiguration(null, null);
        // RefreshTriggers(null, null, false);
    }

    public static int UProgress = 100;
    public static string UState = "就绪";

    public static void ShowProgress(int progress, string state) {
        UProgress = Math.Max(0, progress);
        UState = state;
        // ui.ShowProgress(progress, state);
    }

    public static void ShowProgressWhenComplete(string state) {
        ShowProgress(100, state);
        // System.Threading.Thread.Sleep(2000);
        // ui.ShowProgress(0, "");
    }

    public void DeInitPlugin() {
        // ui?.CloseForms();
        BridgeFFXIV.UnsubscribeFromNetworkEvents(this);
        if (_ep != null) {
            _ep.Stop();
            _ep.Dispose();
            _ep = null;
        }
        if (_obs != null) {
            _obs.Dispose();
            _obs = null;
        }
        if (_livesplit != null) {
            _livesplit?.Dispose();
            _livesplit = null;
        }
        Memory.DisposeXivProcHandle();
        RefreshCancellationToken();
        if (EventQueueTask != null && !EventQueueTask.IsCompleted) {
            var waitTask = EventQueueTask.WaitAsync(TimeSpan.FromSeconds(5));
            waitTask.GetAwaiter().GetResult();
        }
        EventQueueTask = null;
        DeinitActionQueue();
        // DeInitAura();
        if (QueueWakeupEvent != null) {
            QueueWakeupEvent.Dispose();
            QueueWakeupEvent = null;
        }
        if (!configBroken) {
            SaveCurrentConfig();
        }
        //SaveDefaultLanguage(Path.Combine(path, "default.triglations.xml"));
        if (cts != null) {
            cts.Dispose();
            cts = null;
        }
        DeInitAudio();
        // if (ui != null)
        // {
        //     ui.Dispose();
        //     ui = null;
        // }
        _instance = null;
    }

    public CancellationToken GetCancellationToken() {
        lock (ctslock) {
            cts ??= new CancellationTokenSource();
            return cts.Token;
        }
    }

    internal void RefreshCancellationToken() {
        lock (ctslock) {
            if (cts != null) {
                cts.Cancel();
                cts.Dispose();
            }
            cts = new CancellationTokenSource();
        }
    }

    public void LogLineQueuer(string text, string zone, LogEvent.SourceEnum src) {
        var le = new LogEvent();
        le.Text = text;
        le.ZoneName = zone;
        le.Source = src;
        le.Timestamp = DateTime.Now;
        lock (EventQueue) {
            EventQueue.Enqueue(le);
            QueueWakeupEvent.Set();
        }
    }

    internal void LogLineQueuerMass(IEnumerable<string> text, string zone, LogEvent.SourceEnum src, bool testMode, bool testModeZoneId) {
        var max = text.Count();
        var i = 0;
        var lex = new LogEvent[text.Count()];
        foreach (var x in text) {
            lex[i] = new LogEvent();
            lex[i].Text = x;
            lex[i].ZoneName = zone;
            lex[i].Source = src;
            lex[i].Timestamp = DateTime.Now;
            lex[i].TestMode = testMode;
            lex[i].ZoneId = testModeZoneId ? zone : null;
            i++;
        }
        if (lex.Count() > 0) {
            lock (EventQueue) {
                foreach (var le in lex) {
                    EventQueue.Enqueue(le);
                }
                QueueWakeupEvent.Set();
            }
        }
    }

    private async Task LogLineProcessorAsync(CancellationToken cancellationToken) {
        var lxx = new List<LogEvent>();
        var wh = new WaitHandle[2] {
            cancellationToken.WaitHandle, QueueWakeupEvent
        };
        EventQueue.Clear();
        while (!cancellationToken.IsCancellationRequested) {
            try {
                var waitResult = WaitHandle.WaitAny(wh, Timeout.Infinite);
                switch (waitResult) {
                    case 0:
                        return;
                    case 1:
                        lock (EventQueue) {
                            lxx.AddRange(EventQueue);
                            EventQueue.Clear();
                            QueueWakeupEvent.Reset();
                        }
                        foreach (var lx in lxx) LogLineProcessor(lx);
                        lxx.Clear();
                        break;
                }
            }
            catch (OperationCanceledException) {
                return;
            }
            catch (Exception ex) {
                GenericExceptionHandler("LogLineProcessorAsync error", ex);
            }
        }
    }

    public void LogLineProcessor(LogEvent le) {
        if (firstevent) {
            BridgeFFXIV.SubscribeToZoneChanged(this);
            firstevent = false;
        }
        switch (le.Source) {
            case LogEvent.SourceEnum.Log:
                lock (ActiveTextTriggers) // verified
                {
                    foreach (var t in ActiveTextTriggers) {
                        if (t.ZoneBlocked && !le.TestMode) {
                            continue;
                        }
                        TestTrigger(t, le, ActionOld.TriggerForceTypeEnum.NoSkip);
                    }
                }
                break;
            case LogEvent.SourceEnum.NetworkFFXIV:
                lock (ActiveFFXIVNetworkTriggers) // verified
                {
                    foreach (var t in ActiveFFXIVNetworkTriggers) {
                        if (t.ZoneBlocked && !le.TestMode) {
                            continue;
                        }
                        TestTrigger(t, le, ActionOld.TriggerForceTypeEnum.NoSkip);
                    }
                }
                break;
            case LogEvent.SourceEnum.ACT:
                lock (ActiveACTTriggers) // verified
                {
                    foreach (var t in ActiveACTTriggers) {
                        if (t.ZoneBlocked && !le.TestMode) {
                            continue;
                        }
                        TestTrigger(t, le, ActionOld.TriggerForceTypeEnum.NoSkip);
                    }
                }
                break;
            case LogEvent.SourceEnum.Endpoint:
                lock (ActiveEndpointTriggers) // verified
                {
                    foreach (var t in ActiveEndpointTriggers) {
                        if (t.ZoneBlocked && !le.TestMode) {
                            continue;
                        }
                        TestTrigger(t, le, ActionOld.TriggerForceTypeEnum.NoSkip);
                    }
                }
                break;
        }
        var del = (DateTime.Now - le.Timestamp).TotalMilliseconds;
        if (del > 100.0) {
            if ((DateTime.Now - LastDelayWarning).TotalSeconds > 10.0) {
                FilteredAddToLog(DebugLevelEnum.Warning, I18n.Translate("internal/Plugin/warnprocdelay", "Line ({0}) took {1} ms to process, may be falling behind", le.Text, del));
                LastDelayWarning = DateTime.Now;
            }
        }
    }

    internal void ZoneChanged(string zone) {
        int allowed = 0, restricted = 0;
        lock (Triggers) {
            foreach (var t in Triggers) {
                var block = !t.PassesZoneRestriction(zone);
                t.ZoneBlocked = block;
                if (block) {
                    restricted++;
                }
                else {
                    allowed++;
                }
            }
        }
        FilteredAddToLog(DebugLevelEnum.Verbose, I18n.Translate("internal/Plugin/zoneupdate", "Zone update to '{0}' - allowed triggers: {1}, restricted triggers: {2}", zone, allowed, restricted));
    }

    public void ExtendedACTEvents(string[] data) {
        switch (data[0]) {
            case "OnCombatStart":
            case "OnCombatEnd":
                LogLineQueuer(data[0], currentZone != null ? currentZone : "", LogEvent.SourceEnum.ACT);
                break;
        }
    }

    public void EndpointReceive(string data) {
        var detectedZone = currentZone != null ? currentZone : "";
        try {
            if (cfg.LogEndpoint) {
                FilteredAddToLog(DebugLevelEnum.Verbose, I18n.Translate("internal/Plugin/endpointline", "Endpoint data: ({0})", data));
            }
            LogLineQueuer(data, detectedZone, LogEvent.SourceEnum.Endpoint);
        }
        catch (Exception ex) {
            FilteredAddToLog(DebugLevelEnum.Error, I18n.Translate("internal/Plugin/endpointlineprocex", "Exception ({0}) when processing endpoint data ({1}) in zone ({2})", ex.ToString(), data, detectedZone));
        }
    }

    public void BeforeLogLineRead(bool isImport, string logLine, string detectedZone) {
        if (isImport || !isInitialized) {
            return;
        }
        if (currentZone == null || detectedZone != currentZone) {
            currentZone = detectedZone;
            ZoneChanged(currentZone);
        }
        try {
            if (cfg.FfxivLogNetwork) {
                FilteredAddToLog(DebugLevelEnum.Verbose, I18n.Translate("internal/Plugin/ffxivnetworklogline", "Network log line: ({0})", logLine));
            }
            LogLineQueuer(logLine, detectedZone, LogEvent.SourceEnum.NetworkFFXIV);
        }
        catch (Exception ex) {
            FilteredAddToLog(DebugLevelEnum.Error, I18n.Translate("internal/Plugin/ffxivnetworkprocex", "Exception ({0}) when processing network log line ({1}) in zone ({2})", ex.Message, logLine, detectedZone));
        }
    }

    public void OnLogLineRead(bool isImport, string logLine, string detectedZone) {
        if (isImport || !isInitialized) {
            return;
        }
        if (currentZone == null || detectedZone != currentZone) {
            currentZone = detectedZone;
            ZoneChanged(currentZone);
        }
        try {
            if (logLine != "" && (logLine.Length < 5 || logLine.Substring(logLine.Length - 5) != "] FB:")) {
                if (cfg.LogNormalEvents) {
                    logFlattenACT.Enqueue(logLine);
                    if (logFlattenACT.Count > cfg.LogFlattenMaxCount) logFlattenACT.Dequeue();
                }
                var szone = BridgeFFXIV.ZoneID;
                foreach (var script in ActGlobals.oFormActMain.ActPlugins.Where(i => i.isIScriptBase).Select(i => i.pluginObj as IScriptBase))
                    if (script!.TerritoryIds() == null || script.TerritoryIds() == szone)
                        script.MatchAll(logLine);
                LogLineQueuer(logLine, detectedZone, LogEvent.SourceEnum.Log);
            }
        }
        catch (Exception ex) {
            FilteredAddToLog(DebugLevelEnum.Error, I18n.Translate("internal/Plugin/procex", "Exception ({0}) when processing log line ({1}) in zone ({2})", ex.Message, logLine, detectedZone));
        }
    }

    /// <summary> Invoked by <see cref="Triggernometry.PluginBridges.BridgeFFXIV.SubscribeToZoneChanged" /></summary>
    public void ZoneChangeDelegate(uint ZoneID, string ZoneName) {
        // PluginBridges.BridgeFFXIV.ZoneID = ZoneID;
        BridgeFFXIV.UpdateState(); // fix player id, etc. after travelling to a new server
        Entity.UpdateMySnapshot();
        ZoneChanged(currentZone);
    }

    // public Control GetCornerControl()
    // {
    //     return ui.btnCornerPopup;
    // }

    private void ClearCache() {
        int cleared = 0, clearedt = 0;
        cleared = ClearCache("TriggernometryRemoteImages", cfg.CacheImageExpiry);
        if (cleared > 0) {
            FilteredAddToLog(DebugLevelEnum.Info, I18n.Translate("internal/Plugin/cachecleanimage", "{0} item(s) cleared from image cache with expiry {1}", cleared, cfg.CacheImageExpiry));
            clearedt += cleared;
        }
        cleared = ClearCache("TriggernometryRemoteSounds", cfg.CacheSoundExpiry);
        if (cleared > 0) {
            FilteredAddToLog(DebugLevelEnum.Info, I18n.Translate("internal/Plugin/cachecleansound", "{0} item(s) cleared from sound cache with expiry {1}", cleared, cfg.CacheSoundExpiry));
            clearedt += cleared;
        }
        cleared = ClearCache("TriggernometryJsonCache", cfg.CacheJsonExpiry);
        if (cleared > 0) {
            FilteredAddToLog(DebugLevelEnum.Info, I18n.Translate("internal/Plugin/cachecleanjson", "{0} item(s) cleared from JSON cache with expiry {1}", cleared, cfg.CacheJsonExpiry));
            clearedt += cleared;
        }
        cleared = ClearCache("TriggernometryRepoBackups", cfg.CacheRepoExpiry);
        if (cleared > 0) {
            FilteredAddToLog(DebugLevelEnum.Info, I18n.Translate("internal/Plugin/cachecleanrepo", "{0} item(s) cleared from repository cache with expiry {1}", cleared, cfg.CacheRepoExpiry));
            clearedt += cleared;
        }
        if (clearedt > 0) {
            FilteredAddToLog(DebugLevelEnum.Info, I18n.Translate("internal/Plugin/cacheclean", "Total of {0} cached item(s) cleared", clearedt));
        }
    }

    internal int ClearCache(string cachedir, int expiry) {
        var cachepath = Path.Combine(ConfigPath, cachedir);
        var dt = DateTime.Now.AddMinutes(0 - expiry);
        var di = new DirectoryInfo(cachepath);
        if (di.Exists) {
            var i = 0;
            var fis = di.GetFiles();
            foreach (var fi in fis) {
                if (fi.LastWriteTime < dt) {
                    fi.Delete();
                    i++;
                }
            }
            return i;
        }
        return 0;
    }

    public VariableStore GetVariableStore(bool isPersistent)
        => isPersistent ? cfg.PersistentVariables : sessionvars;
}