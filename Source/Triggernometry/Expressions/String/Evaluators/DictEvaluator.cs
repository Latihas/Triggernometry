using System;
using System.Linq;
using Triggernometry.Core.Variables;
using Triggernometry.Expressions.String.Models;
using Triggernometry.Localization;
using static Triggernometry.Expressions.String.Utils.ArgHelper;

namespace Triggernometry.Expressions.String.Evaluators;

internal static class DictEvaluator {
	internal static Func<VariableDictionary, string> BuildEvaluator(IndexMemberExpression expr) {
		// invalid (only name)
		if (expr.Indexes.Length == 0 && !expr.Member.HasValue)
			throw new Exception($"Dictionary variable must include an index or property in expression '{expr.RawExpression}'.");

		// dvar:Name[Key]
		if (expr.Indexes.Length > 0) {
			var key = expr.Index;
			return vd => vd.GetValue(key).ToString();
		}

		// dvar:Name.Method(Args)
		var methodName = expr.Member.Name.ToLowerInvariant();
		var args = expr.Member.Args;

		void CheckArgCountLocal(string argCountRule) {
			CheckArgCount(argCountRule, args.Length, methodName, expr.RawExpression);
		}

		switch (methodName) {
			case "size":
			case "length":
				CheckArgCountLocal("0");
				return vd => vd.Size.ToString();

			case "ekey":
			case "evalue":
				CheckArgCountLocal("1");
			{
				var queryStr = args[0];
				var checkKey = methodName == "ekey";
				return vd => {
					var exist = checkKey ? vd.ContainsKey(queryStr) : vd.ContainsValue(queryStr);
					return exist ? "1" : "0";
				};
			}

			case "ifekey":
			case "ifevalue":
				CheckArgCountLocal("3");
			{
				var queryStr = args[0];
				var trueStr = args[1];
				var falseStr = args[2];
				var checkKey = methodName == "ifekey";
				return vd => {
					var exist = checkKey ? vd.ContainsKey(queryStr) : vd.ContainsValue(queryStr);
					return exist ? trueStr : falseStr;
				};
			}
			case "count": // count(value)
				CheckArgCountLocal("1");
			{
				var value = args[0];
				return vd => vd.Count(value).ToString();
			}

			case "get":
				CheckArgCountLocal("2");
			{
				var key = args[0];
				var defaultValue = args[1];
				return vd => vd.Values.TryGetValue(key, out var val) ? val.ToString() : defaultValue;
			}

			case "keyof":
				CheckArgCountLocal("1-2");
			{
				var value = args[0];
				var defaultKey = GetArgument(args, 1, "");
				return vd => vd.KeyOf(value, defaultKey);
			}

			case "keysof":
				CheckArgCountLocal("1-2");
			{
				var value = args[0];
				var joiner = GetArgument(args, 1, ",");
				return vd => vd.KeysOf(value, joiner);
			}

			case "joinkeys":
				CheckArgCountLocal("0-1");
			{
				var joiner = GetArgument(args, 0, ",");
				return vd => vd.JoinKeys(joiner);
			}

			case "joinvalues": {
				var joiner = GetArgument(args, 0, ",");
				if (args.Length <= 1) {
					// joinvalues(joiner = ",")
					return vd => vd.JoinValues(joiner);
				}
				// joinvalues(joiner, params keys)
				var keys = args.Skip(1).ToArray();
				return vd => vd.JoinValues(joiner, keys);
			}

			case "joinall": {
				var kvjoiner = GetArgument(args, 0, "=");
				var pairjoiner = GetArgument(args, 1, ",");

				if (args.Length <= 2) {
					// joinall(kvjoiner = "=", pairjoiner = ",")
					return vd => vd.JoinAll(kvjoiner, pairjoiner);
				}
				// joinall(kvjoiner, pairjoiner, params keys)
				var keys = args.Skip(2).ToArray();
				return vd => vd.JoinAll(kvjoiner, pairjoiner, keys);
			}

			case "sumkeys":
			case "sum": // sum values
				CheckArgCountLocal("0");
			{
				var sumKeys = methodName == "sumkeys";
				return vd => {
					var sum = sumKeys ? vd.SumKeys() : vd.Sum();
					return I18n.ThingToString(sum);
				};
			}

			case "minkey":
			case "maxkey":
				CheckArgCountLocal("0-1");
			{
				var isMin = methodName.StartsWith("min", StringComparison.OrdinalIgnoreCase);
				var valueType = GetArgument(args, 0);
				var extremum = ExtremumEvaluator.BuildEvaluator(valueType, isMin, expr);
				return vd => extremum(vd.Values.Keys);
			}

			case "min": // dvar:dict.min(type = "n")  num = "n" / str = "s" / hex = "h"
			case "max":
				CheckArgCountLocal("0-1");
			{
				var isMin = methodName.StartsWith("min", StringComparison.OrdinalIgnoreCase);
				var valueType = GetArgument(args, 0);
				var extremum = ExtremumEvaluator.BuildEvaluator(valueType, isMin, expr);
				return vd => extremum(vd.Values.Values.Select(v => v.ToString()));
			}

			default:
				throw new Exception($"Unknown dict method '{methodName}' in expression '{expr.RawExpression}'.");
		}
	}
}