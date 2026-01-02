namespace Triggernometry.FFXIV.ExtractedCsv;

public abstract class TypedCsvRow : CsvRow {
    public abstract string Name { get; }

    public TRow GetRow<TRow>(int id) where TRow : TypedCsvRow => Manager.Get<TRow>()[id];
}