using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Triggernometry.Core;
using Triggernometry.Expressions.String.Evaluators;
using Triggernometry.Expressions.String.Utils;
using Triggernometry.FFXIV;
using static Triggernometry.Expressions.String.Utils.DataStringHelper;

namespace Triggernometry.PluginBridges.BridgeNamazu.Modules;

public class EntityModule : ModuleBase {
	private delegate IntPtr EObjAnimationFunc(IntPtr p1, ushort p2, ushort p3, long p4);

	private EObjAnimationFunc EObjAnimationD;

	public EntityModule() {
		ScanMethod = () => {
			EObjAnimationD = Marshal.GetDelegateForFunctionPointer<EObjAnimationFunc>(Scanner.TryScanMultiple([
				"45 33 C9 0F B7 54 24 ?? 48 8B CB E8 * * * *" // 7.4
			], nameof(EObjAnimationD)));
		};
	}

	[CallbackMethod("InvokeOnMultipleEntities")]
	internal unsafe void CbInvokeOnMultipleEntities(string cmd) {
		CheckBeforeExecution(cmd);
		var cmds = cmd.Split(['\n'], StringSplitOptions.RemoveEmptyEntries);
		// 首行是实体过滤器
		var filter = XivEntityFilterEvaluator.CreateFilter(cmds[0]);
		foreach (var address in Entity.GetEntities().Where(filter).Select(e => e.Address)) {
			var strAddress = address.ToString();
			var hexId = ((GameObject*)address)->EntityId.ToString("X8");
			// 后续行是回调名称和参数，实体地址用 _address 替换
			foreach (var cbPair in cmds.Skip(1).Select(c => c.Split([','], 2))) {
				if (cbPair.Length == 1) throw new Exception($"批量调用回调时未提供回调参数：{cbPair[0]}");
				var cbName = cbPair[0].Trim();
				var cbRawParams = cbPair[1].Replace("_address", strAddress);
				Task.Run(() => {
					try {
						RealPlugin.Instance.InvokeNamedCallback(cbName, cbRawParams);
					} catch (Exception ex) {
						WarningLog($"对实体 0x{hexId} 调用回调 {cbName}: {cbRawParams} 时失败：\n{ex}");
					}
				});
			}
		}
	}

	[CallbackMethod("SetDefaultPos")]
	internal void CbSetDefaultPos(string cmd) {
		CheckBeforeExecution(cmd);
		var (objectPtr, x, y, z) = cmd.ParseArgs<IntPtr, float, float, float>();
		SetDefaultPos(objectPtr, x, y, z);
	}

	[CallbackMethod("SetPos", "Kairos")]
	internal void CbSetPos(string cmd) {
		CheckBeforeExecution(cmd);
		var (objectPtr, x, y, z) = cmd.ParseArgs<IntPtr, float, float, float>();
		SetPos(objectPtr, x, y, z);
	}

	[CallbackMethod("SetModelRelPos")]
	internal void CbSetModelRelPos(string cmd) {
		CheckBeforeExecution(cmd);
		var (objectPtr, dx, dy, dz) = cmd.ParseArgs<IntPtr, float, float, float>();
		SetModelRelPos(objectPtr, dx, dy, dz);
	}

	[CallbackMethod("Teleport", "Kairos")]
	internal void CbTeleport(string cmd) {
		CheckBeforeExecution(cmd);
		var objectPtr = Entity.GetMyself().Address;
		var (x, y, z) = cmd.ParseArgs<float, float, float>();
		SetPos(objectPtr, x, y, z);
	}

	[CallbackMethod("SetDefaultHeading")]
	internal void CbSetDefaultHeading(string cmd) {
		CheckBeforeExecution(cmd);
		var (objectPtr, heading) = cmd.ParseArgs<IntPtr, float>();
		SetDefaultHeading(objectPtr, heading);
	}

	[CallbackMethod("SetHeading")]
	internal void CbSetHeading(string cmd) {
		CheckBeforeExecution(cmd);
		var (objectPtr, heading) = cmd.ParseArgs<IntPtr, float>();
		SetHeading(objectPtr, heading);
	}

	[CallbackMethod("Target")]
	internal void CbTarget(string cmd) {
		CheckBeforeExecution(cmd);
		(uint id, var hard, var soft) = cmd.ParseArgs<HexOrDecId, bool, bool>((1, true), (2, true));
		IntPtr objectPtr = 0;

		if (id != HexOrDecId.Default) {
			var entity = Entity.GetEntityByID(id);
			if (entity.Exist)
				objectPtr = entity.Address;
			else
				WarningLog($"[鲶鱼精邮差扩展] [Target] 未找到实体：0x{id:X8}");
		}
		Target(objectPtr, hard, soft);
	}

