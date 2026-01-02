using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Speech.Synthesis;
using Advanced_Combat_Tracker;
using Triggernometry.Localization;

// ReSharper disable once CheckNamespace
namespace Triggernometry.Core;

public partial class RealPlugin {
    internal Dictionary<string, DateTime> TtsRepetitions = new();
    internal Dictionary<string, DateTime> SoundRepetitions = new();
    internal SpeechSynthesizer tts;
    internal bool WMPUnavailable;

    private void InitAudio(ref string exwhere) {
        exwhere = I18n.Translate("internal/Plugin/iniwmp", "trying to initialize Windows Media Player");
        WMPUnavailable = true;
        exwhere = I18n.Translate("internal/Plugin/initts", "trying to initialize TTS");
        tts = new SpeechSynthesizer();
    }

    private void DeInitAudio() {
        tts?.Dispose();
        tts = null;
    }

    internal void TtsPlaybackAct(Context ctx, ActionOld a) {
        TtsPlaybackSelf(ctx, a);
    }

    internal void SoundPlaybackAct(Context ctx, ActionOld a, string filename) {
        lock (SoundRepetitions) {
            if (!RegisterRepetition(SoundRepetitions, cfg.SoundRepCooldown, filename)) {
                return;
            }
        }
        var vol = ctx.EvaluateNumericExpression(a.ActionContextLogger, ctx, a._PlaySoundVolumeExpression);
        vol *= ctx.Plugin.cfg.SfxVolumeAdjustment / 100.0;
        if (vol < 0.0) {
            vol = 0.0;
        }
        if (vol > 100.0) {
            vol = 100.0;
        }
        SoundPlaybackHook(filename, (int)Math.Floor(vol));
    }

    internal string TtsPlaybackGetTextFromAction(Context ctx, ActionOld a) {
        var text = ctx.EvaluateStringExpression(a.ActionContextLogger, ctx, a._UseTTSTextExpression);
        if (ctx.Plugin != null) {
            text = ctx.Plugin.cfg.PerformSubstitution(text, Configuration.Substitution.SubstitutionScopeEnum.TextToSpeech);
        }
        return text;
    }

    internal void TtsPlaybackSelf(Context ctx, ActionOld a) {
        var text = TtsPlaybackGetTextFromAction(ctx, a);
        lock (TtsRepetitions) {
            if (!RegisterRepetition(TtsRepetitions, cfg.TtsRepCooldown, text)) {
                return;
            }
        }

        ActGlobals.oFormActMain.TTS(text);
    }

    internal void SoundPlaybackSelf(Context ctx, ActionOld a, string filename) {
        lock (SoundRepetitions) {
            if (!RegisterRepetition(SoundRepetitions, cfg.SoundRepCooldown, filename)) {
                return;
            }
        }
        var vol = ctx.EvaluateNumericExpression(a.ActionContextLogger, ctx, a._PlaySoundVolumeExpression);
        vol *= ctx.Plugin.cfg.SfxVolumeAdjustment / 100.0;
        if (vol < 0.0) {
            vol = 0.0;
        }
        if (vol > 100.0) {
            vol = 100.0;
        }
    }

    internal void TtsPlaybackExternal(Context ctx, ActionOld a) {
        var proc = (cfg.TtsExternalApp ?? "").Trim();
        if (proc.Length == 0) {
            return;
        }
        var text = TtsPlaybackGetTextFromAction(ctx, a);
        lock (TtsRepetitions) {
            if (!RegisterRepetition(TtsRepetitions, cfg.TtsRepCooldown, text)) {
                return;
            }
        }
        var args = cfg.TtsExternalAppArgs ?? "";
        args = args.Replace("$source", text);
        Process.Start(proc, args);
    }

    internal void SoundPlaybackExternal(Context ctx, ActionOld a, string filename) {
        lock (SoundRepetitions) {
            if (!RegisterRepetition(SoundRepetitions, cfg.SoundRepCooldown, filename)) {
                return;
            }
        }
        var proc = (cfg.SoundExternalApp ?? "").Trim();
        if (proc.Length == 0) {
            return;
        }
        var args = cfg.SoundExternalAppArgs ?? "";
        args = args.Replace("$source", filename);
        Process.Start(proc, args);
    }

