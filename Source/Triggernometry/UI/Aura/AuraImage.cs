using System;
using System.IO;
using System.Net;
using System.Windows.Forms;
using Triggernometry.Core;

namespace Triggernometry.UI.Aura;

internal sealed class AuraImage : Aura {
    internal string ImageFilenameExpression { get; set; }

    private string _ImageFileName;
    internal string ImageFileName
    {
        get => _ImageFileName;
        set
        {
            if (value != _ImageFileName) {
                Changed = true;
                _ImageFileName = value;
            }
        }
    }

    private PictureBoxSizeMode _Display;
    internal PictureBoxSizeMode Display
    {
        get => _Display;
        set
        {
            if (value != _Display) {
                Changed = true;
                _Display = value;
            }
        }
    }

    public override void Dispose() {
        base.Dispose();
    }

    internal static string GetImageFilename(RealPlugin plug, string ifn) {
        var u = new Uri(ifn);
        if (u.IsFile) {
            return ifn;
        }
        var fn = Path.Combine(plug.ConfigPath, "TriggernometryRemoteImages");
        if (!Directory.Exists(fn)) {
            Directory.CreateDirectory(fn);
        }
        var ext = Path.GetExtension(u.LocalPath);
        fn = Path.Combine(fn, RealPlugin.GenerateHash(u.AbsoluteUri) + Path.GetExtension(u.LocalPath));
        if (File.Exists(fn)) {
            var fi = new FileInfo(fn);
            var dt = DateTime.Now.AddMinutes(0 - plug.cfg.CacheImageExpiry);
            if (fi.LastWriteTime > dt) {
                return fn;
            }
        }
        using (var wc = new WebClient()) {
            wc.Headers["User-Agent"] = "Triggernometry Image Retriever";
            var data = wc.DownloadData(u.AbsoluteUri);
            File.WriteAllBytes(fn, data);
            return fn;
        }
    }
}