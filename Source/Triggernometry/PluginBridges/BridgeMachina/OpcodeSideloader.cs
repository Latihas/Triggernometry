using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using FFXIV_ACT_Plugin.Network;
using Machina.FFXIV;
using Machina.FFXIV.Headers;
using Machina.FFXIV.Headers.Opcodes;
using Triggernometry.Core;

namespace Triggernometry.PluginBridges.BridgeMachina;

public static partial class OpcodeSideloader {
	public static void Callback(object _, string rawOpcodes) {
		try {
			SideloadOpcodes(rawOpcodes);
		} catch (Exception ex) {
			LogError("Failed to sideload opcodes: " + ex.Message);
		}
	}

	private static void Log(string message)
		=> RealPlugin.Instance.UnfilteredAddToLog(RealPlugin.DebugLevelEnum.Custom,
			$"[{nameof(OpcodeSideloader)}] {message}");

	private static void LogError(string message)
		=> RealPlugin.Instance.UnfilteredAddToLog(RealPlugin.DebugLevelEnum.Error,
			$"[{nameof(OpcodeSideloader)}] {message}");

	private static Exception ReflectFail(string name) => new($"Reflection lookup failed: {name}");

	public static void SideloadOpcodes(string rawSideloadOpcodes) {
		var original = GetCurrentOpcodes() ?? throw ReflectFail("CurrentOpcodes");
		var sideload = GetSideloadOpcodes(rawSideloadOpcodes);

		var updated = new Dictionary<string, (ushort, ushort)>(StringComparer.OrdinalIgnoreCase);
		var extra = new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase);

		foreach (var sideloadKv in sideload) {
			// 忽略大小写匹配 key
			var matchedKey = original.Keys.FirstOrDefault(k => string.Equals(k, sideloadKv.Key, StringComparison.OrdinalIgnoreCase));
			if (matchedKey != null) {
				var oldValue = original[matchedKey];
				var newValue = sideloadKv.Value;
				original[matchedKey] = newValue;
				updated[matchedKey] = (oldValue, newValue);
			} else {
				extra[sideloadKv.Key] = sideloadKv.Value;
			}
		}

		var untouched = original.Where(kv => !updated.ContainsKey(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);

		// ---- 输出对比报告 ----
		var sb = new StringBuilder();
		sb.AppendLine("Opcodes sideload initiated successfully:\n");

		if (updated.Count > 0) {
			sb.AppendLine($"Updated opcodes × {updated.Count}:");
			foreach (var kv in updated
				         .Select(kv => new { kv, match = myRegex.Match(kv.Key) })
				         .OrderBy(x => x.match.Groups[1].Value, StringComparer.OrdinalIgnoreCase)
				         .ThenBy(x => int.Parse("0" + x.match.Groups[2].Value)) // 这样可以让 XXX4 XXX8 排在 XXX16 前面
				         .Select(x => x.kv)) {
				var oldVal = kv.Value.Item1;
				var newVal = kv.Value.Item2;
				sb.AppendLine($"  - {kv.Key}: 0x{oldVal:X} => 0x{newVal:X}");
			}
			sb.AppendLine();
		}

		if (untouched.Count > 0) {
			sb.AppendLine($"Untouched opcodes × {untouched.Count}:");
			foreach (var kv in untouched
				         .Select(kv => new { kv, match = myRegex.Match(kv.Key) })
				         .OrderBy(x => x.match.Groups[1].Value, StringComparer.OrdinalIgnoreCase)
				         .ThenBy(x => int.Parse("0" + x.match.Groups[2].Value))
				         .Select(x => x.kv))
				sb.AppendLine($"  - {kv.Key}: 0x{kv.Value:X}");
			sb.AppendLine();
		}

		if (extra.Count > 0) {
			sb.AppendLine($"Extra opcodes × {extra.Count}:");
			foreach (var kv in extra
				         .Select(kv => new { kv, match = myRegex.Match(kv.Key) })
				         .OrderBy(x => x.match.Groups[1].Value, StringComparer.OrdinalIgnoreCase)
				         .ThenBy(x => int.Parse("0" + x.match.Groups[2].Value))
				         .Select(x => x.kv))
				sb.AppendLine($"  - {kv.Key}: 0x{kv.Value:X}");
			sb.AppendLine();
		}

		Log(sb.ToString());

		// ---- 真正让修改生效 ----
		ApplyOpcodes(original);

		Log("Machina opcode structures updated successfully.");
	}

