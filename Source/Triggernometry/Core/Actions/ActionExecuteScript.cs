using System.Threading.Tasks;
using System.Xml.Serialization;
using Triggernometry.Core.Serialization;
using Triggernometry.Localization;

namespace Triggernometry.Core.Actions;

/// <summary>
///     Script execution
/// </summary>
[ActionCategory(ActionCategory.CategoryTypeEnum.Programming)]
[XmlRoot(ElementName = "ExecuteScript")]
public class ActionExecuteScript : ActionBase {
	#region Properties

	/// <summary>
	///     Comma-separated list of referenced assemblies
	/// </summary>
	[XmlIgnore] [Action(1)] public string Assemblies { get; set; } = "";

	[XmlAttribute("Assemblies")] public string Xml_Assemblies {
		get => XmlAttr.String(Assemblies);
		set => Assemblies = value;
	}

	/// <summary>
	///     Script code expression
	/// </summary>
	[XmlIgnore] [Action(2)] public string Script { get; set; } = "";

	[XmlAttribute("Script")] public string Xml_Script {
		get => XmlAttr.String(Script);
		set => Script = value;
	}

	#endregion


	#region Implementation

	internal override string DescribeImplementation() => I18n.Translate("internal/Action/descexecscript", "execute C# script");

	internal override void ExecuteImplementation(ActionInstance ai) {
		var ctx = ai?.ctx ?? Context.Unbound;
		var plug = ctx.Plugin;
		var scp = ctx.EvaluateStringExpression(ActionContextLogger, ctx, Script);
		var assy = ctx.EvaluateStringExpression(ActionContextLogger, ctx, Assemblies);
		while (!plug.scriptingInited) {
			Task.Delay(10);
		}
		if (plug?.scripting.Ready == true) {
			plug.scripting.Evaluate(scp, assy, ctx);
		} else {
			AddToLog(ctx, RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/Action/scriptinifailed", "Action #{0} on trigger '{1}' not fired, scripting not available", OrderNumber, ctx.Trigger?.LogName ?? "(null)"));
		}
	}

	#endregion

	#region Old Action Converter

	// (this)ActionOld
	public static explicit operator ActionExecuteScript(ActionOld oldAction) {
		var action = new ActionExecuteScript();
		oldAction.CopyCommonPropertiesTo(action);
		action.Assemblies = oldAction._ExecScriptAssembliesExpression;
		action.Script = oldAction._ExecScriptExpression;
		return action;
	}

	// (ActionOld)this
	public static explicit operator ActionOld(ActionExecuteScript action) {
		var oldAction = new ActionOld();
		action.CopyCommonPropertiesTo(oldAction);
		oldAction.ActionType = ActionOld.ActionTypeEnum.ExecuteScript;
		oldAction._ExecScriptAssembliesExpression = action.Assemblies;
		oldAction._ExecScriptExpression = action.Script;
		return oldAction;
	}

	#endregion Old Action Converter
}