namespace Triggernometry.FFXIV.ExtractedCsv.Rows;

public class ContentType : TypedCsvRow {
	// Duty Roulette, Dungeons, etc.
	public override string Name => Get("Name");
}