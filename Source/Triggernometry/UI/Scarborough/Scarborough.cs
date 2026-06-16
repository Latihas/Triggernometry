using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Dalamud.Plugin.Services;
using Scarborough;
using Scarborough.Drawing;
using Scarborough.PInvoke;
using Triggernometry.Core;
using Triggernometry.Localization;
using TriggernometryProxy;

namespace Triggernometry.UI;

public class Scarborough : IDisposable {
	private long CurOrdinal = 1;

	public Dictionary<string, ScarboroughImage> imageitems = new();
	private double lag;

	private DateTime prevTick = DateTime.Now;
	private RenderCollection rc = new();
	public Dictionary<string, ScarboroughText> textitems = new();

	public Scarborough() {
		User32.InitializeWindowClass();
		RenderingActive = true;
		ProxyPlugin.Framework.Update += Render;
	}

	internal bool RenderingActive { get; set; }
	internal RealPlugin plug { get; set; }
	private ConcurrentQueue<ItemAction> ItemActions { get; set; } = new();

	public void Dispose() {
		ProxyPlugin.Framework.Update -= Render;
		foreach (var text in textitems) {
			text.Value._graphics.BeginScene();
			text.Value._graphics.ClearScene(new Color());
			text.Value._graphics.EndScene();
			text.Value.Dispose();
		}
		DeactivateAllText();
	}

	private void ExecuteActions() {
		while (!ItemActions.IsEmpty) {
			try {
				if (!ItemActions.TryDequeue(out var ia)) continue;
				ExecuteAction(ia);
				ia.Completed?.Set();
			} catch (Exception) {
			}
		}
	}

	internal void ExecuteAction(ItemAction ia) {
		switch (ia.Action) {
			case ItemAction.ActionTypeEnum.RenderingOn:
				RenderingActive = true;
				break;
			case ItemAction.ActionTypeEnum.RenderingOff:
				RenderingActive = false;
				break;
			case ItemAction.ActionTypeEnum.Activate: {
				switch (ia.Item) {
					case ScarboroughImage item:
						item.Name = ia.Id;
						ActivateImage(ia.Id, item);
						break;
					case ScarboroughText text:
						text.Name = ia.Id;
						ActivateText(ia.Id, text);
						break;
				}
			}
				break;
			case ItemAction.ActionTypeEnum.DeactivateRegex: {
				var rex = new Regex(ia.Id);
				var toRem = new List<string>();
				switch (ia.ItemType) {
					case ItemAction.ItemTypeEnum.Image: {
						toRem.AddRange(from sx in imageitems where rex.IsMatch(sx.Key) select sx.Key);
						foreach (var rem in toRem) {
							var si = imageitems[rem];
							imageitems.Remove(rem);
							if (si != null) {
								si.Dispose();
							}
						}
					}
						break;
					case ItemAction.ItemTypeEnum.Text: {
						toRem.AddRange(from sx in textitems where rex.IsMatch(sx.Key) select sx.Key);
						foreach (var rem in toRem) {
							var si = textitems[rem];
							textitems.Remove(rem);
							if (si != null) {
								si.Dispose();
							}
						}
					}
						break;
				}
			}
				break;
			case ItemAction.ActionTypeEnum.DeactivateTrigger: {
				var toRem = new List<string>();
				switch (ia.ItemType) {
					case ItemAction.ItemTypeEnum.Image: {
						toRem.AddRange(from sx in imageitems where sx.Value.ctx.Trigger.Id.ToString() == ia.Id select sx.Key);
						foreach (var rem in toRem) {
							ScarboroughImage si = null;
							si = imageitems[rem];
							imageitems.Remove(rem);
							if (si != null) {
								si.Dispose();
							}
						}
					}
						break;
					case ItemAction.ItemTypeEnum.Text: {
						toRem.AddRange(from sx in textitems where sx.Value.ctx.Trigger.Id.ToString() == ia.Id select sx.Key);
						foreach (var rem in toRem) {
							ScarboroughText si = null;
							si = textitems[rem];
							textitems.Remove(rem);
							if (si != null) {
								si.Dispose();
							}
						}
					}
						break;
				}
			}
				break;
			case ItemAction.ActionTypeEnum.Deactivate: {
				switch (ia.ItemType) {
					case ItemAction.ItemTypeEnum.Image: {
						ScarboroughImage si = null;
						if (imageitems.TryGetValue(ia.Id, out var value)) {
							si = value;
							imageitems.Remove(ia.Id);
						}
						if (si != null) {
							si.Dispose();
						}
					}
						break;
					case ItemAction.ItemTypeEnum.Text: {
						ScarboroughText si = null;
						if (textitems.TryGetValue(ia.Id, out var value)) {
							si = value;
							textitems.Remove(ia.Id);
						}
						if (si != null) {
							si.Dispose();
						}
					}
						break;
				}
			}
				break;
			case ItemAction.ActionTypeEnum.DeactivateAll: {
				switch (ia.ItemType) {
					case ItemAction.ItemTypeEnum.Image: {
						var toRem = new List<ScarboroughImage>();
						foreach (var si in imageitems) {
							toRem.Add(si.Value);
						}
						imageitems.Clear();
						foreach (var si in toRem) {
							si.Dispose();
						}
					}
						break;
					case ItemAction.ItemTypeEnum.Text: {
						var toRem = new List<ScarboroughText>();
						foreach (var si in textitems) {
							toRem.Add(si.Value);
						}
						textitems.Clear();
						foreach (var si in toRem) {
							si.Dispose();
						}
					}
						break;
				}
			}
				break;
		}
	}

