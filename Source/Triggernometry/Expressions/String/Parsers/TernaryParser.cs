using System;
using Triggernometry.Expressions.Maths;
using Triggernometry.Expressions.String.Utils;
using Triggernometry.Localization;

namespace Triggernometry.Expressions.String.Parsers;

internal static class TernaryParser {
	internal static string Parse(string ternaryExpr) {
		ParseTernaryExpression(ternaryExpr, out var condExpr, out var trueStr, out var falseStr);
		if (trueStr == null || falseStr == null) {
			throw new FormatException(I18n.Translate("internal/Context/ternaryexpressionerror",
				"Ternary expression ({0}) could not be parsed: \r\nCondition: ({1}); \r\nTrueExpr: ({2}); \r\nFalseExpr: ({3})",
				ternaryExpr, condExpr, trueStr ?? "null", falseStr ?? "null"));
		}
		var cond = !MathParser.IsZero(MathParser.Parse(condExpr));
		return cond ? trueStr : falseStr;
	}

	private static void ParseTernaryExpression(string input, out string condExpr, out string trueStr, out string falseStr) {
		falseStr = ExtractLastExpression(ref input, ':');
		trueStr = ExtractLastExpression(ref input, '?');
		condExpr = input.TrimEx();
	}

	private static string ExtractLastExpression(ref string input, char sep) {
		input = input.TrimRightEx();
		var lastIndex = input.Length - 1;
		var lastChar = input[lastIndex];

		int? sepIndex = null;
		if (lastChar is '\'' or '\"') {
			var quoteIndex = input.LastIndexOf(lastChar, lastIndex - 1);
			if (quoteIndex != -1) {
				sepIndex = input.LastIndexOf(sep, quoteIndex - 1);
			}
		}
		sepIndex = sepIndex ?? input.LastIndexOf(sep);
		if (sepIndex == -1) return null;

		var afterSep = input[(sepIndex.Value + 1)..].TrimLeftEx();
		var length = afterSep.Length;
		if (length >= 2 && afterSep[0] == afterSep[length - 1] && (afterSep[0] == '\"' || afterSep[0] == '\'')) {
			afterSep = afterSep.Substring(1, length - 2); // "..." / '...' => ...
		}

		input = input[..sepIndex.Value];
		return afterSep;
	}
}