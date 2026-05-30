using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.InstanceContent;
using FFXIVClientStructs.FFXIV.Client.Graphics.Environment;
using Triggernometry.Expressions.String.Utils;

namespace Triggernometry.PluginBridges.BridgeNamazu.Modules;

public partial class EnvironmentEffectModule : ModuleBase {
	public unsafe delegate void MapEffectOldFunction(ContentDirector* p1, uint p2, ushort p3, ushort p4);

	public MapEffectOldFunction MapEffectOldD;

	public unsafe delegate bool MapEffectFunction(ContentDirector* p1, uint p2, ushort p3);

	public MapEffectFunction MapEffectD;


	// Hyperborea/Hyperborea/Utils.cs
	// GetMapEffectModule() => *(nint*)(((nint)EventFramework.Instance()) + 344);
	// FFCS: ContentDirector (没提供偏移)
	/// <summary> 可能为 0，代表当前地图不存在 Director </summary>
	public unsafe ContentDirector* ContentDirector => EventFramework.Instance()->DirectorModule.ActiveContentDirector;

	public EnvironmentEffectModule() {
		ScanMethod = () => {
			MapEffectOldD = Marshal.GetDelegateForFunctionPointer<MapEffectOldFunction>(Scanner.TryScan("44 0F B7 40 ? E9 * * * * C3", nameof(MapEffectOldD)));
			MapEffectD = Marshal.GetDelegateForFunctionPointer<MapEffectFunction>(Scanner.TryScan("E8 * * * * 3C ? 75 ? 80 64 B3 ? ?", nameof(MapEffectD)));
		};
	}

	private static readonly Regex _mapEffectRegex = MapEffectRegex();

	[CallbackMethod("MapEffect")]
	internal void CbMapEffect(string multiLineCmd) {
		CheckBeforeExecution(multiLineCmd);
		if (GetConfig<bool>("MapEffect") == false) return; // ignored
		var cmds = multiLineCmd
			.Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries)
			.Where(s => !string.IsNullOrWhiteSpace(s) && !s.StartsWith("//"))
			.Select(s => s.Trim());
		var args = new List<(uint, ushort?, ushort)>();
		foreach (var command in cmds) {
			try {
				if (command.Contains(',')) {
					var (index, unknownFlag, flag) = command.ParseArgs<uint, ushort?, ushort?>((2, null));
					// 支持的参数格式如 (index, unknownFlag, flag)，或 (index, flag)，因为游戏中实际并未使用 unknownFlag
					if (flag == null)
						(unknownFlag, flag) = (flag, unknownFlag);
					args.Add((index, unknownFlag, flag.Value));
				} else // 支持格式如 00020001:0F, 00020001|0F   或省略未使用的参数，如 0002:0F
				{
					var match = _mapEffectRegex.Match(command);
					if (match.Success) {
						var flag = ushort.Parse(match.Groups["flag"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
						var unknownFlag = match.Groups["unknownFlag"].Success
							? ushort.Parse(match.Groups["unknownFlag"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)
							: (ushort?)null;
						var index = uint.Parse(match.Groups["index"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
						args.Add((index, unknownFlag, flag));
					} else {
						throw new Exception($"{command} 参数格式无法识别");
					}
				}
			} catch (Exception ex) {
				ErrorLog($"[鲶鱼精邮差扩展] MapEffect 参数错误：{ex.Message}");
			}
		}

		foreach (var (index, unknownFlag, flag) in args) {
			if (!unknownFlag.HasValue) {
				NamazuLog($"[MapEffect] index = {index}, flag = {flag} ({flag:X4}????:{index:X2})");
				MapEffect(index, flag);
			} else {
				NamazuLog($"[MapEffect] index = {index}, flag = {flag} ({flag:X4}{unknownFlag:X4}:{index:X2})");
#pragma warning disable CS0618 // 使用弃用方法的警告
				MapEffectOld(index, unknownFlag.Value, flag);
#pragma warning restore CS0618
			}
		}
	}

	/// <summary> MapEffect 底层函数。 </summary>
	/// <returns> 是否调用成功。</returns>
	public unsafe bool MapEffect(uint index, ushort flag) {
		CheckIfAnyZeroPtr();
		var contentDirectorPtr = ContentDirector;
		if (contentDirectorPtr != null) {
			var success = RunOnFrameworkThread(() => MapEffectD(contentDirectorPtr, index, flag));
			if (!success) {
				WarningLog($"[鲶鱼精邮差扩展] 当前地图 {BridgeFFXIV.ZoneID} 中 MapEffect ({index}, {flag}) 调用失败。");
			}
			return success;
		}
		ErrorLog($"[鲶鱼精邮差扩展] 当前地图 {BridgeFFXIV.ZoneID} 不存在 Director，无法调用 MapEffect ({index}, {flag})。");
		return false;
	}

	/// <summary> <see cref="MapEffect" /> 的上一层函数，第二个参数并未实际使用。 </summary>
	[Obsolete("Use MapEffect(uint index, ushort flag)")]
	public unsafe void MapEffectOld(uint index, ushort unknownFlag, ushort flag) {
		CheckIfAnyZeroPtr();
		var contentDirectorPtr = ContentDirector;
		if (contentDirectorPtr != null) {
			RunOnFrameworkThreadV(() => MapEffectOldD(contentDirectorPtr, index, unknownFlag, flag));
		} else {
			ErrorLog($"[鲶鱼精邮差扩展] 当前地图 {BridgeFFXIV.ZoneID} 不存在 Director，无法调用 MapEffect (Old) ({index}, {unknownFlag}, {flag})。");
		}
	}

	[CallbackMethod("ChangeWeather")]
	internal void CbChangeWeather(string command) {
		var weatherId = command.ParseData<byte>();
		CheckBeforeExecution(command);
		NamazuLog($"[ChangeWeather] {weatherId}");
		ChangeWeather(weatherId);
	}

	// FFXIVClientStructs/FFXIV/Client/Graphics/Environment/EnvManager.cs
	public unsafe void ChangeWeather(byte weatherId) {
		CheckIfAnyZeroPtr();
		var envManagerPtr = EnvManager.Instance();
		RunOnFrameworkThreadV(() => {
			envManagerPtr->ActiveWeather = weatherId; // ActiveWeather
			envManagerPtr->TransitionTime = 1; // TransitionTime
		});
	}

	[GeneratedRegex(@"^(?<flag>[0-9A-Fa-f]{4})(?<unknownFlag>[0-9A-Fa-f]{4})?[:|](?<index>[0-9A-Fa-f]{1,8})$", RegexOptions.Compiled)]
	private static partial Regex MapEffectRegex();
}