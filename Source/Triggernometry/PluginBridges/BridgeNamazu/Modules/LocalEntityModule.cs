using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace Triggernometry.PluginBridges.BridgeNamazu.Modules;

public class LocalEntityModule : ModuleBase {
	public EntityModule entityModule => BridgeNamazu.GetModule<EntityModule>();

	public LocalEntityModule() {
		ScanMethod = () => {
		};
	}

	public unsafe int CreateBattleCharacter(int index = -1, byte param = 0) => (int)ClientObjectManager.Instance()->CreateBattleCharacter((uint)index, param);

	public unsafe IntPtr GetObjectByIndex(int idx) => (IntPtr)ClientObjectManager.Instance()->GetObjectByIndex((ushort)idx);

	public unsafe IntPtr DeleteObjectByIndex(int idx, byte param) {
		ClientObjectManager.Instance()->DeleteObjectByIndex((ushort)idx, param);
		return IntPtr.Zero; //TODO
	}

	public unsafe IntPtr CopyFromCharacter(IntPtr targetPtr, IntPtr sourcePtr, CopyFlags flags) {
		((Character*)targetPtr)->CharacterSetup.CopyFromCharacter((Character*)sourcePtr, (CharacterSetupContainer.CopyFlags)(uint)flags);
		return IntPtr.Zero;
	}

	public unsafe void SetupBNpc(IntPtr targetPtr, uint bNpcBaseId, uint bNpcNameId = 0) {
		((Character*)targetPtr)->CharacterSetup.SetupBNpc(bNpcBaseId, bNpcNameId);
	}

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