using System.Drawing;
using Triggernometry.Core;

namespace Triggernometry.UI.Aura;

internal sealed class AuraText : Aura {
	internal string TextExpression { get; set; }

	internal string Text {
		get;
		set {
			if (value != field) {
				Changed = true;
				field = value;
			}
		}
	}

	internal ActionOld.TextAuraAlignmentEnum TextAlignment {
		get;
		set {
			if (value != field) {
				Changed = true;
				field = value;
			}
		}
	}

	internal bool UseOutline {
		get;
		set {
			if (value != field) {
				Changed = true;
				field = value;
			}
		}
	}

	internal float FontSize {
		get;
		set {
			if (value != field) {
				Changed = true;
				field = value;
			}
		}
	}

	internal Color TextColor {
		get;
		set {
			if (value != field) {
				Changed = true;
				field = value;
			}
		}
	}

	internal Color OutlineColor {
		get;
		set {
			if (value != field) {
				Changed = true;
				field = value;
			}
		}
	}

	internal Color BackgroundColor {
		get;
		set {
			if (value != field) {
				Changed = true;
				field = value;
				/* tododoo
				if (_BackgroundColor != System.Drawing.Color.Transparent)
				{
				    _bgColor.R = _BackgroundColor.R;
				    _bgColor.G = _BackgroundColor.G;
				    _bgColor.B = _BackgroundColor.B;
				}
				else
				{
				    _bgColor = new Color();
				}*/
			}
		}
	}

	internal FontStyle FontStyle {
		get;
		set {
			if (value != field) {
				Changed = true;
				field = value;
			}
		}
	}

	public override void Dispose() {
		base.Dispose();
	}

	internal override bool InternalLogic(int numTicks) {
		if (!base.InternalLogic(numTicks)) {
			return false;
		}
		Text = EvaluateStringExpression(ctx, TextExpression);
		return true;
	}
}