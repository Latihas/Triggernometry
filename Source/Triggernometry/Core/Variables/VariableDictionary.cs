using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Serialization;
using Triggernometry.Expressions.String.Utils;
using Triggernometry.Localization;

namespace Triggernometry.Core.Variables;

[XmlRoot(ElementName = "VariableDictionary")]
public sealed class VariableDictionary : Variable {
    private Dictionary<string, Variable> _values = new(StringComparer.OrdinalIgnoreCase);

    [XmlIgnore] public Dictionary<string, Variable> Values
    {
        get => _values;
        set
        {
            _values = value?.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase)
                      ?? new Dictionary<string, Variable>(StringComparer.OrdinalIgnoreCase);
        }
    }

    [XmlArray("Items")] [XmlArrayItem("Item")]
    public KeyValue[] KeyValuePairs
    {
        get
        {
            var list = new List<KeyValue>();
            foreach (var pair in Values) {
                list.Add(new KeyValue {
                    Key = pair.Key,
                    Value = pair.Value
                });
            }
            return list.ToArray();
        }
        set
        {
            Values.Clear();
            foreach (var kv in value) {
                Values[kv.Key] = kv.Value;
            }
        }
    }

    public class KeyValue {
        [XmlElement("Key")] public string Key { get; set; }

        [XmlElement("Value")] public Variable Value { get; set; }
    }

    public VariableDictionary() {
    }

    public VariableDictionary(Dictionary<string, string> dict) {
        Values = dict.ToDictionary(
            p => p.Key,
            p => (Variable)new VariableScalar(p.Value)
        );
    }

    public int Size => Values.Count;

    private const string DEFAULTCHANGER = "VariableDictionary";

    public override string ToString() {
        return string.Join(",", Values.Select(pair => $"{pair.Key}={pair.Value}"));
    }

    public override int CompareTo(object o) {
        if (!(o is Variable)) {
            throw new InvalidOperationException();
        }
        if (o is VariableScalar) {
            return 1;
        }
        if (o is VariableList) {
            return 1;
        }
        if (o is VariableDictionary) {
            var v = (VariableDictionary)o;
            if (v.Values.Keys.Count > Values.Keys.Count) {
                return -1;
            }
            if (v.Values.Keys.Count < Values.Keys.Count) {
                return 1;
            }
            var a = new List<string>(Values.Keys);
            var b = new List<string>(v.Values.Keys);
            a.Sort();
            b.Sort();
            for (var i = 0; i < a.Count; i++) {
                var res = a[i].CompareTo(b[i]);
                if (res != 0) {
                    return res;
                }
                res = Values[a[i]].CompareTo(v.Values[a[i]]);
                if (res != 0) {
                    return res;
                }
            }
            return 0;
        }
        return -1;
    }

    public override Variable Duplicate() {
        var v = new VariableDictionary();
        foreach (var kp in Values) {
            v.SetValue(kp.Key, kp.Value.Duplicate());
        }
        v.LastChanger = LastChanger;
        v.LastChanged = LastChanged;
        return v;
    }

    public Variable GetValue(string id)
        => Values.TryGetValue(id, out var result) ? result : new VariableScalar();

    public string GetStringValue(string id)
        => Values.TryGetValue(id, out var result) ? result.ToString() : "";

    public void SetValue(string id, int val, string changer = DEFAULTCHANGER) {
        InternalSetValue(id, new VariableScalar {
            Value = I18n.ThingToString(val)
        }, changer);
    }

    public void SetValue(string id, float val, string changer = DEFAULTCHANGER) {
        InternalSetValue(id, new VariableScalar {
            Value = I18n.ThingToString(val)
        }, changer);
    }

    public void SetValue(string id, double val, string changer = DEFAULTCHANGER) {
        InternalSetValue(id, new VariableScalar {
            Value = I18n.ThingToString(val)
        }, changer);
    }

    public void SetValue(string id, string val, string changer = DEFAULTCHANGER) {
        InternalSetValue(id, new VariableScalar {
            Value = val
        }, changer);
    }

    public void SetValue(string id, Variable val, string changer = DEFAULTCHANGER) {
        InternalSetValue(id, val.Duplicate(), changer);
    }

    private void InternalSetValue(string id, Variable val, string changer) {
        Values[id] = val;
        LastChanged = DateTime.Now;
        LastChanger = changer;
    }

    public void RemoveKey(string key, string changer) {
        if (Values.ContainsKey(key)) {
            Values.Remove(key);
            LastChanged = DateTime.Now;
            LastChanger = changer;
        }
    }

    public bool ContainsKey(string key) => Values.ContainsKey(key);

    public bool ContainsValue(string value) {
        return Values.Values.Any(var => var.ToString() == value);
    }

    public int Count(string str) {
        return Values.Count(pair => pair.Value.ToString() == str);
    }

    public double Sum() {
        double sum = 0;
        foreach (var varValue in Values.Values) {
            if (double.TryParse(varValue.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                sum += value;
        }
        return sum;
    }

    public double SumKeys() {
        double sum = 0;
        foreach (var key in Values.Keys) {
            if (double.TryParse(key, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                sum += value;
        }
        return sum;
    }

    public double Min(double initValue) {
        var min = initValue;
        foreach (var item in Values.Values) {
            if (double.TryParse(item.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var num)) {
                if (num < min) {
                    min = num;
                }
            }
        }
        return min;
    }

    public string KeyOf(string value) {
        var keys = Values.Where(pair => pair.Value.ToString() == value)
            .Select(pair => pair.Key);
        return keys.FirstOrDefault() ?? "";
    }

    public string KeysOf(string value, string joiner) {
        var keys = Values.Where(pair => pair.Value.ToString() == value)
            .Select(pair => pair.Key);
        return string.Join(joiner, keys);
    }

    public string JoinKeys(string joiner) => string.Join(joiner, Values.Keys);

    public string JoinValues(string joiner) {
        return string.Join(joiner, Values.Values.Select(v => v.ToString()));
    }

    public string JoinValues(string joiner, IEnumerable<string> keys) {
        return string.Join(joiner, keys.Select(k => Values.TryGetValue(k, out var v) ? v.ToString() : ""));
    }

        public string JoinValues(string joiner, params string[] keys) => JoinValues(joiner, (IEnumerable<string>)keys);

    public string JoinAll(string kvJoiner, string pairJoiner) {
        return string.Join(pairJoiner, Values.Select(pair => $"{pair.Key}{kvJoiner}{pair.Value}"));
    }

    public string JoinAll(string kvJoiner, string pairJoiner, IEnumerable<string> keys) {
        return string.Join(pairJoiner, keys.Select(k => Values.TryGetValue(k, out var v) ? $"{k}{kvJoiner}{v}" : ""));
    }

        public string JoinAll(string kvJoiner, string pairJoiner, params string[] keys) => JoinAll(kvJoiner, pairJoiner, (IEnumerable<string>)keys);

    public void Merge(VariableDictionary sourceDict, bool overwriteExistingKeys = true) {
        foreach (var pair in sourceDict.Values) {
            if (overwriteExistingKeys || !Values.ContainsKey(pair.Key)) {
                Values[pair.Key] = pair.Value;
            }
        }
    }

    public static VariableDictionary Build(string expression, char kvSeparator, char pairSeparator, string changer) {
        // in actions
        var pairs = expression.Split(pairSeparator);
        var vd = new VariableDictionary();
        foreach (var pair in pairs) {
            var sepIndex = pair.IndexOf(kvSeparator);
            var sep = sepIndex >= 0;
            var k = sep ? pair.Substring(0, sepIndex) : pair;
            var v = sep ? pair.Substring(sepIndex + 1) : "";
            vd.SetValue(k, v, changer);
        }
        return vd;
    }

    public static VariableDictionary BuildTemp(string expression) {
        // in expressions: ${?d: a=1, b=2, c=3 [xxx][xxx]}
        var pairs = ArgHelper.SplitArguments(expression);
        var vd = new VariableDictionary();
        foreach (var pair in pairs) {
            var kv = ArgHelper.SplitArguments(pair + "=", separator: "="); // in case only a key was given
            vd.Values[kv[0]] = new VariableScalar {
                Value = kv[1]
            };
        }
        return vd;
    }
}