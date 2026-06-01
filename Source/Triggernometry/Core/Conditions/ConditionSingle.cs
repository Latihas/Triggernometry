using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Triggernometry.Expressions.Maths;
using Triggernometry.Localization;

namespace Triggernometry.Core.Conditions;

public sealed class ConditionSingle : ConditionComponent {
	public enum ExprTypeEnum {
		String,
		Numeric
	}

	public enum CndTypeEnum {
		NumericEqual,
		NumericNotEqual,
		NumericGreater,
		NumericGreaterEqual,
		NumericLess,
		NumericLessEqual,
		StringEqualCase,
		StringEqualNocase,
		StringNotEqualCase,
		StringNotEqualNocase,
		RegexMatch,
		RegexNotMatch,
		ListContains,
		ListDoesNotContain
	}

	private string _ExpressionL;
	private ExprTypeEnum _ExpressionTypeL;
	private string _ExpressionR;
	private ExprTypeEnum _ExpressionTypeR;
	private CndTypeEnum _ConditionType;


	[XmlAttribute] public string ExpressionL {
		get => _ExpressionL;
		set {
			if (value != _ExpressionL) {
				_ExpressionL = value;
				TriggerOnPropertyChange();
			}
		}
	}

	[XmlAttribute] public ExprTypeEnum ExpressionTypeL {
		get => _ExpressionTypeL;
		set {
			if (value != _ExpressionTypeL) {
				_ExpressionTypeL = value;
				TriggerOnPropertyChange();
			}
		}
	}

	[XmlAttribute] public string ExpressionR {
		get => _ExpressionR;
		set {
			if (value != _ExpressionR) {
				_ExpressionR = value;
				TriggerOnPropertyChange();
			}
		}
	}

	[XmlAttribute] public ExprTypeEnum ExpressionTypeR {
		get => _ExpressionTypeR;
		set {
			if (value != _ExpressionTypeR) {
				_ExpressionTypeR = value;
				TriggerOnPropertyChange();
			}
		}
	}

	[XmlAttribute] public CndTypeEnum ConditionType {
		get => _ConditionType;
		set {
			if (value != _ConditionType) {
				_ConditionType = value;
				TriggerOnPropertyChange();
			}
		}
	}

	private string Capitalize(string str) {
		if (str == null) {
			return null;
		}
		if (str.Length > 1) {
			return char.ToUpper(str[0]) + str[1..];
		}
		return str.ToUpper();
	}

	public override string ToString() {
		var desc = "";
		var descL = ExpressionL != null && ExpressionL.Length > 256 ? ExpressionL[..256] + "..." : ExpressionL;
		var descR = ExpressionR != null && ExpressionR.Length > 256 ? ExpressionR[..256] + "..." : ExpressionR;
		if (ConditionType == CndTypeEnum.ListContains || ConditionType == CndTypeEnum.ListDoesNotContain) {
			desc = I18n.Translate("internal/ConditionSingle/listvar", "List variable specified by");
			desc += " ";
			switch (ExpressionTypeL) {
				case ExprTypeEnum.Numeric:
					desc += I18n.Translate("internal/ConditionSingle/numericexpression", "numeric expression ({0})", descL);
					break;
				case ExprTypeEnum.String:
					desc += I18n.Translate("internal/ConditionSingle/stringexpression", "string expression ({0})", descL);
					break;
			}
			desc += " ";
			switch (ConditionType) {
				case CndTypeEnum.ListContains:
					desc += I18n.Translate("internal/ConditionSingle/listmustcontain", "must contain the result from");
					break;
				case CndTypeEnum.ListDoesNotContain:
					desc += I18n.Translate("internal/ConditionSingle/listcantcontain", "must not contain the result from");
					break;
			}
		} else {
			switch (ExpressionTypeL) {
				case ExprTypeEnum.Numeric:
					desc = Capitalize(I18n.Translate("internal/ConditionSingle/numericexpression", "numeric expression ({0})", descL));
					break;
				case ExprTypeEnum.String:
					desc = Capitalize(I18n.Translate("internal/ConditionSingle/stringexpression", "string expression ({0})", descL));
					break;
			}
			desc += " ";
			switch (ConditionType) {
				case CndTypeEnum.NumericEqual:
					desc += I18n.Translate("internal/ConditionSingle/numericequal", "must be numerically equal to");
					break;
				case CndTypeEnum.NumericNotEqual:
					desc += I18n.Translate("internal/ConditionSingle/numericnotequal", "must not be numerically equal to");
					break;
				case CndTypeEnum.NumericGreater:
					desc += I18n.Translate("internal/ConditionSingle/numericgreater", "must be numerically greater than");
					break;
				case CndTypeEnum.NumericGreaterEqual:
					desc += I18n.Translate("internal/ConditionSingle/numericgreaterequal", "must be numerically greater or equal to");
					break;
				case CndTypeEnum.NumericLess:
					desc += I18n.Translate("internal/ConditionSingle/numericless", "must be numerically less than");
					break;
				case CndTypeEnum.NumericLessEqual:
					desc += I18n.Translate("internal/ConditionSingle/numericlessequal", "must be numerically less or equal to");
					break;
				case CndTypeEnum.StringEqualCase:
					desc += I18n.Translate("internal/ConditionSingle/stringequalcase", "must pass case-sensitive string comparison to the string from");
					break;
				case CndTypeEnum.StringEqualNocase:
					desc += I18n.Translate("internal/ConditionSingle/stringequalnocase", "must pass case-insensitive string comparison to the string from");
					break;
				case CndTypeEnum.StringNotEqualCase:
					desc += I18n.Translate("internal/ConditionSingle/stringnotequalcase", "must not pass case-sensitive string comparison to the string from");
					break;
				case CndTypeEnum.StringNotEqualNocase:
					desc += I18n.Translate("internal/ConditionSingle/stringnotequalnocase", "must not pass case-insensitive string comparison to the string from");
					break;
				case CndTypeEnum.RegexMatch:
					desc += I18n.Translate("internal/ConditionSingle/regexmatch", "must match the regular expression from");
					break;
				case CndTypeEnum.RegexNotMatch:
					desc += I18n.Translate("internal/ConditionSingle/noregexmatch", "must not match the regular expression from");
					break;
			}
		}
		switch (ExpressionTypeR) {
			case ExprTypeEnum.Numeric:
				desc += " " + I18n.Translate("internal/ConditionSingle/numericexpression", "numeric expression ({0})", descR);
				break;
			case ExprTypeEnum.String:
				desc += " " + I18n.Translate("internal/ConditionSingle/stringexpression", "string expression ({0})", descR);
				break;
		}
		return Capitalize(desc);
	}

