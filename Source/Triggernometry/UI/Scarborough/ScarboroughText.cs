using System.Drawing;
using SharpDX.DirectWrite;
using Triggernometry.Core;
using Font = Scarborough.Drawing.Font;
using FontStyle = System.Drawing.FontStyle;
using SolidBrush = Scarborough.Drawing.SolidBrush;

namespace Scarborough;

internal class ScarboroughText : ScarboroughItem {
    internal string TextExpression { get; set; }
    internal bool NeedFont { get; set; }
    internal string FontName { get; set; }

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
                if (_BackgroundColor != Color.Transparent) {
                    _bgColor.R = _BackgroundColor.R / 255.0f;
                    _bgColor.G = _BackgroundColor.G / 255.0f;
                    _bgColor.B = _BackgroundColor.B / 255.0f;
                }
                else {
                    _bgColor = new Drawing.Color();
                }
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

    private Font TextFont { get; set; }
    private SolidBrush TextBrush { get; set; }

    public ScarboroughText(Triggernometry.UI.Scarborough own) : base(own) {
    }

    public override void Free() {
        if (TextFont != null) {
            TextFont.Dispose();
            TextFont = null;
        }
        if (TextBrush != null) {
            TextBrush.Dispose();
            TextBrush = null;
        }
    }

    public void LoadFontOnDemand() {
        Free();
        LoadFontData(plug);
        CreateBrush();
    }

    public void CreateBrush() {
        TextBrush = new SolidBrush(_graphics.GetRenderTarget());
    }

    public override void Render() {
        if (NeedFont) {
            if (_window == null) {
                if (!AdjustSurface()) {
                    AdjustVisibility();
                    return;
                }
            }
            LoadFontOnDemand();
            NeedFont = false;
            NeedRender = true;
        }
        if (!Owner.RenderingActive) {
            if (!WasHidden) {
                NeedRender = true;
                WasHidden = true;
            }
        }
        else {
            if (WasHidden) {
                NeedRender = true;
                WasHidden = false;
            }
        }
        if (!NeedRender) {
            return;
        }
        if (!AdjustSurface()) {
            AdjustVisibility();
            return;
        }
        AdjustVisibility();
        NeedRender = false;
        _graphics.BeginScene();
        if (!Owner.RenderingActive || InvalidSize) {
            var tempBgColor = new Drawing.Color();
            _graphics.ClearScene(tempBgColor);
            _graphics.EndScene();
            return;
        }
        if (BackgroundColor != Color.Transparent) {
            _bgColor.A = Opacity / 100.0f;
        }
        _graphics.ClearScene(_bgColor);
        var pa = ParagraphAlignment.Center;
        var ta = SharpDX.DirectWrite.TextAlignment.Center;
        switch (TextAlignment) {
            case ActionOld.TextAuraAlignmentEnum.TopLeft:
                pa = ParagraphAlignment.Near;
                ta = SharpDX.DirectWrite.TextAlignment.Leading;
                break;
            case ActionOld.TextAuraAlignmentEnum.TopCenter:
                pa = ParagraphAlignment.Near;
                break;
            case ActionOld.TextAuraAlignmentEnum.TopRight:
                pa = ParagraphAlignment.Near;
                ta = SharpDX.DirectWrite.TextAlignment.Trailing;
                break;
            case ActionOld.TextAuraAlignmentEnum.MiddleLeft:
                ta = SharpDX.DirectWrite.TextAlignment.Leading;
                break;
            case ActionOld.TextAuraAlignmentEnum.MiddleCenter:
                break;
            case ActionOld.TextAuraAlignmentEnum.MiddleRight:
                ta = SharpDX.DirectWrite.TextAlignment.Trailing;
                break;
            case ActionOld.TextAuraAlignmentEnum.BottomLeft:
                pa = ParagraphAlignment.Far;
                ta = SharpDX.DirectWrite.TextAlignment.Leading;
                break;
            case ActionOld.TextAuraAlignmentEnum.BottomCenter:
                pa = ParagraphAlignment.Far;
                break;
            case ActionOld.TextAuraAlignmentEnum.BottomRight:
                pa = ParagraphAlignment.Far;
                ta = SharpDX.DirectWrite.TextAlignment.Trailing;
                break;
        }
        if (UseOutline) {
            TextBrush.Color = new Drawing.Color(OutlineColor.R, OutlineColor.G, OutlineColor.B, Opacity / 100.0f);
            _graphics.DrawTextEx(TextFont, FontStyle, FontSize * 1.3f, TextBrush, pa, ta, 1, 0, Width, Height, Text);
            _graphics.DrawTextEx(TextFont, FontStyle, FontSize * 1.3f, TextBrush, pa, ta, -1, 0, Width, Height, Text);
            _graphics.DrawTextEx(TextFont, FontStyle, FontSize * 1.3f, TextBrush, pa, ta, 0, 1, Width, Height, Text);
            _graphics.DrawTextEx(TextFont, FontStyle, FontSize * 1.3f, TextBrush, pa, ta, 0, -1, Width, Height, Text);
        }
        TextBrush.Color = new Drawing.Color(TextColor.R, TextColor.G, TextColor.B, Opacity / 100.0f);
        _graphics.DrawTextEx(TextFont, FontStyle, FontSize * 1.3f, TextBrush, pa, ta, 0, 0, Width, Height, Text);
        _graphics.EndScene();
    }

    internal void LoadFontData(RealPlugin plug) {
        TextFont = _graphics.CreateFont(
            FontName,
            FontSize,
            (FontStyle & FontStyle.Bold) == FontStyle.Bold,
            (FontStyle & FontStyle.Italic) == FontStyle.Italic,
            true
        );
    }

    public override bool InternalLogic(int numTicks) {
        while (numTicks > 0) {
            if (!GenericLogic()) {
                return false;
            }
            numTicks--;
        }
        Text = EvaluateStringExpression(ctx, TextExpression);
        return true;
    }
}