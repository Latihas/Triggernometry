using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Triggernometry.Localization;

// ReSharper disable once CheckNamespace
namespace Triggernometry.Core;

public partial class RealPlugin {
	private static readonly HttpClient client = new() {
		Timeout = TimeSpan.FromSeconds(10)
	};

	#region Plugin Update

	internal void CheckForUpdates(bool isManual = false) {
		switch (cfg.UpdateCheckMethod) {
			case Configuration.UpdateCheckMethodEnum.ACT:
				CheckForUpdatesACT();
				break;
			case Configuration.UpdateCheckMethodEnum.Builtin:
				CheckForUpdatesBuiltin(isManual);
				break;
			case Configuration.UpdateCheckMethodEnum.External:
				CheckForUpdatesExternal(cfg.UpdateExternalChannelUrl, isManual);
				break;
		}
	}

	internal void CheckForUpdatesACT() {
		CheckUpdateHook();
	}

	private string _builtInUpdateDownloadUrl;

	/// <summary>
	///     Checks for new versions of Triggernometry by querying GitHub Releases.
	///     This is the built-in update check (legacy).
	/// </summary>
	internal void CheckForUpdatesBuiltin(bool alwaysNotify = false) {
	}

	#endregion Plugin Update

	#region Plugin Update (External)

	/// <summary>
	///     Represents an external update manifest used for Triggernometry auto-updates. <br />
	///     The manifest provides version information, download URLs, and an optional update message displayed to the user.
	/// </summary>
	public class UpdateManifest {
		/// <summary>
		///     The version of the remote plugin file.
		/// </summary>
		[XmlIgnore] public Version Version { get; set; }

		[XmlAttribute("Version")] public string Xml_Version {
			get => Version?.ToString();
			set => Version = Version.Parse(value);
		}

		/// <summary>
		///     (Optional) The minimum version, required to continue running without warnings. <br />
		///     If the current version is lower, an urgent restart prompt may be shown.
		/// </summary>
		[XmlIgnore] public Version LowestAllowedVersion { get; set; }

		[XmlAttribute("LowestAllowedVersion")] public string Xml_LowestAllowedVersion {
			get => LowestAllowedVersion?.ToString();
			set => LowestAllowedVersion = Version.Parse(value);
		}

		/// <summary>
		///     The URL to download the remote plugin DLL.
		/// </summary>
		[XmlAttribute] public string Url { get; set; }

		/// <summary>
		///     (Optional) The URL to download the updated translation file.
		/// </summary>
		[XmlAttribute] public string TranslationUrl { get; set; }

		/// <summary>
		///     (Optional) The message template shown to the user when an update is available. <br />
		///     May contain placeholders {0} (local version) and {1} (remote version). <br />
		///     A default message is used if not provided.
		/// </summary>
		[XmlAttribute] public string Message { get; set; }
	}

	public void CheckForUpdatesExternal(string manifestUrl = null, bool alwaysNotify = false, bool forceAutoUpdate = false) {
	}

	private void UpdatePluginExternal(UpdateManifest um, Version localVersion) {
	}

	public async Task UpdateTranslationExternal() {
	}

	private void UpdateTranslationExternal(UpdateManifest um) {
	}

	public void UpdatePostNamazu(string remoteVersion) {
	}

	#endregion Plugin Update (External)

	#region Repo Update

	public async Task UpdateAllRepositoriesAsync(bool isStartup) {
		await UpdateRepositoriesAsync(cfg.RepositoryRoot.Repositories.Where(r => r.Enabled), isStartup);
	}

	internal async Task UpdateSingleRepositoryAsync(Repository r) {
		var info = I18n.Translate("internal/Plugin/repoupdate", "Going to update {0} repository(s)", 1);
		FilteredAddToLog(DebugLevelEnum.Info, info);
		ShowProgress(-1, info);

		await r.CheckAndUpdateAsync();
		_ = CompleteRepositoryUpdate();
	}

	internal async Task UpdateRepositoriesAsync(IEnumerable<Repository> repos, bool isStartup) {
		if (!repos.Any()) {
			return;
		}
		var trans = I18n.Translate("internal/Plugin/repoupdate", "Going to update {0} repository(s)", repos.Count());
		FilteredAddToLog(DebugLevelEnum.Info, trans);
		ShowProgress(-1, trans);
		var total = repos.Count();
		var completed = 0;
		var progressLock = new object();
		var tasks = repos.Select(async r => {
			// if (ExitEvent.WaitOne(0)) return;
			await r.CheckAndUpdateAsync(isStartup);
			int percent;
			string info;
			lock (progressLock) {
				completed++;
				info = I18n.Translate("internal/Plugin/repoupdatedcount",
					"[{0}/{1}] Updated repository {2} at {3}",
					completed, total, r.Name, r.Address);
				r.AddToLog(DebugLevelEnum.Info, info);
				percent = (int)(100.0 * completed / total);
			}
			ShowProgress(percent, info);
		});
		await Task.WhenAll(tasks);
		_ = CompleteRepositoryUpdate();
	}

	private Task CompleteRepositoryUpdate() {
		var info = I18n.Translate("internal/Plugin/repoupdatecomplete", "Repository update complete");
		Instance.UnfilteredAddToLog(DebugLevelEnum.Info, info);
		ShowProgressWhenComplete(info);
		return Task.CompletedTask;
	}

	#endregion
}