	internal override ConditionComponent Duplicate() {
		var cs = new ConditionSingle {
			ConditionType = ConditionType,
			Enabled = Enabled,
			ExpressionL = ExpressionL,
			ExpressionR = ExpressionR,
			ExpressionTypeL = ExpressionTypeL,
			ExpressionTypeR = ExpressionTypeR
		};
		return cs;
	}

	internal string GetExpressionResultL(Context ctx, Context.LoggerDelegate logger, object o)
		=> GetExpressionResult(ExpressionL ?? "", ExpressionTypeL, ctx, logger, o);

	internal string GetExpressionResultR(Context ctx, Context.LoggerDelegate logger, object o)
		=> GetExpressionResult(ExpressionR ?? "", ExpressionTypeR, ctx, logger, o);

	private string GetExpressionResult(string expr, ExprTypeEnum exprType, Context ctx, Context.LoggerDelegate logger, object o) {
		switch (exprType) {
			case ExprTypeEnum.Numeric:
				return I18n.ThingToString(ctx.EvaluateNumericExpression(logger, o, expr));
			case ExprTypeEnum.String:
				return ctx.EvaluateStringExpression(logger, o, expr);
			default:
				throw new Exception($"Invalid expression type: {exprType}");
		}
	}

	internal override bool CheckCondition(Context ctx, Context.LoggerDelegate logger, object o) {
		try {
			if (!Enabled) {
				return false;
			}
			var lval = GetExpressionResultL(ctx, logger, o);
			var rval = GetExpressionResultR(ctx, logger, o);
			switch (ConditionType) {
				case CndTypeEnum.NumericEqual: {
					var ld = double.Parse(lval, CultureInfo.InvariantCulture);
					var rd = double.Parse(rval, CultureInfo.InvariantCulture);
					return MathParser.IsZero(ld - rd) ? true : false;
				}
				case CndTypeEnum.NumericNotEqual: {
					var ld = double.Parse(lval, CultureInfo.InvariantCulture);
					var rd = double.Parse(rval, CultureInfo.InvariantCulture);
					return MathParser.IsZero(ld - rd) ? false : true;
				}
				case CndTypeEnum.NumericGreater: {
					var ld = double.Parse(lval, CultureInfo.InvariantCulture);
					var rd = double.Parse(rval, CultureInfo.InvariantCulture);
					return ld > rd + MathParser.TOLERANCE;
				}
				case CndTypeEnum.NumericGreaterEqual: {
					var ld = double.Parse(lval, CultureInfo.InvariantCulture);
					var rd = double.Parse(rval, CultureInfo.InvariantCulture);
					return ld + MathParser.TOLERANCE >= rd;
				}
				case CndTypeEnum.NumericLess: {
					var ld = double.Parse(lval, CultureInfo.InvariantCulture);
					var rd = double.Parse(rval, CultureInfo.InvariantCulture);
					return ld + MathParser.TOLERANCE < rd;
				}
				case CndTypeEnum.NumericLessEqual: {
					var ld = double.Parse(lval, CultureInfo.InvariantCulture);
					var rd = double.Parse(rval, CultureInfo.InvariantCulture);
					return ld <= rd + MathParser.TOLERANCE;
				}
				case CndTypeEnum.StringEqualCase: {
					return string.Compare(lval, rval, false) == 0;
				}
				case CndTypeEnum.StringEqualNocase: {
					return string.Compare(lval, rval, true) == 0;
				}
				case CndTypeEnum.StringNotEqualCase: {
					return string.Compare(lval, rval, false) != 0;
				}
				case CndTypeEnum.StringNotEqualNocase: {
					return string.Compare(lval, rval, true) != 0;
				}
				case CndTypeEnum.RegexMatch: {
					return Regex.IsMatch(lval, rval);
				}
				case CndTypeEnum.RegexNotMatch: {
					return !Regex.IsMatch(lval, rval);
				}
				case CndTypeEnum.ListContains: {
					lock (ctx.Plugin.GetVariableStore(false).List) {
						if (ctx.Plugin.GetVariableStore(false).List.ContainsKey(lval)) {
							if (ctx.Plugin.GetVariableStore(false).List[lval].IndexOf(rval) > 0) {
								return true;
							}
						}
					}
					return false;
				}
				case CndTypeEnum.ListDoesNotContain: {
					lock (ctx.Plugin.GetVariableStore(false).List) {
						if (ctx.Plugin.GetVariableStore(false).List.ContainsKey(lval)) {
							if (ctx.Plugin.GetVariableStore(false).List[lval].IndexOf(rval) > 0) {
								return false;
							}
						}
					}
					return true;
				}
			}
		} catch (Exception) {
		}
		return false;
	}
}