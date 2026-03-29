using System;
using System.Collections.Generic;
using System.Linq;
using RainbowMage.OverlayPlugin.NetworkProcessors;
using Triggernometry.Core;
using Triggernometry.Expressions.Maths;

namespace Triggernometry.PluginBridges;

public static class ActorControlPatcher {
	public static void RegisterCategoriesCallback(object _, string data) {
		var newCategories = data.Split(',').Select(raw => (ushort)MathParser.Parse(raw)).ToArray();
		RegisterCategories(newCategories);
	}

	public static void RegisterCategories(ushort[] newCategories) {
		var enumType = typeof(Server_ActorControlCategory);

		// original categories
		var original = LineActorControlExtra.AllowedActorControlCategories;
		var originalObjects = original.Cast<object>().ToList();

		// filter new categories
		var toAdd = new List<object>();
		foreach (var val in newCategories) {
			var enumValue = Enum.ToObject(enumType, val);
			if (!originalObjects.Any(v => v.Equals(enumValue))) {
				toAdd.Add(enumValue);
			}
		}

		// overwrite if any new categories
		if (toAdd.Count > 0) {
			var newArray = Array.CreateInstance(enumType, original.Length + toAdd.Count);

			Array.Copy(original, newArray, original.Length);
			for (var i = 0; i < toAdd.Count; i++) {
				newArray.SetValue(toAdd[i], original.Length + i);
			}
			LineActorControlExtra.AllowedActorControlCategories = newArray.Cast<Server_ActorControlCategory>().ToArray();
			// field.SetValue(null, newArray);

			RealPlugin.Instance.UnfilteredAddToLog(RealPlugin.DebugLevelEnum.Info,
				$"已添加 {toAdd.Count} 个 ActorControl 新分类：{string.Join(", ", toAdd.Select(o => $"0x{(ushort)o:X4}"))}");
		}
	}
}