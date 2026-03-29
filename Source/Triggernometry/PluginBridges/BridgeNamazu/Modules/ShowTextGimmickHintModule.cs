using System;
using FFXIVClientStructs.FFXIV.Client.UI;
using Triggernometry.Expressions.Maths;

namespace Triggernometry.PluginBridges.BridgeNamazu.Modules;

public class ShowTextGimmickHintModule : ModuleBase {
	public ShowTextGimmickHintModule() {
		ScanMethod = () => { };
	}

	[CallbackMethod("Hint")]
	internal void CbHint(string command) => ShowTextGimmickHintRaw(true, command);

	[CallbackMethod("Warn")]
	internal void CbWarn(string command) => ShowTextGimmickHintRaw(false, command);

	private void ShowTextGimmickHintRaw(bool isHint, string command) {
		CheckBeforeExecution(command);
		var lines = command.Split(['\n'], 2);
		var rawTime = lines[0].Trim();
		var text = lines.Length > 1 ? lines[1] : "";

		var timeIn100Ms = Math.Max(0, (int)(MathParser.Parse(rawTime) * 10));
		NamazuLog((isHint ? "[Hint]" : "[Warn]") + $": ({timeIn100Ms / 10.0:F1} s) {text}");

		GreyMagicMemoryBase.ExecuteWithLock(() => ShowTextGimmickHint(isHint, text, timeIn100Ms));
	}

	public unsafe void ShowTextGimmickHint(bool isHint, string text, int timeIn100Ms) {
		CheckIfAnyZeroPtr();
		RaptureAtkModule.Instance()->ShowTextGimmickHint(text, (RaptureAtkModule.TextGimmickHintStyle)(isHint ? 1 : 0), timeIn100Ms);
	}
}