	[CallbackMethod("SetModelStatus")]
	internal void CbSetModelStatus(string cmd) {
		CheckBeforeExecution(cmd);
		var (objectPtr, modelStatus) = cmd.ParseArgs<IntPtr, int>();
		SetModelStatus(objectPtr, modelStatus);
	}

	// 新方法 直接修改实体参数并重绘
	[CallbackMethod("SetObjectScale")]
	internal void CbSetObjectScale(string cmd) {
		CheckBeforeExecution(cmd);
		if (GetConfig<bool>("ObjectScale") == false) return; // ignored
		var (objectPtr, scale) = cmd.ParseArgs<IntPtr, float>();
		SetObjectScale(objectPtr, scale);
	}

	// 旧方法 临时修改已经绘制生成的实体模型
	[CallbackMethod("ObjectScaling")]
	internal void CbObjectScaling(string cmd) {
		CheckBeforeExecution(cmd);
		if (GetConfig<bool>("ObjectScale") == false) return; // ignored
		var (objectPtr, scaleX, scaleY, scaleZ) = cmd.ParseArgs<IntPtr, float, float?, float?>((2, null), (3, null));
		SetObjectScaleTemp(objectPtr, scaleX, scaleY ?? scaleX, scaleZ ?? scaleX);
	}

	[CallbackMethod("SetOpacity")]
	internal void CbSetOpacity(string cmd) {
		CheckBeforeExecution(cmd);
		if (GetConfig<bool>("Opacity") == false) return; // ignored
		var (objectPtr, opacity) = cmd.ParseArgs<IntPtr, float>();
		SetOpacity(objectPtr, opacity);
	}

	[CallbackMethod("SetStatusLoopVfx")]
	internal void CbSetStatusLoopVfx(string cmd) {
		CheckBeforeExecution(cmd);
		var (objectPtr, vfxId) = cmd.ParseArgs<IntPtr, ushort>();
		SetStatusLoopVfx(objectPtr, vfxId);
	}

	[CallbackMethod("Redraw")]
	internal void CbRedraw(string cmd) {
		CheckBeforeExecution(cmd);
		var objectPtr = cmd.ParseData<IntPtr>();
		ReDraw(objectPtr);
	}

	[CallbackMethod("SetHighlightColor")]
	internal void CbSetHighlightColor(string cmd) {
		CheckBeforeExecution(cmd);
		var (objectPtr, color) = cmd.ParseArgs<IntPtr, byte>();
		SetHighlightColor(objectPtr, color);
	}

	[CallbackMethod("RemoveStatus")]
	internal void CbRemoveStatus(string cmd) {
		CheckBeforeExecution(cmd);
		var (objectPtr, statusId) = cmd.ParseArgs<IntPtr, ushort>();
		RemoveStatus(objectPtr, statusId);
	}

	[CallbackMethod("EObjAnimation")]
	internal void CbEObjAnimation(string cmd) {
		CheckBeforeExecution(cmd);
		var (objectPtr, animationId, slotMask, context) = cmd.ParseArgs<IntPtr, ushort, ushort, long>((3, 0L));
		EObjAnimation(objectPtr, animationId, slotMask, context);
	}

	[CallbackMethod("PlayActionTimeline")]
	internal void CbPlayActionTimeline(string cmd) {
		CheckBeforeExecution(cmd);
		var (objectPtr, timelineId, a3, a4) = cmd.ParseArgs<IntPtr, ushort, ushort, bool>((2, 0L), (3, false));
		 PlayActionTimeline(objectPtr, timelineId, a3, a4);
	}

	public unsafe void SetPos(IntPtr objectAddress, float x, float y, float z) {
		var pos = new Vector3(x, z, y); // 注意 Y Z 轴交换
		var gameObject = (GameObject*)objectAddress;
		var modelAddress = gameObject->DrawObject;
		RunOnFrameworkThreadV(() => {
			gameObject->Position = pos;
			if (modelAddress != null)
				modelAddress->Position = pos;
		});
	}

	public unsafe void SetDefaultPos(IntPtr objectAddress, float x, float y, float z) =>
		RunOnFrameworkThreadV(() => ((GameObject*)objectAddress)->DefaultPosition = new Vector3(x, z, y)); // 注意 Y Z 轴交换

	public unsafe void SetModelRelPos(IntPtr objectAddress, float dx, float dy, float dz) =>
		RunOnFrameworkThreadV(() => ((GameObject*)objectAddress)->DrawOffset = new Vector3(dx, dz, dy)); // 注意 Y Z 轴交换

	public unsafe void SetHeading(IntPtr objectAddress, float h) {
		var gameObject = (GameObject*)objectAddress;
		var modelAddress = gameObject->DrawObject;
		RunOnFrameworkThreadV(() => {
			gameObject->Rotation = h;
			// 四元数
			modelAddress->Rotation.Y = (float)Math.Sin(h / 2);
			modelAddress->Rotation.W = (float)Math.Cos(h / 2);
		});
	}

