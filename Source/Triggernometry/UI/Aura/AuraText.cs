using System.Drawing;
using Triggernometry.Core;

namespace Triggernometry.UI.Aura;

internal sealed class AuraText : Aura {
    internal string TextExpression { get; set; }

    private string _Text;
    internal string Text
    {
        get => _Text;
        set
        {
            if (value != _Text) {
                Changed = true;
                _Text = value;
            }
        }
    }

    private ActionOld.TextAuraAlignmentEnum _TextAlignment;
    internal ActionOld.TextAuraAlignmentEnum TextAlignment
    {
        get => _TextAlignment;
        set
        {
            if (value != _TextAlignment) {
                Changed = true;
                _TextAlignment = value;
            }
        }
    }

    private bool _UseOutline;
    internal bool UseOutline
    {
        get => _UseOutline;
        set
        {
            if (value != _UseOutline) {
                Changed = true;
                _UseOutline = value;
            }
        }
    }

    private float _FontSize;
    internal float FontSize
    {
        get => _FontSize;
        set
        {
            if (value != _FontSize) {
                Changed = true;
                _FontSize = value;
            }
        }
    }

    private Color _TextColor;
    internal Color TextColor
    {
        get => _TextColor;
        set
        {
            if (value != _TextColor) {
                Changed = true;
                _TextColor = value;
            }
        }
    }

    private Color _OutlineColor;
    internal Color OutlineColor
    {
        get => _OutlineColor;
        set
        {
            if (value != _OutlineColor) {
                Changed = true;
                _OutlineColor = value;
            }
        }
    }

    private Color _BackgroundColor;
    internal Color BackgroundColor
    {
        get => _BackgroundColor;
        set
        {
            if (value != _BackgroundColor) {
                Changed = true;
                _BackgroundColor = value;
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

    private FontStyle _FontStyle;
    internal FontStyle FontStyle
    {
        get => _FontStyle;
        set
        {
            if (value != _FontStyle) {
                Changed = true;
                _FontStyle = value;
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