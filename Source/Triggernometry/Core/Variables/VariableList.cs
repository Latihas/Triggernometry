using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Serialization;
using Triggernometry.Expressions.String.Utils;
using Triggernometry.PluginBridges;

namespace Triggernometry.Core.Variables;

[XmlRoot(ElementName = "VariableList")]
public class VariableList : Variable {
	[XmlArrayItem(ElementName = "VariableScalar", Type = typeof(VariableScalar))] [XmlArrayItem(ElementName = "VariableList", Type = typeof(VariableList))] [XmlArrayItem(ElementName = "VariableTable", Type = typeof(VariableTable))]
	public List<Variable> Values { get; set; } = [];

	public VariableList() {
	}

	public VariableList(IEnumerable<string> values) {
		Values = values.Select(v => (Variable)new VariableScalar(v)).ToList();
	}

	public VariableList(IEnumerable<double> values) : this(values.Select(v => v.ToString(CultureInfo.InvariantCulture))) {
	}

	public VariableList(IEnumerable<int> values) : this(values.Select(v => v.ToString(CultureInfo.InvariantCulture))) {
	}

	public int Size => Values.Count;

	public VariableList(IEnumerable<object> objs) {
		foreach (var obj in objs) {
			Values.Add(new VariableScalar {
				Value = obj.ToString()
			});
		}
	}

	public override string ToString() => string.Join(",", Values);

	public override int CompareTo(object o) {
		if (!(o is Variable)) {
			throw new InvalidOperationException();
		}
		if (o is VariableScalar) {
			return 1;
		}
		if (o is VariableList) {
			var v = (VariableList)o;
			if (v.Values.Count > Values.Count) {
				return -1;
			}
			if (v.Values.Count < Values.Count) {
				return 1;
			}
			for (var i = 0; i < Values.Count; i++) {
				var res = Values[i].CompareTo(v.Values[i]);
				if (res != 0) {
					return res;
				}
			}
			return 0;
		}
		return -1;
	}

	private int ProcessRawIndex(int rawIndex) =>
		// rawIndex: starts from 1; could be negative
		rawIndex < 0 ? rawIndex + Size : rawIndex - 1;

	public override Variable Duplicate() {
		var v = new VariableList();
		foreach (var vx in Values) {
			v.Push(vx.Duplicate(), "");
		}
		v.LastChanger = LastChanger;
		v.LastChanged = LastChanged;
		return v;
	}

	public void Push(Variable value, string changer) {
		Values.Add(value);
		LastChanged = DateTime.Now;
		LastChanger = changer;
	}

	public Variable Pop(int rawIndex, string changer) {
		var index = ProcessRawIndex(rawIndex);
		if (index >= 0 && index < Size) {
			var x = Values[index];
			Values.RemoveAt(index);
			LastChanged = DateTime.Now;
			LastChanger = changer;
			return x;
		}
		return new VariableScalar();
	}

	public void Insert(int rawIndex, Variable value, string changer) {
		var idx = ProcessRawIndex(rawIndex);
		if (idx < 0) {
			return;
		}
		if (idx >= Values.Count) {
			var needcre = idx - Values.Count;
			while (needcre > 0) {
				Push(new VariableScalar {
					Value = ""
				}, changer);
				needcre--;
			}
		}
		Values.Insert(idx, value);
		LastChanged = DateTime.Now;
		LastChanger = changer;
	}

	public void Insert(int rawIndex, string value, string changer) {
		Insert(rawIndex, new VariableScalar {
			Value = value
		}, changer);
	}

	public void InsertList(int rawIndex, VariableList srcList, string changer) {
		var idx = ProcessRawIndex(rawIndex);
		if (idx < 0) {
			return;
		}
		if (idx >= Values.Count) {
			var needcre = idx - Values.Count;
			while (needcre > 0) {
				Push(new VariableScalar {
					Value = ""
				}, changer);
				needcre--;
			}
		}
		for (var i = srcList.Size - 1; i >= 0; i--) {
			Values.Insert(idx, srcList.Values[i].Duplicate());
		}
		LastChanged = DateTime.Now;
		LastChanger = changer;
	}

	public void Set(int rawIndex, Variable value, string changer) {
		var idx = ProcessRawIndex(rawIndex);
		if (idx < 0) {
			return;
		}
		if (idx >= Values.Count) {
			var needcre = idx - Values.Count + 1;
			while (needcre > 0) {
				Push(new VariableScalar {
					Value = ""
				}, changer);
				needcre--;
			}
		}
		Values[idx] = value;
		LastChanged = DateTime.Now;
		LastChanger = changer;
	}

	public void Set(int rawIndex, string value, string changer) {
		Set(rawIndex, new VariableScalar {
			Value = value
		}, changer);
	}

	public void Remove(int rawIndex, string changer) {
		var idx = ProcessRawIndex(rawIndex);
		if (idx >= 0 && idx < Values.Count) {
			Values.RemoveAt(idx);
			LastChanged = DateTime.Now;
			LastChanger = changer;
		}
	}

	public Variable Peek(int rawIndex, string defaultValue = "") {
		var idx = ProcessRawIndex(rawIndex);
		if (idx < 0 || idx >= Values.Count) {
			return new VariableScalar(defaultValue);
		}
		return Values[idx] ?? new VariableScalar(defaultValue);
	}