    internal bool RegisterRepetition(Dictionary<string, DateTime> repstore, int cooldown, string item) {
        if (cooldown == 0) {
            return true;
        }
        if (repstore.TryGetValue(item, out var last)) {
            if (last.AddMilliseconds(cooldown) > DateTime.Now) {
                return false;
            }
        }
        repstore[item] = DateTime.Now;
        // clean old entries while we're here
        var olds = (from rx in repstore where rx.Value.AddMilliseconds(cooldown) < DateTime.Now select rx.Key).ToList();
        foreach (var old in olds) {
            repstore.Remove(old);
        }
        return true;
    }

    public void TtsPlaybackSmart(Context ctx, ActionOld a) {
        switch (a._TTSRouting) {
            case Configuration.AudioRoutingMethodEnum.None:
                if (cfg.TtsMethod == Configuration.AudioRoutingMethodEnum.ExternalApplication) {
                    TtsPlaybackExternal(ctx, a);
                }
                else if (cfg.TtsMethod == Configuration.AudioRoutingMethodEnum.ACT) {
                    TtsPlaybackAct(ctx, a);
                }
                else if (cfg.TtsMethod == Configuration.AudioRoutingMethodEnum.Triggernometry) {
                    TtsPlaybackSelf(ctx, a);
                }
                break;
            case Configuration.AudioRoutingMethodEnum.ACT:
                TtsPlaybackAct(ctx, a);
                break;
            case Configuration.AudioRoutingMethodEnum.Triggernometry:
                TtsPlaybackSelf(ctx, a);
                break;
        }
    }

    public void SoundPlaybackSmart(Context ctx, ActionOld a) {
        var filename = ctx.EvaluateStringExpression(a.ActionContextLogger, ctx, a._PlaySoundFileExpression);
        var u = new Uri(filename);
        if (!u.IsFile) {
            var fn = Path.Combine(ConfigPath, "TriggernometryRemoteSounds");
            if (!Directory.Exists(fn)) {
                Directory.CreateDirectory(fn);
            }
            var ext = Path.GetExtension(u.LocalPath);
            fn = Path.Combine(fn, GenerateHash(u.AbsoluteUri) + Path.GetExtension(u.LocalPath));
            var fromcache = false;
            if (File.Exists(fn)) {
                var fi = new FileInfo(fn);
                var dt = DateTime.Now.AddMinutes(0 - cfg.CacheSoundExpiry);
                if (fi.LastWriteTime > dt) {
                    filename = fn;
                    fromcache = true;
                }
            }
            if (!fromcache) {
                using (var wc = new WebClient()) {
                    wc.Headers["User-Agent"] = "Triggernometry Sound Retriever";
                    var data = wc.DownloadData(u.AbsoluteUri);
                    File.WriteAllBytes(fn, data);
                    filename = fn;
                }
            }
        }
        switch (a._SoundRouting) {
            case Configuration.AudioRoutingMethodEnum.None:
                if (cfg.SoundMethod == Configuration.AudioRoutingMethodEnum.ExternalApplication) {
                    SoundPlaybackExternal(ctx, a, filename);
                }
                else if (WMPUnavailable || cfg.SoundMethod == Configuration.AudioRoutingMethodEnum.ACT) {
                    SoundPlaybackAct(ctx, a, filename);
                }
                else if (cfg.SoundMethod == Configuration.AudioRoutingMethodEnum.Triggernometry) {
                    SoundPlaybackSelf(ctx, a, filename);
                }
                break;
            case Configuration.AudioRoutingMethodEnum.ACT:
                SoundPlaybackAct(ctx, a, filename);
                break;
            case Configuration.AudioRoutingMethodEnum.Triggernometry:
                SoundPlaybackSelf(ctx, a, filename);
                break;
        }
    }
}