	public unsafe void SetDefaultHeading(IntPtr objectAddress, float h) =>
		RunOnFrameworkThreadV(() => ((GameObject*)objectAddress)->DefaultRotation = h);

	public unsafe void Target(IntPtr address, bool hard = true, bool soft = true) {
		CheckIfAnyZeroPtr();
		RunOnFrameworkThreadV(() => {
			if (hard) TargetSystem.Instance()->Target = (GameObject*)address;
			if (soft) TargetSystem.Instance()->SoftTarget = (GameObject*)address;
		});
	}

	/// <summary> 见 status 参数描述 </summary>
	/// <param name="status">
	///     0: "visible" 正常状态 <br />
	///     512/1024: 玩家切换地图时会经历的两种状态，类似 16384 <br />
	///     2048: 不重绘: 有模型无名牌、列表可选；重绘：恢复 0 <br />
	///     4096: 不重绘: 有模型无名牌、列表可选；重绘：不变，刷新模型 <br />
	///     8192: 不重绘：有模型无名牌、不可选；重绘/移动/攻击：恢复 0 <br />
	///     16384: 不重绘：有模型无名牌、不可选；重绘：不变，刷新模型 <br />
	/// </param>
	public unsafe void SetModelStatus(IntPtr objectAddress, int status) =>
		RunOnFrameworkThreadV(() => ((GameObject*)objectAddress)->RenderFlags = (VisibilityFlags)status);

	public unsafe void SetObjectScaleTemp(IntPtr objectAddress, float scaleX, float scaleY, float scaleZ) =>
		RunOnFrameworkThreadV(() => ((GameObject*)objectAddress)->DrawObject->Scale = new Vector3(scaleX, scaleZ, scaleY));

	public unsafe void SetObjectScale(IntPtr objectAddress, float scale) {
		RunOnFrameworkThreadV(() => ((GameObject*)objectAddress)->Scale = scale);
		ReDraw(objectAddress);
	}

	// FFXIVClientStructs/FFXIV/Client/Game/Character/Character.cs    public float Alpha;
	public unsafe void SetOpacity(IntPtr objectAddress, float opacity) =>
		RunOnFrameworkThreadV(() => ((Character*)objectAddress)->Alpha = opacity);

	public unsafe void SetStatusLoopVfx(IntPtr objectAddress, ushort id) {
		RunOnFrameworkThreadV(() => ((GameObject*)objectAddress)->GimmickId = id);
		ReDraw(objectAddress);
	}

	public unsafe void EnableDraw(IntPtr objectAddress) {
		RunOnFrameworkThreadV(() => {
			var character = (GameObject*)objectAddress;
			character->VirtualTable->EnableDraw(character);
		});
	}

	public unsafe void DisableDraw(IntPtr objectAddress) {
		RunOnFrameworkThreadV(() => {
			var character = (GameObject*)objectAddress;
			character->VirtualTable->DisableDraw(character);
		});
	}

	public void ReDraw(IntPtr address) {
		DisableDraw(address);
		EnableDraw(address);
	}

	public unsafe void SetHighlightColor(IntPtr character, byte color) =>
		RunOnFrameworkThreadV(() => ((GameObject*)character)->Highlight((ObjectHighlightColor)color));

	public unsafe void RemoveStatus(IntPtr address, ushort statusId) {
		CheckIfAnyZeroPtr();
		var sm = ((Character*)address)->GetStatusManager();
		RunOnFrameworkThreadV(() => sm->RemoveStatus((int)sm->GetStatusId(statusId)));
	}

	public void EObjAnimation(IntPtr objectPtr, ushort animationId, ushort slotMask, long context = 0) {
		CheckIfAnyZeroPtr();
		var obj = Entity.GetEntities(e => e.Address == objectPtr).FirstOrDefault();
		if (obj == null) {
			WarningLog("[EObjAnimation] 未找到对应的实体");
			return;
		}
		if (obj.Type != EntityType.EventObj) {
			throw new Exception($"[EObjAnimation] 指定实体 \"{obj.Name}\" ({obj.ID:X8}) @ {(long)objectPtr:X} 类型 {obj.Type} 不是 EventObject");
		}
		RunOnFrameworkThreadV(() => EObjAnimationD(objectPtr, animationId, slotMask, context));
	}

	//TODO Verify
	public unsafe bool PlayActionTimeline(IntPtr objectPtr, ushort introId, ushort loopId = 0, bool a4 = false) {
		// a4: should skip mount/submodel timeline
		// 原函数是 实体->TimelineContainer 的方法，这里封装改用了实体本身的地址
		CheckIfAnyZeroPtr();
		var chara = (Character*)objectPtr;
		RunOnFrameworkThreadV(() => {
			chara->Timeline.BaseOverride = (ushort)(a4 ? 1 : 0);
			chara->Timeline.PlayActionTimeline(introId, loopId);
		});
		return true;
	}
}