using System;
using System.Linq;
using Triggernometry.Core.Variables;
using Triggernometry.Expressions.Maths;
using Triggernometry.Expressions.String.Models;
using Triggernometry.Localization;
using static Triggernometry.Expressions.String.Utils.ArgHelper;
using static Triggernometry.Expressions.String.Utils.ParserCommon;

namespace Triggernometry.Expressions.String.Evaluators;

internal static class TableEvaluator {
	internal static Func<VariableTable, string> BuildEvaluator(IndexMemberExpression expr) {
		// invalid (only name)
		if (expr.Indexes.Length == 0 && !expr.Member.HasValue)
                throw new Exception($"Table variable must include an index or property in expression '{expr.RawExpression}'.");

		// tvar:Name[Col][Row]
		if (expr.Indexes.Length > 0) {
			if (expr.Indexes.Length != 2) {
				throw new Exception($"Table index must have 2 components [col][row] in expression '{expr.RawExpression}'.");
			}

			var col = expr.Col.Equals("last", StringComparison.OrdinalIgnoreCase) ? -1 : (int)MathParser.Parse(expr.Col);
			var row = expr.Row.Equals("last", StringComparison.OrdinalIgnoreCase) ? -1 : (int)MathParser.Parse(expr.Row);

			return vt => vt.Peek(col, row).ToString();
		}

		// tvar:Name.Method(Args)
		var methodName = expr.Member.Name.ToLowerInvariant();
		var args = expr.Member.Args;

		void CheckArgCountLocal(string argCountRule) {
			CheckArgCount(argCountRule, args.Length, methodName, expr.RawExpression);
		}

		switch (methodName) {
			case "w":
			case "width":
				CheckArgCountLocal("0");
				return vt => vt.Width.ToString();

			case "h":
			case "height":
				CheckArgCountLocal("0");
				return vt => vt.Height.ToString();

                case "get":
                    {
                        CheckArgCountLocal("3");
                        int col = (int)MathParser.Parse(args[0]);
                        int row = (int)MathParser.Parse(args[1]);
                        string defaultValue = args[2];
                        return vt => vt.Peek(col, row, defaultValue).ToString();
                    }

			case "hjoin": // .hjoin(joiner1 = ",", joiner2 = LINEBREAK_PLACEHOLDER, colSlices = ":", rowSlices = ":")
			case "vjoin": // .vjoin(joiner1 = ",", joiner2 = LINEBREAK_PLACEHOLDER, colSlices = ":", rowSlices = ":")
				CheckArgCountLocal("0-4");
			{
				var joiner1 = GetArgument(args, 0, ",");
				var joiner2 = GetArgument(args, 1, LINEBREAK_STR);
				var colSlicesStr = GetArgument(args, 2, ":");
				var rowSlicesStr = GetArgument(args, 3, ":");

				return vt => {
					var colIndices = GetSliceIndices(colSlicesStr, vt.Width, expr.RawExpression, 1);
					var rowIndices = GetSliceIndices(rowSlicesStr, vt.Height, expr.RawExpression, 1);

					if (methodName.StartsWith("hj")) // hjoin / hjoin(...)
						return vt.HJoin(joiner1, joiner2, colIndices, rowIndices);
					return vt.VJoin(joiner1, joiner2, colIndices, rowIndices);
				};
			}

			case "hl":
			case "hlookup": // .hlookup(targetStr, rowIndex, colSlices = ":") => colIndex
			case "vl":
			case "vlookup": // .vlookup(targetStr, colIndex, rowSlices = ":") => rowIndex
				CheckArgCountLocal("2-3");
			{
				var targetStr = args[0];
				var indexStr = args[1];
				var rawIndex = (int)MathParser.Parse(indexStr);

				var slicesStr = GetArgument(args, 2, ":");
				var horizontal = methodName.StartsWith("h");

				return vt => {
					var maxLength = horizontal ? vt.Height : vt.Width;
					var index = rawIndex < 0 ? rawIndex + maxLength : rawIndex - 1;

					var indices = GetSliceIndices(
						slicesStr,
						horizontal ? vt.Width : vt.Height,
						expr.RawExpression,
						1);

					var res = horizontal
						? vt.HLookup(targetStr, index, indices)
						: vt.VLookup(targetStr, index, indices);

					return res.ToString();
				};
			}

			case "count": // count(targetStr, colSlices = ":", rowSlices = ":")
				CheckArgCountLocal("1-3");
			{
				var targetStr = args[0];
				var colSlicesStr = GetArgument(args, 1, ":");
				var rowSlicesStr = GetArgument(args, 2, ":");

				return vt => {
					var colIndices = GetSliceIndices(colSlicesStr, vt.Width, expr.RawExpression, 1);
					var rowIndices = GetSliceIndices(rowSlicesStr, vt.Height, expr.RawExpression, 1);
					return vt.Count(targetStr, colIndices, rowIndices).ToString();
				};
			}

			case "sum":
				CheckArgCountLocal("0-2");
			{
				var colSlicesStr = GetArgument(args, 0, ":");
				var rowSlicesStr = GetArgument(args, 1, ":");

				return vt => {
					var colIndices = GetSliceIndices(colSlicesStr, vt.Width, expr.RawExpression, 1);
					var rowIndices = GetSliceIndices(rowSlicesStr, vt.Height, expr.RawExpression, 1);
					return I18n.ThingToString(vt.Sum(colIndices, rowIndices));
				};
			}

			case "min": // tvar:table.min(type = "n", colSlices = ":", rowSlices = ":")
			case "max":
				CheckArgCountLocal("0-3");
			{
				var isMin = methodName.StartsWith("min", StringComparison.OrdinalIgnoreCase);
				var valueType = GetArgument(args, 0);
				var colSlicesStr = GetArgument(args, 1, ":");
				var rowSlicesStr = GetArgument(args, 2, ":");

				var extremum = ExtremumEvaluator.BuildEvaluator(valueType, isMin, expr);

				return vt => {
					var colIndices = GetSliceIndices(colSlicesStr, vt.Width, expr.RawExpression, 1);
					var rowIndices = GetSliceIndices(rowSlicesStr, vt.Height, expr.RawExpression, 1);

					var strings = rowIndices.SelectMany(row => colIndices.Select(col => vt.Rows[row].Values[col]))
						.Select(v => v.ToString());
					return extremum(strings);
				};
			}

			case "contain":
				CheckArgCountLocal("1-3");
			{
				var targetStr = args[0];
				var colSlicesStr = GetArgument(args, 1, ":");
				var rowSlicesStr = GetArgument(args, 2, ":");

				return vt => {
					var colIndices = GetSliceIndices(colSlicesStr, vt.Width, expr.RawExpression, 1);
					var rowIndices = GetSliceIndices(rowSlicesStr, vt.Height, expr.RawExpression, 1);

					var exist = colIndices.Any(col =>
						rowIndices.Any(row => vt.Rows[row].Values[col].ToString() == targetStr));

					return exist ? "1" : "0";
				};
			}

			case "ifcontain":
				CheckArgCountLocal("3");
			{
				var targetStr = args[0];
				var trueStr = args[1];
				var falseStr = args[2];

				return vt => {
					var exist = vt.Rows
						.SelectMany(row => row.Values)
						.Any(cell => cell.ToString() == targetStr);

					return exist ? trueStr : falseStr;
				};
			}

			default:
				throw new Exception($"Unknown table method '{methodName}' in expression '{expr.RawExpression}'.");
		}
	}
}