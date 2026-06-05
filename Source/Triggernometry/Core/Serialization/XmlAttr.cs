using System;
using System.Globalization;

namespace Triggernometry.Core.Serialization;

/// <summary>Provides helpers for serializing and deserializing XML attribute values in Triggernometry. </summary>
internal static class XmlAttr {
	// ---------- string ----------

	/// <summary>Serializes a <see cref="string" /> for XML attributes, omitting the specified default value. </summary>
	public static string String(string value, string omitValue, bool omitWhitespace = true) {
		if (omitWhitespace && string.IsNullOrWhiteSpace(value))
			return null;

		if (value == omitValue)
			return null;

		return value;
	}

	/// <summary>Serializes a <see cref="string" /> for XML attributes, omitting any of the specified default values. </summary>
	public static string String(string value, bool omitWhitespace = true, params string[] omitValues) {
		if (omitWhitespace && string.IsNullOrWhiteSpace(value))
			return null;

		if (omitValues.Contains(value))
			return null;

		return value;
	}

	// string setter: field = value;


	// ---------- bool ----------

	/// <summary>Serializes a <see cref="bool" /> for XML attributes, omitting the specified default value. </summary>
	public static string Bool(bool value, bool omitValue) {
		if (value == omitValue)
			return null;

		return value.ToString();
	}

	/// <summary>Parses a <see cref="bool" /> from an XML attribute string. </summary>
	public static bool Bool(string value) => bool.Parse(value);


	// ---------- int ----------

	/// <summary>Serializes an <see cref="int" /> for XML attributes, omitting the specified default value. </summary>
	public static string Int(int value, int omitValue) {
		if (value == omitValue)
			return null;

		return value.ToString(CultureInfo.InvariantCulture);
	}

	/// <summary>Parses an <see cref="int" /> from an XML attribute string. </summary>
	public static int Int(string value) => int.Parse(value, CultureInfo.InvariantCulture);


	// ---------- long ----------

	/// <summary>Serializes a <see cref="long" /> for XML attributes, omitting the specified default value. </summary>
	public static string Long(long value, long omitValue) {
		if (value == omitValue)
			return null;

		return value.ToString(CultureInfo.InvariantCulture);
	}

	/// <summary>Parses a <see cref="long" /> from an XML attribute string. </summary>
	public static long Long(string value) => long.Parse(value, CultureInfo.InvariantCulture);


	// ---------- double ----------

	/// <summary>Serializes a <see cref="double" /> for XML attributes, omitting the specified default value. </summary>
	public static string Double(double value, double omitValue) {
		if (value == omitValue)
			return null;

		return value.ToString(CultureInfo.InvariantCulture);
	}

	/// <summary>Parses a <see cref="double" /> from an XML attribute string. </summary>
	public static double Double(string value) => double.Parse(value, CultureInfo.InvariantCulture);


	// ---------- float ----------

	/// <summary>Serializes a <see cref="float" /> for XML attributes, omitting the specified default value. </summary>
	public static string Float(float value, float omitValue) {
		if (value == omitValue)
			return null;

		return value.ToString(CultureInfo.InvariantCulture);
	}

	/// <summary>Parses a <see cref="float" /> from an XML attribute string. </summary>
	public static float Float(string value) => float.Parse(value, CultureInfo.InvariantCulture);


	// ---------- enum ----------

	/// <summary>Serializes an enum value for XML attributes, omitting the specified default value. </summary>
	public static string Enum<T>(T value, T omitValue) where T : struct, Enum {
		if (value.Equals(omitValue))
			return null;

		return value.ToString();
	}

	/// <summary>Parses an enum value from an XML attribute string, ignoring case. </summary>
	public static T Enum<T>(string value) where T : struct, Enum => System.Enum.Parse<T>(value, true);

	/// <summary>
	///     Tries to parse an enum value from an XML attribute string, ignoring case.
	/// </summary>
	public static bool TryEnum<T>(string value, out T result) where T : struct, Enum => System.Enum.TryParse(value, true, out result);

	// ---------- Version ----------

	/// <summary>Serializes a <see cref="System.Version" /> for XML attributes, omitting the default value. </summary>
	public static string Version(Version value, Version omitValue) {
		if (value == omitValue)
			return null;

		return value.ToString();
	}

	/// <summary>Parses a <see cref="System.Version" /> from an XML attribute string. </summary>
	public static Version Version(string value) => System.Version.Parse(value);

	// ---------- Guid ----------

	/// <summary>Serializes a <see cref="System.Guid" /> for XML attributes, omitting the specified default value. </summary>
	public static string Guid(Guid value, Guid omitValue) {
		if (value == omitValue)
			return null;

		return value.ToString();
	}

	/// <summary>Parses a <see cref="System.Guid" /> from an XML attribute string. </summary>
	public static Guid Guid(string value) => System.Guid.Parse(value);
}