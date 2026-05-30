using System;
using Triggernometry.Expressions.String.Utils;

namespace Triggernometry.Expressions.String.Models;

/// <summary>
///     Represents a generic member expression. <br />
///     The term "member" includes property/method-like forms: <br />
///     · "<c>PropName</c>" <br />
///     · "<c>MethodName()</c>"  <br />
///     · "<c>MethodName(args...)</c>"
/// </summary>
public readonly struct MemberExpression {
	/// <summary>
	///     Method or property name.
	///     An empty string indicates an invalid or missing member.
	/// </summary>
	public readonly string Name;

	/// <summary>
	///     Argument parsing result. <br />
	///     · <see langword="null" />: parentheses not present: Name <br />
	///     · Empty array: parentheses present but no arguments: Name() <br />
	///     · Non-empty array: arguments parsed from parentheses: Name(args)
	/// </summary>
	public readonly string[] _args;

	/// <summary>
	///     Get the argument parsing result, or an empty array if no arguments were given.
	/// </summary>
	public string[] Args => _args ?? [];

	private readonly string _rawExpression;

	/// <summary>
	///     Get the original expression. Reconstructed if the raw expression has not been provided.
	/// </summary>
	public string RawExpression => _rawExpression ?? BuildRawExpression();

	/// <summary>
	///     Whether this instance represents a valid member expression.
	/// </summary>
	public bool HasValue => !string.IsNullOrEmpty(Name);

	/// <summary>
	///     Represents an empty or missing member expression.
	/// </summary>
	public static readonly MemberExpression Empty = new(string.Empty, null, string.Empty);

	/// <summary>
	///     Creates an instance directly from the given data. Name should not be null.
	/// </summary>
	public MemberExpression(string name, string[] args, string rawExpression = null) {
		Name = name ?? throw new ArgumentNullException(nameof(name));
		_args = args;
		_rawExpression = rawExpression;
	}

	/// <summary>
	///     Parse a member expression from the given string. <br />
	///     · <paramref name="startIndex" /> points to the first character of the member name
	/// </summary>
	public MemberExpression(string expr, int startIndex = 0) {
		if (expr == null)
			throw new ArgumentNullException(nameof(expr));
		if (startIndex < 0 || startIndex >= expr.Length)
			throw new ArgumentOutOfRangeException(nameof(startIndex));

		_rawExpression = startIndex == 0 ? expr : expr[startIndex..];
		Name = _rawExpression;
		_args = null;

		var lParenPos = expr.IndexOf('(', startIndex);

		// No parentheses → property access
		if (lParenPos == -1) {
			Name = expr[startIndex..].TrimEx();
			return;
		}

		// search ')' from the end
		var rParenPos = -1;
		for (var i = expr.Length - 1; i > lParenPos; i--) {
			var c = expr[i];
			if (c.IsWhiteSpaceEx()) continue;
			if (c == ')') {
				rParenPos = i;
			}
			break;
		}

		// Invalid or unterminated ')' → treat the whole expression as Name
		if (rParenPos == -1) {
			Name = Name.TrimEx();
			return;
		}

		var nameStrLength = lParenPos - startIndex;
		Name = expr.Substring(startIndex, nameStrLength).TrimEx();

		var argsStrLength = rParenPos - lParenPos - 1;
		var rawArgs = expr.Substring(lParenPos + 1, argsStrLength);

		var hasArgs = !string.IsNullOrWhiteSpace(rawArgs);
		_args = hasArgs ? ArgHelper.SplitArguments(rawArgs) : [];
	}

	private string BuildRawExpression() {
		if (string.IsNullOrEmpty(Name))
			return string.Empty;

		if (_args == null)
			return Name;

		return $"{Name}({string.Join(", ", _args)})";
	}
}