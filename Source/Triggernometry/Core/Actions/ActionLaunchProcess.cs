using System.Diagnostics;
using System.Xml.Serialization;
using Triggernometry.Core.Serialization;
using Triggernometry.Localization;

namespace Triggernometry.Core.Actions;

/// <summary>
///     Launch process
/// </summary>
[ActionCategory(ActionCategory.CategoryTypeEnum.Programming)]
[XmlRoot(ElementName = "LaunchProcess")]
    public class ActionLaunchProcess : ActionBase {

	#region Properties

	/// <summary>
	///     Window style to launch the process with
	/// </summary>
	[XmlIgnore] [Action(1)] public ProcessWindowStyle WindowStyle { get; set; } = ProcessWindowStyle.Normal;

	[XmlAttribute("WindowStyle")] public string Xml_WindowStyle {
		get => XmlAttr.Enum(WindowStyle, ProcessWindowStyle.Normal);
		set => WindowStyle = XmlAttr.Enum<ProcessWindowStyle>(value);
	}

	/// <summary>
	///     Path to the process to launch
	/// </summary>
	[XmlIgnore] [Action(2, specialtype: ActionAttribute.SpecialTypeEnum.ExecutableSelector)]
	public string Path { get; set; } = "";

	[XmlAttribute("Path")] public string Xml_Path {
		get => XmlAttr.String(Path);
		set => Path = value;
	}

	/// <summary>
	///     Command line arguments to pass to the process
	/// </summary>
	[XmlIgnore] [Action(3)] public string Arguments { get; set; } = "";

	[XmlAttribute("Arguments")] public string Xml_Arguments {
		get => XmlAttr.String(Arguments);
		set => Arguments = value;
	}

	/// <summary>
	///     Working directory
	/// </summary>
	[XmlIgnore] [Action(4)] public string WorkingDirectory { get; set; } = "";

	[XmlAttribute("WorkingDirectory")] public string Xml_WorkingDirectory {
		get => XmlAttr.String(WorkingDirectory);
		set => WorkingDirectory = value;
	}

	#endregion


	#region Implementation

	internal override string DescribeImplementation() {
		var tempt = "";
		switch (WindowStyle) {
			case ProcessWindowStyle.Hidden:
				tempt = I18n.Lookup("ActionForm/cbxProcessWindowStyle[Hidden from view]", WindowStyle.ToString());
				break;
			case ProcessWindowStyle.Maximized:
				tempt = I18n.Lookup("ActionForm/cbxProcessWindowStyle[Maximized to fullscreen]", WindowStyle.ToString());
				break;
			case ProcessWindowStyle.Minimized:
				tempt = I18n.Lookup("ActionForm/cbxProcessWindowStyle[Minimized to taskbar]", WindowStyle.ToString());
				break;
			case ProcessWindowStyle.Normal:
				tempt = I18n.Lookup("ActionForm/cbxProcessWindowStyle[Normal]", WindowStyle.ToString());
				break;
			default:
				return NotImplementedEnumMessage(WindowStyle);
		}
		return I18n.Translate("internal/Action/desclaunchprocess", "launch process ({0}) as ({1}) using command line parameters ({2})",
			Xml_Path,
			tempt,
			Xml_Arguments
		);
	}

	internal override void ExecuteImplementation(ActionInstance ai) {
		var ctx = ai?.ctx ?? Context.Unbound;
		var plug = ctx.Plugin;

		var p = new Process();
		var psi = new ProcessStartInfo {
			Arguments = ctx.EvaluateStringExpression(ActionContextLogger, ctx, Arguments),
			WindowStyle = WindowStyle,
			WorkingDirectory = ctx.EvaluateStringExpression(ActionContextLogger, ctx, WorkingDirectory),
			FileName = ctx.EvaluateStringExpression(ActionContextLogger, ctx, Path)
		};
		p.StartInfo = psi;
		p.Start();
		if (!Asynchronous) {
			AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/waitingprocexit", "Waiting for process to exit"));
			p.WaitForExit();
		}
	}

	#endregion

	#region Old Action Converter

	// (this)ActionOld
	public static explicit operator ActionLaunchProcess(ActionOld oldAction) {
		var action = new ActionLaunchProcess();
		oldAction.CopyCommonPropertiesTo(action);
		action.WindowStyle = oldAction._LaunchProcessWindowStyle;
		action.Path = oldAction._LaunchProcessPathExpression;
		action.Arguments = oldAction._LaunchProcessCmdlineExpression;
		action.WorkingDirectory = oldAction._LaunchProcessWorkingDirExpression;
		return action;
	}

	// (ActionOld)this
	public static explicit operator ActionOld(ActionLaunchProcess action) {
		var oldAction = new ActionOld();
		action.CopyCommonPropertiesTo(oldAction);
		oldAction.ActionType = ActionOld.ActionTypeEnum.LaunchProcess;
		oldAction._LaunchProcessWindowStyle = action.WindowStyle;
		oldAction._LaunchProcessPathExpression = action.Path;
		oldAction._LaunchProcessCmdlineExpression = action.Arguments;
		oldAction._LaunchProcessWorkingDirExpression = action.WorkingDirectory;
		return oldAction;
	}

	#endregion Old Action Converter
}