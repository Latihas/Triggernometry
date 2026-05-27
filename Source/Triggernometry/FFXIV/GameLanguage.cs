using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace Triggernometry.FFXIV;

public static class GameLanguage {
	internal static void ScanOffsets() {
	}

	public static unsafe Framework* FrameworkPtr => Framework.Instance();

	public static unsafe GameLanguageEnum Language => (GameLanguageEnum)FrameworkPtr->ClientLanguage;
}

public enum GameLanguageEnum : byte {
	JP = 0,
	EN = 1,
	DE = 2,
	FR = 3,
	CN = 4,
	KR = 6,
	TCN = 7,
	None = 0xFF
}