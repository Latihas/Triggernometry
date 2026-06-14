using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Xml.Serialization;
using CsvHelper;
using Triggernometry.Core.Serialization;
using Triggernometry.Core.Variables;
using Triggernometry.Localization;

namespace Triggernometry.Core.Actions;

/// <summary>
///     File system operations
/// </summary>
[ActionCategory(ActionCategory.CategoryTypeEnum.File)]
[XmlRoot(ElementName = "DiskOperation")]
internal class ActionDiskOperation : ActionBase {
	#region Properties

	/// <summary>
	///     File system operations
	/// </summary>
	public enum OperationEnum {
		/// <summary>
		///     Read the contents of a file into a scalar variable
		/// </summary>
		ReadIntoVariable,
		/// <summary>
		///     Read the contents of a file into a list variable, where every line is its own index
		/// </summary>
		ReadIntoListVariable,
		/// <summary>
		///     Read the contents of a CSV file into a table variable
		/// </summary>
		ReadCSVIntoTableVariable
	}

	/// <summary>
	///     Type of the file system operation
	/// </summary>
	[XmlIgnore] [Action(1)] public OperationEnum Operation { get; set; } = OperationEnum.ReadIntoVariable;

	[XmlAttribute("Operation")] public string Xml_Operation {
		get => XmlAttr.Enum(Operation, OperationEnum.ReadIntoVariable);
		set => Operation = XmlAttr.Enum<OperationEnum>(value);
	}

	/// <summary>
	///     File name
	/// </summary>
	[XmlIgnore] [Action(2, specialtype: ActionAttribute.SpecialTypeEnum.FileSelector)]
	public string Filename { get; set; } = "";

	[XmlAttribute("Filename")] public string Xml_Filename {
		get => XmlAttr.String(Filename);
		set => Filename = value;
	}

	/// <summary>
	///     Target variable name
	/// </summary>
	[XmlIgnore] [Action(3)] public string Variable { get; set; } = "";

	[XmlAttribute("Variable")] public string Xml_Variable {
		get => XmlAttr.String(Variable);
		set => Variable = value;
	}

	/// <summary>
	///     If set, instructs Triggernometry to look at its own cache first for the file, reading that instead if found
	///     (applies to remote files)
	/// </summary>
	[XmlIgnore] [Action(4)] public bool UseCache { get; set; }

	[XmlAttribute("UseCache")] public string Xml_UseCache {
		get => XmlAttr.Bool(UseCache, false);
		set => UseCache = XmlAttr.Bool(value);
	}

	/// <summary>
	///     Indicates whether referenced variable is persistent or not
	/// </summary>
	[XmlIgnore] [Action(5)] // todo need to couple this with variable on editor
	public bool Persistent { get; set; }

	[XmlAttribute("Persistent")] public string Xml_Persistent {
		get => XmlAttr.Bool(Persistent, false);
		set => Persistent = XmlAttr.Bool(value);
	}

	#endregion


	#region Implementation

	internal override string DescribeImplementation() {
		var persist = I18n.TrlVarPersist(Persistent);
		var cache = I18n.TrlCacheFile(UseCache);
		switch (Operation) {
			case OperationEnum.ReadIntoListVariable:
				return I18n.Translate(
					"internal/Action/descfilereadlistvar",
					"read file ({0}) lines into {2}list variable ({1}){3}",
					Filename, Variable, persist, cache
				);
			case OperationEnum.ReadIntoVariable:
				return I18n.Translate(
					"internal/Action/descfilereadvar",
					"read file ({0}) lines into {2}scalar variable ({1}){3}",
					Filename, Variable, persist, cache
				);
			case OperationEnum.ReadCSVIntoTableVariable:
				return I18n.Translate(
					"internal/Action/descfilereadcsvtable",
					"read csv file ({0}) into {2}table variable ({1}){3}",
					Filename, Variable, persist, cache
				);
			default:
				return NotImplementedEnumMessage(Operation);
		}
	}

