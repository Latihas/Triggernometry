using System;
using System.Collections.Generic;
using System.Linq;
using Triggernometry.Core.Scripting;
using Triggernometry.Expressions.String.Utils;
using Triggernometry.Localization;

// ReSharper disable once CheckNamespace
namespace Triggernometry.Core;

public partial class RealPlugin {
	public readonly Interpreter scripting= new();
	internal readonly Dictionary<string, object> scriptingStorage = new(StringComparer.OrdinalIgnoreCase);
	private readonly List<Configuration.APIUsage> DefaultAPIUsages = ScriptSecurity.SecurityAPIs.Select(name => new Configuration.APIUsage {
		Name = name,
		AllowLocal = false,
		AllowRemote = false,
		AllowAdmin = false
	}).ToList();

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
                throw new ArgumentException(I18n.Translate("internal/Scripting/callbacknotfound",
                    "Callback function '{0}' was not found.", name));

		if (!(obj is Delegate callback))
                throw new ArgumentException(I18n.Translate("internal/Scripting/storedobjectnotdelegate",
                    "Stored object '{0}' is not a delegate.", name));

		return callback.RawInvoke(args?.ToArray());
	}
}