	public void Activate(string id, ScarboroughItem si) {
		ItemActions.Enqueue(new ItemAction {
			Action = ItemAction.ActionTypeEnum.Activate,
			Id = id,
			Item = si
		});
	}

	public void Deactivate(string id, ItemAction.ItemTypeEnum it) {
		ItemActions.Enqueue(new ItemAction {
			Action = ItemAction.ActionTypeEnum.Deactivate,
			Id = id,
			ItemType = it
		});
	}

	public void DeactivateRegex(string rex, ItemAction.ItemTypeEnum it) {
		ItemActions.Enqueue(new ItemAction {
			Action = ItemAction.ActionTypeEnum.DeactivateRegex,
			Id = rex,
			ItemType = it
		});
	}

	public void DeactivateTrigger(Trigger t, ItemAction.ItemTypeEnum it) {
		ItemActions.Enqueue(new ItemAction {
			Action = ItemAction.ActionTypeEnum.DeactivateTrigger,
			Id = t.Id.ToString(),
			ItemType = it
		});
	}

	private long GetNextOrdinal() => Interlocked.Increment(ref CurOrdinal);

	private void ActivateImage(string id, ScarboroughImage si) {
		var existing = GetImage(id);
		if (existing != null) {
			existing.Ordinal = GetNextOrdinal();
			existing.Left = si.EvaluateNumericExpression(si.ctx, si.InitXExpression);
			existing.Top = si.EvaluateNumericExpression(si.ctx, si.InitYExpression);
			existing.Width = si.EvaluateNumericExpression(si.ctx, si.InitWExpression);
			existing.Height = si.EvaluateNumericExpression(si.ctx, si.InitHExpression);
			existing.Opacity = string.IsNullOrWhiteSpace(si.InitOExpression) ? 100 : si.EvaluateNumericExpression(si.ctx, si.InitOExpression);
			existing.UpdateXExpression = si.UpdateXExpression;
			existing.UpdateYExpression = si.UpdateYExpression;
			existing.UpdateWExpression = si.UpdateWExpression;
			existing.UpdateHExpression = si.UpdateHExpression;
			existing.UpdateOExpression = si.UpdateOExpression;
			existing.TTLExpression = si.TTLExpression;
			existing.Display = si.Display;
			existing.ctx = si.ctx;
			si.ImageFilename = si.EvaluateStringExpression(si.ctx, si.ImageExpression);
			if (existing.ImageFilename != si.ImageFilename) {
				existing.ImageFilename = si.ImageFilename;
				existing.NeedImage = true;
			}
		} else {
			si.Ordinal = GetNextOrdinal();
			si.NeedImage = true;
			si.plug = plug;
			si.ImageFilename = si.EvaluateStringExpression(si.ctx, si.ImageExpression);
			si.Left = si.EvaluateNumericExpression(si.ctx, si.InitXExpression);
			si.Top = si.EvaluateNumericExpression(si.ctx, si.InitYExpression);
			si.Width = si.EvaluateNumericExpression(si.ctx, si.InitWExpression);
			si.Height = si.EvaluateNumericExpression(si.ctx, si.InitHExpression);
			si.Opacity = string.IsNullOrWhiteSpace(si.InitOExpression) ? 100 : si.EvaluateNumericExpression(si.ctx, si.InitOExpression);
			imageitems[id] = si;
		}
	}

