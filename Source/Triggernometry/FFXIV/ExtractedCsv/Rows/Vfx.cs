namespace Triggernometry.FFXIV.ExtractedCsv.Rows;

public class Vfx : TypedCsvRow {
	/// <summary> VFX name (path), e.g. "dk01rf_atk0h" </summary>
	public override string Name => Get("Location");
}