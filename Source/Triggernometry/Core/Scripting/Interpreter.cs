using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Triggernometry.Localization;

namespace Triggernometry.Core.Scripting;

public class Interpreter {
	private readonly ScriptOptions _scriptOptions;

	internal bool Ready;

	internal Interpreter() {
		_scriptOptions = ScriptOptions.Default.AddImports("System");
		foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
			_scriptOptions = _scriptOptions.AddMetadataReferenceFromAssembly(asm);
		}
		Task.Run(Initialize); // takes ~3 s, do it async
	}

	private void Initialize() {
		try {
			Evaluate("int whee;", null, Context.Unbound);
			Ready = true;
		} catch (Exception ex) {
			RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Error,
				I18n.Translate(
					"internal/Plugin/iniscripterror",
					"Error when initializing scripting - try changing plugin load order: {0}",
					ex.Message));
		}
		RealPlugin.Instance.scriptingInited = true;
	}

	public void Evaluate(string rawScript, string extraAssembliesInput, Context ctx) {
		if (!CSharpScriptCompiler.CompileScript(rawScript, true)) return;
		var fn = "";
		try {
			fn = CSharpScriptCompiler.GetScriptDllPath(rawScript);
			Assembly asm;
			using (var memoryStream = new MemoryStream(File.ReadAllBytes(fn))) {
				asm = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())!.LoadFromStream(memoryStream);
			}
			var implementingTypes = asm.GetTypes().Where(t =>
				t is { IsClass: true, IsAbstract: false } &&
				typeof(ITrnNamedCallback).IsAssignableFrom(t)).ToList();
			foreach (var type in implementingTypes) {
				((ITrnNamedCallback)Activator.CreateInstance(type)!).Load();
				RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Info, $"Loaded Dll {type.Name}");
			}
		} catch (Exception e) {
			RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Error, $"Load Dll Failed: {e}");
			RealPlugin.Instance.FilteredAddToLog(RealPlugin.DebugLevelEnum.Error, $"Load Dll Failed: {rawScript}");
			try {
				if (!string.IsNullOrEmpty(fn)) File.Delete(fn);
			} catch {
				//
			}
		}
	}

	/// <summary>
	///     · <paramref name="extraAssembliesInput" />: User-input assemblies, separated with comma
	/// </summary>
	private ScriptOptions BuildScriptOptions(string extraAssembliesInput, Context ctx) {
		var allowUnsafe = ScriptSecurity.IsFeatureAllowedByConfig(ctx.Plugin.cfg.UnsafeUsage, ctx);
		var scriptOptions = _scriptOptions.WithAllowUnsafe(allowUnsafe);

		if (string.IsNullOrEmpty(extraAssembliesInput))
			return scriptOptions;

		var currentAsms = AppDomain.CurrentDomain.GetAssemblies();
		var extraAsmNames = extraAssembliesInput.Split(',')
			.Select(s => s.Trim())
			.Where(name => !string.IsNullOrEmpty(name));

		foreach (var extraAsmName in extraAsmNames) {
			var found = currentAsms.FirstOrDefault(a => a.GetName().Name.Equals(extraAsmName, StringComparison.OrdinalIgnoreCase));

			// try to first load from current assemblies
			// including the assemblies loaded into memory by ACT which were not detected during InitPlugin
			scriptOptions = found != null
				? scriptOptions.AddMetadataReferenceFromAssembly(found)
				: scriptOptions.AddReferences(extraAsmName);
		}

		return scriptOptions;
	}

	private void ExecuteScriptTask(Task task, ScriptGlobs g) {
		try {
			task.Wait();
		} catch (Exception ex) {
			var exList = ex is AggregateException aex
				? aex.Flatten().InnerExceptions.ToList()
				: [ex];

			foreach (var e in exList) {
				g.TriggernometryHelpers.Log(RealPlugin.DebugLevelEnum.Error,
					I18n.Translate(
						"internal/Interpreter/scriptExecutionError",
						"Error occurred during script execution: \n{0}", e.FullMessage()
					)
				);
			}
		}
	}

	private static bool PassedRestrictedApiCheck(Script<object> script, string[] restrictedApis, ScriptGlobs globs) {
		if (restrictedApis == null || restrictedApis.Length == 0)
			return true;

		if (!ScriptSecurity.TryGetViolatingApi(script, out var violatingApi, restrictedApis))
			return true;

		LogBlockedError(globs, violatingApi);
		return false;
	}

	private static bool PassedDynamicUsageCheck(Script<object> script, bool allowDynamic, ScriptGlobs globs) {
		if (allowDynamic) return true;

		if (!ScriptSecurity.ContainsDynamic(script)) return true;

		LogBlockedError(globs, "dynamic");
		return false;
	}

	private static void LogBlockedError(ScriptGlobs globs, string reason) {
		var ctx = globs.TriggernometryHelpers.CurrentContext;
		var triggerName = ctx?.Trigger?.LogName ?? "(null)";
		globs.TriggernometryHelpers.Log(
			RealPlugin.DebugLevelEnum.Error,
			I18n.Translate(
				"internal/Interpreter/scriptblocked",
				"Script execution on trigger {0} blocked due to restricted API/feature: {1}",
				triggerName, reason));
	}
}

public static class ScriptOptionsExtensions {
	public static ScriptOptions AddMetadataReferenceFromAssembly(this ScriptOptions options, Assembly asm) {
		try {
			options = options.AddReferences(asm);
		} catch // fallback
		{
			if (TryCreateReferenceFromRawBytes(asm, out var reference))
				options = options.AddReferences(reference);
		}
		return options;
	}

	private static bool TryCreateReferenceFromRawBytes(Assembly asm, out PortableExecutableReference reference) {
		try {
			var getRawBytesMethod = asm.GetType().GetMethod("GetRawBytes", BindingFlags.Instance | BindingFlags.NonPublic);
			var assemblyBytes = (byte[])getRawBytesMethod.Invoke(asm, null);
			if (assemblyBytes is { Length: > 0 }) {
				reference = MetadataReference.CreateFromImage(assemblyBytes);
				return true;
			}
		} catch {
			//
		}
		reference = null;
		return false;
	}
}