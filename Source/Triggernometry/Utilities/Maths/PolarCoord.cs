using System;
using Triggernometry.Expressions.Maths;
using Triggernometry.Expressions.String.Utils;
using Triggernometry.Localization;
using static System.Math;

namespace Triggernometry.Utilities.Maths;

public class PolarCoord(double r, double θ, double z) : XIVCoord {
	public double R = r;
	public double θ = θ;
	public double Z = z;

	public override XIVCoord Duplicate() => new PolarCoord(R, θ, Z);

	public override XIVCoord RotateTo(double theta) {
		θ += theta + PI;
		return this;
	}

	public override XIVCoord MoveTo(double dx, double dy, double dz)
		=> ToCartesian().MoveTo(dx, dy, dz);

	public override XIVCoord ScaleBy(double scaleX, double scaleY, double scaleZ) {
		if (Abs(scaleX - scaleY) < 1e-5 && scaleX >= 1e-4) {
			R *= scaleX;
			Z *= scaleZ;
			return this;
		}
		return ToCartesian().ScaleBy(scaleX, scaleY, scaleZ);
	}

	public override CartesianCoord ToCartesian() {
		var x = R * Sin(θ);
		var y = R * Cos(θ);
		return new CartesianCoord(x, y, Z);
	}

	public override PolarCoord ToPolar() => new(R, θ, Z);

	public override double Length => Sqrt(R * R + Z * Z);

	public override string ToString() => $"(R={R}, θ={θ}, Z={Z})";

	public override string Jsonify() => ToCartesian().Jsonify();

	public static PolarCoord Parse(params string[] coords) {
		switch (coords.Length) {
			case 2:
				return ParsePolarCoordsString(coords[0], coords[1]);
			case 3:
				return ParsePolarCoordsString(coords[0], coords[1], coords[2]);
			default:
				throw ErrorHelper.ArgumentCountError(I18n.Translate("internal/PolarCoord/constructor", "PolarCoord constructor"), "2-3", coords.Length, "[" + string.Join("], [", coords) + "]");
		}
	}

	private static PolarCoord ParsePolarCoordsString(string rawR, string rawtheta, string? rawZ = null) {
		try {
			return new PolarCoord(
				MathParser.Parse(rawR),
				MathParser.Parse(rawtheta),
				rawZ == null ? 0 : MathParser.Parse(rawZ));
		} catch (Exception ex) {
			throw new ArgumentException(I18n.Translate("internal/PolarCoord/parseerror", "Failed to parse polar coordinates: {0}\n\nRaw data:\nr = ({1}),\nθ = ({2}),\nz = ({3})", ex.Message, rawR, rawtheta, rawZ));
		}
	}
}