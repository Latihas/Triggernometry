using System;
using System.Runtime.InteropServices;
using Triggernometry.Expressions.String.Utils;

namespace Triggernometry.PluginBridges.BridgeNamazu.Modules;

public class QuitInstanceModule : ModuleBase {
	public static QuitInstanceDelegate QuitInstanceD = null!;

	public delegate IntPtr QuitInstanceDelegate(byte shouldForceQuit);

	public QuitInstanceModule() {
		ScanMethod = () => { QuitInstanceD = Marshal.GetDelegateForFunctionPointer<QuitInstanceDelegate>(Scanner.TryScan("48 83 EC ?? 0F B6 D1 45 33 C9", "QuitInstancePtr")); };
	}

	[CallbackMethod("QuitInstance")]
	internal void CbQuitInstance(string cmd) {
		CheckBeforeExecution(cmd);
		var shouldForceQuit = cmd.ParseDataOrDefault(false);
		GreyMagicMemoryBase.ExecuteWithLock(() => QuitInstance(shouldForceQuit));
	}

	public void QuitInstance(bool shouldForceQuit) {
		CheckIfAnyZeroPtr();
		QuitInstanceD((byte)(shouldForceQuit ? 1 : 0));
	}
}