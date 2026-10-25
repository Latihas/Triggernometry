using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Scarborough.Drawing;
using Scarborough.Windows;
using Triggernometry.Core;
using Triggernometry.Localization;

namespace Scarborough;

public abstract class ScarboroughItem(Triggernometry.UI.Scarborough own) : IDisposable {
	protected Color _bgColor = new();
	public Graphics? _graphics;

	protected OverlayWindow? _window;

	protected Triggernometry.UI.Scarborough Owner { get; set; } = own;

	internal bool NeedRender { get; set; } = true;
	internal bool WasHidden { get; set; } = false;

	internal long Ordinal { get; set; }

	internal string Name { get; set; }
	internal string InitXExpression { get; set; }
	internal string InitYExpression { get; set; }
	internal string InitWExpression { get; set; }
	internal string InitHExpression { get; set; }
	internal string InitOExpression { get; set; }
	internal string UpdateXExpression { get; set; }
	internal string UpdateYExpression { get; set; }
	internal string UpdateWExpression { get; set; }
	internal string UpdateHExpression { get; set; }
	internal string UpdateOExpression { get; set; }
	internal string TTLExpression { get; set; }
	internal Context ctx { get; set; }
	internal RealPlugin plug { get; set; }

	internal bool Changed { get; set; }
	internal bool InvalidSize { get; set; }
	internal int Left {
		get;
		set {
			if (value != field) {
				Changed = true;
				field = value;
			}
		}
	}
	internal int Top {
		get;
		set {
			if (value != field) {
				Changed = true;
				field = value;
			}
		}
	}
	internal int Width {
		get;
		set {
			if (value != field) {
				Changed = true;
				field = value;
			}
		}
	}
	internal int Height {
		get;
		set {
			if (value != field) {
				Changed = true;
				field = value;
			}
		}
	}
	internal int Opacity {
		get;
		set {
			if (value != field) {
				Changed = true;
				field = value;
			}
		}
	}

	public void Dispose() {
		Free();
		if (_graphics != null) {
			_graphics.Dispose();
			_graphics = null;
		}
		if (_window != null) {
			_window.Dispose();
			_window = null;
		}
	}

	public abstract void Free();
	public abstract void Render();
	public abstract bool InternalLogic(int numTicks);

	internal virtual void DrawImGui(ImDrawListPtr drawList) {
	}

	private string PreprocessExpression(string exp) {
		exp = exp.Replace("${_x}", Left.ToString());
		exp = exp.Replace("${_y}", Top.ToString());
		exp = exp.Replace("${_width}", Width.ToString());
		exp = exp.Replace("${_height}", Height.ToString());
		exp = exp.Replace("${_opacity}", Opacity.ToString());
		return exp;
	}

	internal int EvaluateNumericExpression(Context c, string exp) => (int)c.EvaluateNumericExpression(c.Trigger != null ? c.Trigger.TriggerContextLogger : null, c.Plugin, PreprocessExpression(exp));

	internal string EvaluateStringExpression(Context c, string exp) => c.EvaluateStringExpression(c.Trigger != null ? c.Trigger.TriggerContextLogger : null, c.Plugin, PreprocessExpression(exp));

	public bool GenericLogic() {
		if (!string.IsNullOrWhiteSpace(UpdateXExpression)) {
			Left = EvaluateNumericExpression(ctx, UpdateXExpression);
		}
		if (!string.IsNullOrWhiteSpace(UpdateYExpression)) {
			Top = EvaluateNumericExpression(ctx, UpdateYExpression);
		}
		if (!string.IsNullOrWhiteSpace(UpdateWExpression)) {
			var newval = EvaluateNumericExpression(ctx, UpdateWExpression);
			if (newval < 0) {
				newval = 0;
			}
			Width = newval;
		}
		if (!string.IsNullOrWhiteSpace(UpdateHExpression)) {
			var newval = EvaluateNumericExpression(ctx, UpdateHExpression);
			if (newval < 0) {
				newval = 0;
			}
			Height = newval;
		}
		if (!string.IsNullOrWhiteSpace(UpdateOExpression)) {
			var newval = EvaluateNumericExpression(ctx, UpdateOExpression);
			if (newval < 0) {
				newval = 0;
			}
			if (newval > 100) {
				newval = 100;
			}
			Opacity = newval;
		}
		if (!string.IsNullOrWhiteSpace(TTLExpression)) {
			if (EvaluateNumericExpression(ctx, TTLExpression) < 0) {
				if (ctx.Trigger != null) {
					ctx.Trigger.AddToLog(RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/AuraContainer/deactaurattl", "Deactivating aura due to TTL expression"));
				} else {
					plug.FilteredAddToLog(RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/AuraContainer/deactaurattl", "Deactivating aura due to TTL expression"));
				}
				return false;
			}
		}
		return true;
	}

	public bool Logic(int numTicks) {
		try {
			return InternalLogic(numTicks);
            }
            catch (Exception ex)
            {
                if (ctx.Trigger != null)
                {
	                ctx.Trigger.AddToLog(RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/AuraContainer/updateerrortrigger", "Deactivating aura '{0}' from trigger '{1}' due to update exception: {2}", Name, ctx.Trigger.LogName, ex.Message));
                }
                else
                {
	                plug.FilteredAddToLog(RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/AuraContainer/updateerror", "Deactivating aura '{0}' due to update exception: {1}", Name, ex.Message));
			}
			return false;
		}
	}

	protected void AdjustVisibility() {
		if (Owner.IsImGuiRenderer || _window == null) return;
		if (_window.IsVisible) {
			if (InvalidSize || !Owner.RenderingActive) {
				_window.Hide();
			}
		} else if (!InvalidSize && Owner.RenderingActive) {
			_window.Show();
		}
	}

	protected bool AdjustSurface() {
		InvalidSize = Width <= 0 || Height <= 0;
		if (InvalidSize || Owner.IsImGuiRenderer) return !InvalidSize;

		if (_window == null) {
			_window = new OverlayWindow(Left, Top, Width, Height) {
				IsTopmost = true,
				IsVisible = true
			};
			_graphics = new Graphics {
				MeasureFPS = false,
				Width = _window.Width,
				Height = _window.Height,
				PerPrimitiveAntiAliasing = true,
				TextAntiAliasing = true,
				UseMultiThreadedFactories = false,
				VSync = false,
				WindowHandle = IntPtr.Zero
			};
			_window.CreateWindow();
			_graphics.WindowHandle = _window.Handle;
			_graphics.Setup();
		} else {
			if (Left != _window.X || Top != _window.Y) {
				_window.Move(Left, Top);
			}
			if (Width != _window.Width || Height != _window.Height) {
				_window.Resize(Width, Height);
				_graphics!.Resize(Width, Height);
			}
		}
		return true;
	}

	protected static uint ToImGuiColor(System.Drawing.Color color, float opacity = 1.0f) {
		var alpha = (byte)Math.Clamp((int)MathF.Round(color.A * opacity), 0, 255);
		return (uint)(color.R | color.G << 8 | color.B << 16 | alpha << 24);
	}

	protected static Vector2 Position(int x, int y) => new(x, y);
}