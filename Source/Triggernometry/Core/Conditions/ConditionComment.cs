using System.Xml.Serialization;
using Triggernometry.Localization;

namespace Triggernometry.Core.Conditions;

/// <summary> Stores a note in condition order without participating in evaluation. </summary>
public sealed class ConditionComment : ConditionComponent {
	[XmlAttribute]
	public string Text {
		get;
		set {
			if (field == value) return;
			field = value;
			TriggerOnPropertyChange();
		}
	} = "";

	public override string ToString() => string.IsNullOrEmpty(Text)
		? I18n.Translate("internal/ConditionComment/empty", "Comment")
		: Text.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');

	internal override bool CheckCondition(Context ctx, Context.LoggerDelegate logger, object o) =>
		// ConditionGroup skips comments before evaluating its children.
		false;

	internal override ConditionComponent Duplicate() => new ConditionComment { Enabled = Enabled, Text = Text };
}