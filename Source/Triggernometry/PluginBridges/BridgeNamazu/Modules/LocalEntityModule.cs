using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace Triggernometry.PluginBridges.BridgeNamazu.Modules;

public class LocalEntityModule : ModuleBase {
	// FFXIVClientStructs/FFXIV/Client/Game/Character/Character.CharacterSetupContainer
	public Func<int> CharacterSetupContainerOffset;

	// FFXIVClientStructs/FFXIV/Client/Game/Character/CharacterSetupContainer
	public IntPtr CopyFromCharacterFuncPtr;
	public IntPtr SetupBNpcFuncPtr;

	public EntityModule entityModule => BridgeNamazu.GetModule<EntityModule>();

	public LocalEntityModule() {
		ScanMethod = () => {
			CharacterSetupContainerOffset = () => 0x1B10;

			CopyFromCharacterFuncPtr = Scanner.TryScan("E8 * * * * 8B 87 ?? ?? ?? ?? 85 C0 74 ?? 83 F8", nameof(CopyFromCharacterFuncPtr));
			SetupBNpcFuncPtr = Scanner.TryScan("E8 * * * * 45 0F B6 86 ?? ?? ?? ?? 48 8D 8F", nameof(SetupBNpcFuncPtr));
		};
	}

	public unsafe int CreateBattleCharacter(int index = -1, byte param = 0) => (int)ClientObjectManager.Instance()->CreateBattleCharacter((uint)index, param);

	public unsafe IntPtr GetObjectByIndex(int idx) => (IntPtr)ClientObjectManager.Instance()->GetObjectByIndex((ushort)idx);

	// public unsafe IntPtr DeleteObjectByIndex(int idx, byte param)
	// {  ClientObjectManager.Instance()->DeleteObjectByIndex((ushort)idx,param);
	// }

	// public IntPtr CopyFromCharacter(IntPtr targetPtr, IntPtr sourcePtr, CopyFlags flags)
	// {
	//     var characterSetupContainerPtr = targetPtr + CharacterSetupContainerOffset();
	//     return Memory.CallInjected64<IntPtr>(CopyFromCharacterFuncPtr, characterSetupContainerPtr, sourcePtr, (uint)flags);
	// }

	// public void SetupBNpc(IntPtr targetPtr, uint bNpcBaseId, uint bNpcNameId = 0)
	// {
	//     var characterSetupContainerPtr = targetPtr + CharacterSetupContainerOffset();
	//     Memory.CallInjected64(SetupBNpcFuncPtr, characterSetupContainerPtr, bNpcBaseId, bNpcNameId);
	// }

	public IntPtr CreateLocalEntity(Vector3 pos, float heading = 0) {
		var idx = CreateBattleCharacter();
		var entityPtr = GetObjectByIndex(idx);
		entityModule.SetPos(entityPtr, pos.X, pos.Y, pos.Z);
		entityModule.SetDefaultPos(entityPtr, pos.X, pos.Y, pos.Z);
		entityModule.SetHeading(entityPtr, heading);
		entityModule.SetDefaultHeading(entityPtr, heading);
		return entityPtr;
	}

	public void ReDraw(IntPtr entityPtr) => entityModule.ReDraw(entityPtr);

	[Flags]
	public enum CopyFlags : uint {
		None = 0x00,
		Mode = 0x1, // emote loop etc
		Mount = 0x2,
		ClassJob = 0x4,
		Companion = 0x20,
		WeaponHiding = 0x80,
		Target = 0x400,
		Name = 0x1000,
		LastAnimation = 0x8000,
		Position = 0x10000, // includes rotation
		UseSecondaryCharaId = 0x200000,
		Ornament = 0x400000
	}
}