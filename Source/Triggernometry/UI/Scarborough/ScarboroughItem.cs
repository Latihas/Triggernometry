using System;
using Scarborough.Drawing;
using Scarborough.Windows;
using Triggernometry.Core;
using Triggernometry.Localization;

namespace Scarborough;

internal abstract class ScarboroughItem : IDisposable {
	protected Triggernometry.UI.Scarborough Owner { get; set; }

	protected OverlayWindow? _window;
	public Graphics? _graphics;
	protected Color _bgColor = new();

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

	private int _Left;
	internal int Left {
		get => _Left;
		set {
			if (value != _Left) {
				Changed = true;
				_Left = value;
			}
		}
	}

	private int _Top;
	internal int Top {
		get => _Top;
		set {
			if (value != _Top) {
				Changed = true;
				_Top = value;
			}
		}
	}

	private int _Width;
	internal int Width {
		get => _Width;
		set {
			if (value != _Width) {
				Changed = true;
				_Width = value;
			}
		}
	}

	private int _Height;
	internal int Height {
		get => _Height;
		set {
			if (value != _Height) {
				Changed = true;
				_Height = value;
			}
		}
	}

	private int _Opacity;
	internal int Opacity {
		get => _Opacity;
		set {
			if (value != _Opacity) {
				Changed = true;
				_Opacity = value;
			}
		}
	}

	public abstract void Free();
	public abstract void Render();
	public abstract bool InternalLogic(int numTicks);

	public ScarboroughItem(Triggernometry.UI.Scarborough own) {
		Owner = own;
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
		} catch (Exception ex) {
			if (ctx.Trigger != null) {
				ctx.Trigger.AddToLog(RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/AuraContainer/updateerror", $"Deactivating aura '{Name}' from trigger '{ctx.Trigger.LogName}' due to update exception: {ex.Message}"));
			} else {
				plug.FilteredAddToLog(RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/AuraContainer/updateerror", $"Deactivating aura '{Name}' due to update exception: {ex.Message}"));
			}
			return false;
		}
	}

	protected void AdjustVisibility() {
		if (_window == null) {
			return;
		}
		if (_window.IsVisible) {
			if (InvalidSize || !Owner.RenderingActive) {
				_window.Hide();
			}
		} else {
			if (!InvalidSize && Owner.RenderingActive) {
				_window.Show();
			}
		}
	}

	protected bool AdjustSurface() {
		InvalidSize = Width <= 0 || Height <= 0;
		if (InvalidSize) {
			return false;
		}
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
				_graphics.Resize(Width, Height);
			}
		}
		return true;
	}
}