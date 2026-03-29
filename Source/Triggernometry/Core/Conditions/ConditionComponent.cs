using System.Xml.Serialization;

namespace Triggernometry.Core.Conditions;

[XmlInclude(typeof(ConditionSingle))]
[XmlInclude(typeof(ConditionGroup))]
public abstract class ConditionComponent {
	public delegate void ChangeDelegate(ConditionComponent re);

	public event ChangeDelegate OnPropertyChange;

	private static object lobject = new();
	private static long IdCounter = 1;
	public long Id { get; set; }

	[XmlAttribute] public bool Enabled { get; set; } = true;

	[XmlIgnore] public ConditionGroup Parent { get; set; } = null;

	public void TriggerOnPropertyChange() {
		if (OnPropertyChange != null) {
			OnPropertyChange(this);
		}
	}

	internal abstract bool CheckCondition(Context ctx, Context.LoggerDelegate logger, object o);
	internal abstract ConditionComponent Duplicate();

	public ConditionComponent() {
		lock (lobject) {
			Id = IdCounter;
			IdCounter++;
		}
	}
}