	private void ActivateText(string id, ScarboroughText si) {
		var existing = GetText(id);
		if (existing != null) {
			existing.Ordinal = GetNextOrdinal();
			existing.Left = si.EvaluateNumericExpression(si.ctx, si.InitXExpression);
			existing.Top = si.EvaluateNumericExpression(si.ctx, si.InitYExpression);
			existing.Width = si.EvaluateNumericExpression(si.ctx, si.InitWExpression);
			existing.Height = si.EvaluateNumericExpression(si.ctx, si.InitHExpression);
			existing.Opacity = string.IsNullOrWhiteSpace(si.InitOExpression) ? 100 : si.EvaluateNumericExpression(si.ctx, si.InitOExpression);
			existing.UpdateXExpression = si.UpdateXExpression;
			existing.UpdateYExpression = si.UpdateYExpression;
			existing.UpdateWExpression = si.UpdateWExpression;
			existing.UpdateHExpression = si.UpdateHExpression;
			existing.UpdateOExpression = si.UpdateOExpression;
			existing.TTLExpression = si.TTLExpression;
			existing.TextAlignment = si.TextAlignment;
			existing.UseOutline = si.UseOutline;
			existing.TextColor = si.TextColor;
			existing.OutlineColor = si.OutlineColor;
			existing.BackgroundColor = si.BackgroundColor;
			existing.FontName = si.FontName;
			existing.FontSize = si.FontSize;
			existing.FontStyle = si.FontStyle;
			existing.Text = si.EvaluateStringExpression(si.ctx, si.TextExpression);
			existing.TextExpression = si.TextExpression;
			existing.ctx = si.ctx;
			existing.NeedFont = true;
		} else {
			si.Ordinal = GetNextOrdinal();
			si.plug = plug;
			si.Left = si.EvaluateNumericExpression(si.ctx, si.InitXExpression);
			si.Top = si.EvaluateNumericExpression(si.ctx, si.InitYExpression);
			si.Width = si.EvaluateNumericExpression(si.ctx, si.InitWExpression);
			si.Height = si.EvaluateNumericExpression(si.ctx, si.InitHExpression);
			si.Opacity = string.IsNullOrWhiteSpace(si.InitOExpression) ? 100 : si.EvaluateNumericExpression(si.ctx, si.InitOExpression);
			si.Text = si.EvaluateStringExpression(si.ctx, si.TextExpression);
			si.NeedFont = true;
			textitems[id] = si;
		}
	}

	public ScarboroughImage? GetImage(string id) => imageitems.GetValueOrDefault(id);

	public ScarboroughText? GetText(string id) => textitems.GetValueOrDefault(id);

	public void DeactivateAllImages() {
		ItemActions.Enqueue(new ItemAction {
			Action = ItemAction.ActionTypeEnum.DeactivateAll,
			ItemType = ItemAction.ItemTypeEnum.Image
		});
	}

	public void DeactivateAllText() {
		ItemActions.Enqueue(new ItemAction {
			Action = ItemAction.ActionTypeEnum.DeactivateAll,
			ItemType = ItemAction.ItemTypeEnum.Text
		});
	}

	private void ProcessMessages(IEnumerable<DeferredMessage> msgs) {
		foreach (var msg in msgs) {
			if (msg.ctx != null) {
				msg.ctx.Trigger.AddToLog(msg.level, msg.Message);
			} else {
				plug.FilteredAddToLog(msg.level, msg.Message);
			}
		}
	}