	public void RemoveAll(string changer) {
		Values.Clear();
		LastChanged = DateTime.Now;
		LastChanger = changer;
	}

	public int IndexOf(Variable value) {
		for (var i = 0; i < Values.Count; i++) {
			if (Values[i].CompareTo(value) == 0) {
				return i + 1;
			}
		}
		return 0;
	}

	public int IndexOf(string value) => IndexOf(new VariableScalar {
		Value = value
	});

	public int LastIndexOf(Variable value) {
		for (var i = Values.Count - 1; i >= 0; i--) {
			if (Values[i].CompareTo(value) == 0) {
				return i + 1;
			}
		}
		return 0;
	}

	public int LastIndexOf(string value) => LastIndexOf(new VariableScalar {
		Value = value
	});

	public string IndicesOf(string targetStr, string joiner, List<int> indices) {
		// accepts indices starts from 0; returns indices starts from 1
		var selectedIndices = indices.Where(i => Values[i].ToString() == targetStr).Select(i => i + 1).ToList();
		return string.Join(joiner, selectedIndices);
	}

	public int Count(string str, List<int> indices) {
		return indices.Count(index => Values[index].ToString() == str);
	}

	public double Sum(List<int> indices) {
		double sum = 0;
		foreach (var i in indices) {
			if (double.TryParse(Values[i].ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
				sum += value;
		}
		return sum;
	}

	public string Join(string joiner, List<int> indices) {
		return string.Join(joiner, indices.Select(index => Values[index].ToString()));
	}

	public string Join(string joiner) => string.Join(joiner, Values);

	public void SortAlphaAsc(string changer) {
		Values.Sort((a, b) => a.CompareTo(b));
		LastChanged = DateTime.Now;
		LastChanger = changer;
	}

	public void SortAlphaDesc(string changer) {
		Values.Sort((a, b) => a.CompareTo(b) * -1);
		LastChanged = DateTime.Now;
		LastChanger = changer;
	}

	public void SortNumericAsc(string changer) {
		Values.Sort((a, b) => decimal.Parse(a.ToString()).CompareTo(decimal.Parse(b.ToString())));
		LastChanged = DateTime.Now;
		LastChanger = changer;
	}

	public void SortNumericDesc(string changer) {
		Values.Sort((a, b) => decimal.Parse(a.ToString()).CompareTo(decimal.Parse(b.ToString())) * -1);
		LastChanged = DateTime.Now;
		LastChanger = changer;
	}

	public void SortFfxivPartyAsc(Configuration cfg, string changer) {
		Values.Sort((a, b) => {
			var pa = BridgeFFXIV.GetNamedPartyMember(a.ToString());
			var pb = BridgeFFXIV.GetNamedPartyMember(b.ToString());
			if (cfg.FfxivPartyOrdering == Configuration.FfxivPartyOrderingEnum.CustomSelfFirst) {
				var se = BridgeFFXIV.GetMyself();
				if (se == pa) {
					return -1;
				}
				if (se == pb) {
					return 1;
				}
			}
			var vla = cfg.GetPartyOrderValue(pa.GetValue("jobid").ToString());
			var vlb = cfg.GetPartyOrderValue(pb.GetValue("jobid").ToString());
			if (vla < vlb) {
				return -1;
			}
			if (vla > vlb) {
				return 1;
			}
			return pa.GetValue("name").CompareTo(pb.GetValue("name"));
		});
	}

	public void SortFfxivPartyDesc(Configuration cfg, string changer) {
		Values.Sort((a, b) => {
			var pa = BridgeFFXIV.GetNamedPartyMember(a.ToString());
			var pb = BridgeFFXIV.GetNamedPartyMember(b.ToString());
			if (cfg.FfxivPartyOrdering == Configuration.FfxivPartyOrderingEnum.CustomSelfFirst) {
				var se = BridgeFFXIV.GetMyself();
				if (se == pa) {
					return 1;
				}
				if (se == pb) {
					return -1;
				}
			}
			var vla = cfg.GetPartyOrderValue(pa.GetValue("jobid").ToString());
			var vlb = cfg.GetPartyOrderValue(pb.GetValue("jobid").ToString());
			if (vla < vlb) {
				return 1;
			}
			if (vla > vlb) {
				return -1;
			}
			return pa.GetValue("name").CompareTo(pb.GetValue("name")) * -1;
		});
	}

	public static VariableList Build(string expression, char separator, string changer) {
		// in actions
		var elements = expression.Split(separator);
		var vl = new VariableList();
		foreach (var element in elements) {
			vl.Values.Add(new VariableScalar {
				Value = element
			});
		}
		vl.LastChanger = changer;
		vl.LastChanged = DateTime.Now;
		return vl;
	}

	public static VariableList BuildTemp(string expression) {
		// in expressions: ${?l: 1, 2, 3 [xxx]}
		var elements = ArgHelper.SplitArguments(expression);
		var vl = new VariableList();
		foreach (var element in elements) {
			vl.Values.Add(new VariableScalar {
				Value = element
			});
		}
		return vl;
	}
}