	/// <summary>
	///     接受输入如： <br />
	///     ActorCast | 36F <br />
	///     ActorControl | 1CD <br />
	///     ...
	/// </summary>
	public static Dictionary<string, ushort> GetSideloadOpcodes(string rawOpcodes) {
		if (string.IsNullOrWhiteSpace(rawOpcodes))
			throw new Exception("No opcode data provided to OpcodeSideloader callback.");

		var dict = new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase);
		var lines = rawOpcodes
			.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
			.Select(line => line.Trim())
			.Where(line => line.Length > 0 && !line.StartsWith("//") && !line.StartsWith("#"));

		foreach (var line in lines) {
			var parts = line.Split('|');
			if (parts.Length != 2)
				throw new Exception("Invalid line format: " + line);

			var key = parts[0].Trim();
			var valStr = parts[1].Trim();

			if (key.Length == 0 || valStr.Length == 0)
				throw new Exception("Invalid line (empty key or value): " + line);

			if (!ushort.TryParse(valStr, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
				throw new Exception("Unable to parse hex value: " + valStr);

			dict[key] = value;
		}

		return dict;
	}

	#region Machina

	public static Dictionary<string, ushort> GetCurrentOpcodes() {
		var rawOpcodes = OpcodeManager.Instance.CurrentOpcodes;
		var result = new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase);
		foreach (var entry in rawOpcodes) {
			if (entry.Key == null || entry.Value == null) continue;
			result[entry.Key] = Convert.ToUInt16(entry.Value, CultureInfo.InvariantCulture);
		}
		return result;
	}

	public static void ApplyOpcodes(Dictionary<string, ushort> replaceDict) {
		var opcodeManagerInstance = OpcodeManager.Instance;
		var regionName = GetCurrentMachinaRegionName();
		// 更新 OpcodeManager._opcodes[当前区域]
		UpdateOpcodeManagerBackingStore(opcodeManagerInstance, regionName, replaceDict);
		// 让 OpcodeManager.CurrentOpcodes 切回当前区域，并读到刚写入的值
		SetOpcodeManagerRegion(regionName);
		// 更新 Server_MessageType 静态字段
		var ServerMessageType = typeof(Server_MessageType);
		var internalValueProp = ServerMessageType.GetProperty("InternalValue", BindingFlags.Public | BindingFlags.Instance)
		                        ?? throw ReflectFail("Server_MessageType.InternalValue Property");
		foreach (var field in ServerMessageType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)) {
			var isInitOnly = typeof(FieldInfo).GetField("m_isInitOnly", BindingFlags.NonPublic | BindingFlags.Instance);
			isInitOnly?.SetValue(field, false);
			var instance = Activator.CreateInstance(ServerMessageType);
			if (replaceDict.TryGetValue(field.Name, out var newVal))
				internalValueProp.SetValue(instance, newVal);
			field.SetValue(null, instance);
		}
		// 不要手动重建 _packetHandlers；让 FFXIV_ACT_Plugin 自己按当前版本结构重载
		RefreshPacketHandlers();
	}

	private static string GetCurrentMachinaRegionName() => OpcodeManager.Instance.GameRegion.ToString();

	private static GameRegion GetMachinaRegion(string regionName) {
		try {
			return Enum.Parse<GameRegion>(regionName);
		} catch (Exception ex) {
			throw new Exception($"[OpcodeSideloader] Unsupported Machina GameRegion: {regionName}", ex);
		}
	}

	private static void SetOpcodeManagerRegion(string regionName) {
		OpcodeManager.Instance.SetRegion(GetMachinaRegion(regionName));
	}

	private static void UpdateOpcodeManagerBackingStore(object opcodeManagerInstance, string regionName, Dictionary<string, ushort> opcodes) {
		OpcodeManager.Instance._opcodes[GetMachinaRegion(regionName)] = new Dictionary<string, ushort>(opcodes);
	}

	private static void RefreshPacketHandlers() => GetPacketHandlerMediator().LoadPacketHandlers();

	private static PacketHandlerMediator GetPacketHandlerMediator() =>
		(PacketHandlerMediator)BridgeFFXIV.GetInstance()._iocContainer.GetService(typeof(PacketHandlerMediator))!;

	[GeneratedRegex(@"^(.+?)(\d*)$")]
	private static partial Regex MyRegex();

	private static readonly Regex myRegex = MyRegex();

	#endregion Machina
}