	private void UpdateImages(int numTicks, ref RenderCollection rc) {
		var toRem = new List<string>();
		var messages = new List<DeferredMessage>();
		toRem.Clear();
		foreach (var si in imageitems) {
			try {
				if (!si.Value.Logic(numTicks)) {
					toRem.Add(si.Key);
				} else {
					if (si.Value.Changed) {
						si.Value.NeedRender = true;
						si.Value.Changed = false;
					}
					rc.Add(si.Value);
				}
			} catch (Exception ex) {
				if (si.Value.ctx is { Trigger: not null }) {
					messages.Add(new DeferredMessage {
							ctx = si.Value.ctx,
							plug = plug,
							level = RealPlugin.DebugLevelEnum.Error,
							Message = I18n.Translate("internal/AuraContainer/updateerror", $"Deactivating aura '{si.Key}' from trigger '{si.Value.ctx.Trigger.LogName}' due to update exception: {ex.Message}")
						}
					);
				} else {
					messages.Add(new DeferredMessage {
							ctx = null,
							plug = plug,
							level = RealPlugin.DebugLevelEnum.Error,
							Message = I18n.Translate("internal/AuraContainer/updateerror", $"Deactivating aura '{si.Key}' due to update exception: {ex.Message}")
						}
					);
				}
				toRem.Add(si.Key);
			}
		}
		if (toRem.Count > 0) {
			foreach (var si in toRem) {
				var sit = imageitems[si];
				if (sit.ctx is { Trigger: not null }) {
					messages.Add(new DeferredMessage {
							ctx = sit.ctx,
							plug = plug,
							level = RealPlugin.DebugLevelEnum.Verbose,
							Message = I18n.Translate("internal/AuraContainer/closingaura", "Closing aura window")
						}
					);
				} else {
					messages.Add(new DeferredMessage {
							ctx = null,
							plug = plug,
							level = RealPlugin.DebugLevelEnum.Verbose,
							Message = I18n.Translate("internal/AuraContainer/closingaura", "Closing aura window")
						}
					);
				}
				imageitems.Remove(si);
				sit.Dispose();
			}
		}
		ProcessMessages(messages);
	}

	private void UpdateText(int numTicks, ref RenderCollection rc) {
		var toRem = new List<string>();
		var messages = new List<DeferredMessage>();
		toRem.Clear();
		foreach (var si in textitems) {
			try {
				if (!si.Value.Logic(numTicks)) {
					toRem.Add(si.Key);
				} else {
					if (si.Value.Changed) {
						si.Value.NeedRender = true;
						si.Value.Changed = false;
					}
					rc.Add(si.Value);
				}
			} catch (Exception ex) {
				if (si.Value.ctx is { Trigger: not null }) {
					messages.Add(new DeferredMessage {
							ctx = si.Value.ctx,
							plug = plug,
							level = RealPlugin.DebugLevelEnum.Error,
							Message = I18n.Translate("internal/AuraContainer/updateerror", $"Deactivating aura '{si.Key}' from trigger '{si.Value.ctx.Trigger.LogName}' due to update exception: {ex.Message}")
						}
					);
				} else {
					messages.Add(new DeferredMessage {
							ctx = null,
							plug = plug,
							level = RealPlugin.DebugLevelEnum.Error,
							Message = I18n.Translate("internal/AuraContainer/updateerror", $"Deactivating aura '{si.Key}' due to update exception: {ex.Message}")
						}
					);
				}
				toRem.Add(si.Key);
			}
		}
		if (toRem.Count > 0) {
			foreach (var si in toRem) {
				var sit = textitems[si];
				if (sit.ctx is { Trigger: not null }) {
					messages.Add(new DeferredMessage {
							ctx = sit.ctx,
							plug = plug,
							level = RealPlugin.DebugLevelEnum.Verbose,
							Message = I18n.Translate("internal/AuraContainer/closingaura", "Closing aura window")
						}
					);
				} else {
					messages.Add(new DeferredMessage {
							ctx = null,
							plug = plug,
							level = RealPlugin.DebugLevelEnum.Verbose,
							Message = I18n.Translate("internal/AuraContainer/closingaura", "Closing aura window")
						}
					);
				}
				textitems.Remove(si);
				sit.Dispose();
			}
		}
		ProcessMessages(messages);
	}