	internal override void ExecuteImplementation(ActionInstance ai) {
		var ctx = ai?.ctx ?? Context.Unbound;
		var plug = ctx.Plugin;

		var filename = ctx.EvaluateStringExpression(ActionContextLogger, ctx, Filename);
		var varname = ctx.EvaluateStringExpression(ActionContextLogger, ctx, Variable);
		var persist = I18n.TrlVarPersist(Persistent);
		var cache = I18n.TrlCacheFile(UseCache);
		var vs = plug.GetVariableStore(Persistent);
		if (Operation == OperationEnum.ReadCSVIntoTableVariable ||
		    Operation == OperationEnum.ReadIntoListVariable ||
		    Operation == OperationEnum.ReadIntoVariable) {
			var u = new Uri(filename);
			if (!u.IsFile) {
				var fn = Path.Combine(plug.ConfigPath, "TriggernometryFileCache");
				if (!Directory.Exists(fn)) {
					Directory.CreateDirectory(fn);
				}
				var ext = Path.GetExtension(u.LocalPath);
				fn = Path.Combine(fn, RealPlugin.GenerateHash(u.AbsoluteUri) + Path.GetExtension(u.LocalPath));
				var fromcache = false;
				if (File.Exists(fn) && UseCache) {
					var fi = new FileInfo(fn);
					var dt = DateTime.Now.AddMinutes(0 - plug.cfg.CacheFileExpiry);
					if (fi.LastWriteTime > dt) {
						filename = fn;
						fromcache = true;
					}
				}
				if (!fromcache) {
					using var wc = new WebClient();
					wc.Headers["User-Agent"] = "Triggernometry File Retriever";
					var data = wc.DownloadData(u.AbsoluteUri);
					File.WriteAllBytes(fn, data);
					filename = fn;
				}
			}
		}
		switch (Operation) {
			case OperationEnum.ReadCSVIntoTableVariable: {
				var data = new List<string[]>();
				var datawidth = 0;
				using (var sr = new StreamReader(filename))
				using (var csv = new CsvReader(sr, CultureInfo.InvariantCulture)) {
					while (csv.Parser.Read()) {
						var x = csv.Parser.Record;
						if (x.Length > datawidth) {
							datawidth = x.Length;
						}
						data.Add(x);
					}
				}
				var vt = vs.GetTableVariable(varname, true);
				if (data.Count > 0 && datawidth > 0) {
					string vtchanger;
					vtchanger = ctx.Trigger != null
						? I18n.Translate("internal/Action/changetagtrigaction", "Trigger '{0}' action '{1}'", ctx.Trigger.LogName, Describe())
						: I18n.Translate("internal/Action/changetagtestmode", "Action '{0}' test mode", Describe());
					vt.Resize(datawidth, data.Count);
					var y = 1;
					foreach (var row in data) {
						for (var x = 0; x < row.Length; x++) {
							vt.Set(x + 1, y, row[x], vtchanger);
						}
						y++;
					}
				}
				AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/filetableset",
					"{2}Table variable ({0}) value read from CSV file ({1})", varname, filename, persist));
			}
				break;
			case OperationEnum.ReadIntoListVariable: {
				var data = File.ReadAllLines(filename);
				lock (vs.List) // verified
				{
					if (!vs.List.TryGetValue(varname, out var x)) {
						x = new VariableList();
						vs.List[varname] = x;
					}

					foreach (var dat in data) {
						x.Push(new VariableScalar {
							Value = dat
						}, "");
					}
					x.LastChanger = ctx.Trigger != null
						? I18n.Translate("internal/Action/changetagtrigaction", "Trigger '{0}' action '{1}'", ctx.Trigger.LogName, Describe())
						: I18n.Translate("internal/Action/changetagtestmode", "Action '{0}' test mode", Describe());
					x.LastChanged = DateTime.Now;
				}
				AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/filelistset",
					"{2}List variable ({0}) value read from file ({1})", varname, filename, persist));
			}
				break;
			case OperationEnum.ReadIntoVariable: {
				var data = File.ReadAllText(filename);
				lock (vs.Scalar) // verified
				{
					if (!vs.Scalar.TryGetValue(varname, out var x)) {
						x = new VariableScalar();
						vs.Scalar[varname] = x;
					}

					x.Value = data;
					if (ctx.Trigger != null) {
						x.LastChanger = I18n.Translate("internal/Action/changetagtrigaction", "Trigger '{0}' action '{1}'", ctx.Trigger.LogName, Describe());
					} else {
						x.LastChanger = I18n.Translate("internal/Action/changetagtestmode", "Action '{0}' test mode", Describe());
					}
					x.LastChanged = DateTime.Now;
				}
				AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/filescalarset",
					"{2}Scalar variable ({0}) value read from file ({1})",
					varname, filename, persist));
			}
				break;
			default:
				throw NotImplementedEnumException(Operation);
		}
	}

	#endregion

	#region Old Action Converter

	// (this)ActionOld
	public static explicit operator ActionDiskOperation(ActionOld oldAction) {
		var action = new ActionDiskOperation();
		oldAction.CopyCommonPropertiesTo(action);
		action.Operation = (OperationEnum)(int)oldAction._DiskFileOp;
		action.Filename = oldAction._DiskFileOpName;
		action.Variable = oldAction._DiskFileOpVar;
		action.UseCache = oldAction._DiskFileCache;
		action.Persistent = oldAction._DiskPersist;
		return action;
	}

	// (ActionOld)this
	public static explicit operator ActionOld(ActionDiskOperation action) {
		var oldAction = new ActionOld();
		action.CopyCommonPropertiesTo(oldAction);
		oldAction.ActionType = ActionOld.ActionTypeEnum.DiskFile;
		oldAction._DiskFileOp = (ActionOld.DiskFileOpEnum)(int)action.Operation;
		oldAction._DiskFileOpName = action.Filename;
		oldAction._DiskFileOpVar = action.Variable;
		oldAction._DiskFileCache = action.UseCache;
		oldAction._DiskPersist = action.Persistent;
		return oldAction;
	}

	#endregion Old Action Converter
}