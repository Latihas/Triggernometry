using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Triggernometry.Core;

namespace Triggernometry.PluginBridges;

public static class BridgeCafe {
	private static object cafeStore;

	private static Type dependencyObjectType;
	private static Type dispatcherObjectType;
	private static Type visualTreeHelperType;
	private static Type listViewType;
	private static Type notifyCollectionChangedType;

	private static object observedSource;
	private static Delegate collectionChangedHandler;

	static BridgeCafe() {
		cafeStore = RealPlugin.InstanceHook(null, "CafeStore.CafeStorePlugin")?.pluginObj;
		if (cafeStore == null) return;

		dependencyObjectType = Type.GetType("System.Windows.DependencyObject, WindowsBase")
		                       ?? throw new Exception("没找到 DependencyObject");

		dispatcherObjectType = Type.GetType("System.Windows.Threading.DispatcherObject, WindowsBase")
		                       ?? throw new Exception("没找到 DispatcherObject");

		visualTreeHelperType = Type.GetType("System.Windows.Media.VisualTreeHelper, PresentationCore")
		                       ?? throw new Exception("没找到 VisualTreeHelper");

		listViewType = Type.GetType("System.Windows.Controls.ListView, PresentationFramework")
		               ?? throw new Exception("没找到 ListView");

		notifyCollectionChangedType = Type.GetType("System.Collections.Specialized.NotifyCollectionChangedEventHandler, System")
		                              ?? throw new Exception("没找到 NotifyCollectionChangedEventHandler");
	}

	public static string AutoRemoveTriggernometryFromCafeStore() {
		if (cafeStore == null)
			return "CafeStore 不存在或未加载。";

		var mainView = GetMainView();
		var result = "";
		Exception error = null;

		void action() {
			try {
				var source = GetPluginListSource(mainView);

				UnsubscribeCollectionChanged_NoThrow();

				var method = typeof(BridgeCafe).GetMethod(
					nameof(OnPluginCollectionChanged),
					BindingFlags.Static | BindingFlags.NonPublic);

				collectionChangedHandler = Delegate.CreateDelegate(
					notifyCollectionChangedType,
					method);

				source.GetType().GetEvent("CollectionChanged")
					?.AddEventHandler(source, collectionChangedHandler);

				observedSource = source;

				// 安装监听后，立刻删一次
				var target = FindOldTriggernometryEntry(source);
				if (target != null) {
					result = "已移除列表项，";
					source.Remove(target);
				}

				result += "开始监听 CafeStore 插件列表刷新。";
			} catch (Exception ex) {
				error = ex;
			}
		}

		InvokeByDispatcher(mainView, action);

		if (error != null)
			throw error;

		return result;
	}

	private static void OnPluginCollectionChanged(object sender, object e) {
		try {
			var source = sender as IList;
			if (source == null)
				return;

			var target = FindOldTriggernometryEntry(source);
			if (target != null)
				source.Remove(target);
		} catch {
		}
	}

	private static void UnsubscribeCollectionChanged_NoThrow() {
		try {
			if (observedSource == null || collectionChangedHandler == null)
				return;

			observedSource.GetType().GetEvent("CollectionChanged")
				?.RemoveEventHandler(observedSource, collectionChangedHandler);
		} catch {
		}

		observedSource = null;
		collectionChangedHandler = null;
	}

	private static object GetMainView() {
		var mainView = cafeStore.GetType()
			               .GetField("_mainView", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			               ?.GetValue(cafeStore)
		               ?? throw new Exception("没找到 mainView");

		if (!dependencyObjectType.IsInstanceOfType(mainView))
			throw new Exception("mainView 不是 DependencyObject");

		return mainView;
	}

	private static IList GetPluginListSource(object mainView) {
		var listView = GetVisualTree(mainView)
			               .FirstOrDefault(x => listViewType.IsInstanceOfType(x))
		               ?? throw new Exception("没找到 ListView");

		return GetPropertyValue(listView, "ItemsSource") as IList
		       ?? throw new Exception("ItemsSource 不是 IList");
	}

	private static object FindOldTriggernometryEntry(IList source) {
		return source.Cast<object>()
			.FirstOrDefault(x => GetFriendlyName(x)?.StartsWith("Triggernometry") == true);
	}

	private static void InvokeByDispatcher(object mainView, Action action) {
		if (mainView == null)
			throw new ArgumentNullException(nameof(mainView));
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		var dispatcher = dispatcherObjectType
			                 .GetProperty("Dispatcher", BindingFlags.Public | BindingFlags.Instance)
			                 .GetValue(mainView, null)
		                 ?? throw new Exception("没找到 Dispatcher");

		dispatcher.GetType().InvokeMember(
			"Invoke",
			BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance,
			null,
			dispatcher,
			[action]);
	}

	private static object GetPropertyValue(object obj, string propertyName) {
		if (obj == null)
			return null;

		var prop = obj.GetType().GetProperty(
			propertyName,
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

		return prop?.GetValue(obj, null);
	}

	private static string GetFriendlyName(object item) {
		if (item == null)
			return null;

		var meta = GetPropertyValue(item, "Meta");
		if (meta == null)
			return null;

		return GetPropertyValue(meta, "FriendlyName") as string;
	}

	private static IEnumerable<object> GetVisualTree(object root) {
		if (root == null)
			yield break;

		yield return root;

		var count = (int)visualTreeHelperType.InvokeMember(
			"GetChildrenCount",
			BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static,
			null,
			null,
			[root]);

		for (var i = 0; i < count; i++) {
			var child = visualTreeHelperType.InvokeMember(
				"GetChild",
				BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static,
				null,
				null,
				[root, i]);

			foreach (var x in GetVisualTree(child))
				yield return x;
		}
	}
}