	internal void HideAllItems() {
		ItemActions.Enqueue(new ItemAction {
			Action = ItemAction.ActionTypeEnum.RenderingOff
		});
	}

	internal void ShowAllItems() {
		ItemActions.Enqueue(new ItemAction {
			Action = ItemAction.ActionTypeEnum.RenderingOn
		});
	}

	private void Render(RenderCollection rc) {
		var toRem = new List<ScarboroughItem>();
		var messages = new List<DeferredMessage>();
		rc.items.Sort((a, b) => a.Ordinal.CompareTo(b.Ordinal));
		foreach (var si in rc.items) {
			try {
				si.Render();
			} catch (Exception ex) {
				if (si.ctx is { Trigger: not null }) {
					messages.Add(new DeferredMessage {
							ctx = si.ctx,
							plug = plug,
							level = RealPlugin.DebugLevelEnum.Error,
							Message = I18n.Translate("internal/AuraContainer/updateerror", $"Deactivating aura '{si.Name}' from trigger '{si.ctx.Trigger.LogName}' due to update exception: {ex.Message}")
						}
					);
				} else {
					messages.Add(new DeferredMessage {
							ctx = null,
							plug = plug,
							level = RealPlugin.DebugLevelEnum.Error,
							Message = I18n.Translate("internal/AuraContainer/updateerror", $"Deactivating aura '{si.Name}' due to update exception: {ex.Message}")
						}
					);
				}
				toRem.Add(si);
			}
		}
		if (toRem.Count > 0) {
			foreach (var si in toRem) {
				if (si.ctx is { Trigger: not null }) {
					messages.Add(new DeferredMessage {
							ctx = si.ctx,
							plug = plug,
							level = RealPlugin.DebugLevelEnum.Verbose,
							Message = I18n.Translate("internal/AuraContainer/closingaura", "Closing aura window")
						}
					);
				} else {
					messages.Add(new DeferredMessage {
							ctx = null,
							plug = plug,
							level = RealPlugin.DebugLevelEnum.Verbose,
							Message = I18n.Translate("internal/AuraContainer/closingaura", "Closing aura window")
						}
					);
				}
				if (si is ScarboroughImage) {
					var myKey = imageitems.FirstOrDefault(x => x.Value == si).Key;
					imageitems.Remove(myKey);
				}
				if (si is ScarboroughText) {
					var myKey = textitems.FirstOrDefault(x => x.Value == si).Key;
					textitems.Remove(myKey);
				}
				si.Dispose();
			}
		}
		ProcessMessages(messages);
	}

	public void Render(IFramework framework) {
		var tickTime = DateTime.Now;
		var msSince = (tickTime - prevTick).TotalMilliseconds;
		var numTicks = (int)Math.Floor(msSince);
		lag = msSince - numTicks;
		prevTick = tickTime;
		if (numTicks > 0) {
			rc.Clear();
			ExecuteActions();
			// UpdateImages(numTicks, ref rc);
			UpdateText(numTicks, ref rc);
		}
		if (rc.items.Count > 0) Render(rc);
	}

	public class ItemAction {
		public enum ActionTypeEnum {
			Activate,
			Deactivate,
			DeactivateAll,
			RenderingOn,
			RenderingOff,
			DeactivateRegex,
			DeactivateTrigger
		}

		public enum ItemTypeEnum {
			Image,
			Text
		}

		public ActionTypeEnum Action { get; set; }
		public ScarboroughItem Item { get; set; }
		public ItemTypeEnum ItemType { get; set; }
		public ManualResetEvent? Completed { get; set; } = null;
		public string Id { get; set; }
	}

	public class DeferredMessage {
		public Context ctx { get; set; }
		public RealPlugin plug { get; set; }
		public RealPlugin.DebugLevelEnum level { get; set; } = RealPlugin.DebugLevelEnum.None;
		public string Message { get; set; } = "";
	}

	private class RenderCollection {
		public readonly List<ScarboroughItem> items = [];

		public RenderCollection() {
			Clear();
		}

		public void Clear() => items.Clear();

		public void Add(ScarboroughItem si) => items.Add(si);
	}
}