using System;
using System.Collections.Generic;
using System.Linq;
using Triggernometry.Core.Scripting;
using Triggernometry.Expressions.String.Utils;
using Triggernometry.Localization;

// ReSharper disable once CheckNamespace
namespace Triggernometry.Core;

public partial class RealPlugin {
	public Interpreter scripting;
	internal bool scriptingInited = true;
	internal Dictionary<string, object> scriptingStorage = new(StringComparer.OrdinalIgnoreCase);
	private List<Configuration.APIUsage> DefaultAPIUsages = ScriptSecurity.SecurityAPIs.Select(name => new Configuration.APIUsage {
		Name = name,
		AllowLocal = false,
		AllowRemote = false,
		AllowAdmin = false
	}).ToList();

	private void InitScripting() {
		try {
			scripting = new Interpreter();
		} catch (Exception ex) {
			FilteredAddToLog(DebugLevelEnum.Error, I18n.Translate("internal/Plugin/iniscripterror",
				"Error when initializing scripting - try changing plugin load order: {0}", ex.FullMessage()));
		}
	}

	internal List<Configuration.APIUsage> GetDefaultAPIUsages() {
		var l = new List<Configuration.APIUsage>();
		foreach (var a in DefaultAPIUsages) {
			l.Add(new Configuration.APIUsage {
				Name = a.Name,
				AllowLocal = a.AllowLocal,
				AllowRemote = a.AllowRemote,
				AllowAdmin = a.AllowAdmin
			});
		}
		return l;
	}

	private void SetupDefaultSecurity() {
		foreach (var a in DefaultAPIUsages) {
			cfg.AddAPIUsage(a, false);
		}
	}

	public object InvokeStorageCallback(string name, IEnumerable<string> args) {
		if (!Instance.scriptingStorage.TryGetValue(name, out var obj))
			throw new ArgumentException($"未找到回调函数：{name}");

		if (!(obj is Delegate callback))
			throw new ArgumentException($"存储的对象 {name} 不是 Delegate 类型");

		return callback.RawInvoke(args?.ToArray());
	}
}