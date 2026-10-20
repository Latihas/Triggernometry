using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility;
using SharpDX.DirectWrite;
using Triggernometry.Core;
using TriggernometryProxy;
using FontStyle = System.Drawing.FontStyle;
using ScarboroughFont = Scarborough.Drawing.Font;
using ScarboroughSolidBrush = Scarborough.Drawing.SolidBrush;

namespace Scarborough;

public class ScarboroughText(Triggernometry.UI.Scarborough own) : ScarboroughItem(own) {
	private IFontHandle? FontHandle;
	private ScarboroughFont? TextFont;
	private ScarboroughSolidBrush? TextBrush;
	private string FontHandleKey { get; set; }
	private readonly List<string> cachedLines = [];
	private readonly List<float> cachedLineWidths = [];
	private string cachedText = "";
	private string cachedFontHandleKey = "";
	private int cachedWidth;
	private float cachedFontSize;
	private float cachedScale;
	private float cachedDefaultFontSize;
	private float cachedGlobalScale;
	private float cachedLineHeight;
	private bool cachedFontAvailable;
	private bool hasTextLayout;

	internal string TextExpression { get; set; }
	internal bool NeedFont { get; set; }
	internal string FontName { get; set; }
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
	internal string Text {
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
				if (field != Color.Transparent) {
					_bgColor.R = field.R / 255.0f;
					_bgColor.G = field.G / 255.0f;
					_bgColor.B = field.B / 255.0f;
				} else {
					_bgColor = new Drawing.Color();
				}
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

	public override void Free() {
		TextFont?.Dispose();
		TextFont = null;
		TextBrush?.Dispose();
		TextBrush = null;
		FontHandle?.Dispose();
		FontHandle = null;
		FontHandleKey = null;
		cachedLines.Clear();
		cachedLineWidths.Clear();
		hasTextLayout = false;
	}

	public void LoadFontOnDemand() {
		if (Owner.IsImGuiRenderer) return;
		Free();
		if (!AdjustSurface()) return;
		LoadFontData(plug);
		CreateBrush();
		NeedFont = false;
	}

	public void CreateBrush() {
		if (Owner.IsImGuiRenderer || _graphics == null) return;
		TextBrush = new ScarboroughSolidBrush(_graphics.GetRenderTarget());
	}

	public override void Render() {
		if (Owner.IsImGuiRenderer) {
			NeedFont = false;
			NeedRender = false;
			AdjustSurface();
			return;
		}

		if (NeedFont) {
			if (!AdjustSurface()) {
				AdjustVisibility();
				return;
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
		} else if (WasHidden) {
			NeedRender = true;
			WasHidden = false;
		}
		if (!NeedRender) return;
		if (!AdjustSurface()) {
			AdjustVisibility();
			return;
		}
		AdjustVisibility();
		NeedRender = false;
		var graphics = _graphics!;
		graphics.BeginScene();
		if (!Owner.RenderingActive || InvalidSize) {
			graphics.ClearScene(new Drawing.Color());
			graphics.EndScene();
			return;
		}
		if (BackgroundColor != Color.Transparent) {
			_bgColor.A = Opacity / 100.0f;
		}
		graphics.ClearScene(_bgColor);
		var paragraphAlignment = ParagraphAlignment.Center;
		var textAlignment = SharpDX.DirectWrite.TextAlignment.Center;
		switch (TextAlignment) {
			case ActionOld.TextAuraAlignmentEnum.TopLeft:
				paragraphAlignment = ParagraphAlignment.Near;
				textAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
				break;
			case ActionOld.TextAuraAlignmentEnum.TopCenter:
				paragraphAlignment = ParagraphAlignment.Near;
				break;
			case ActionOld.TextAuraAlignmentEnum.TopRight:
				paragraphAlignment = ParagraphAlignment.Near;
				textAlignment = SharpDX.DirectWrite.TextAlignment.Trailing;
				break;
			case ActionOld.TextAuraAlignmentEnum.MiddleLeft:
				textAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
				break;
			case ActionOld.TextAuraAlignmentEnum.MiddleRight:
				textAlignment = SharpDX.DirectWrite.TextAlignment.Trailing;
				break;
			case ActionOld.TextAuraAlignmentEnum.BottomLeft:
				paragraphAlignment = ParagraphAlignment.Far;
				textAlignment = SharpDX.DirectWrite.TextAlignment.Leading;
				break;
			case ActionOld.TextAuraAlignmentEnum.BottomCenter:
				paragraphAlignment = ParagraphAlignment.Far;
				break;
			case ActionOld.TextAuraAlignmentEnum.BottomRight:
				paragraphAlignment = ParagraphAlignment.Far;
				textAlignment = SharpDX.DirectWrite.TextAlignment.Trailing;
				break;
		}
		if (TextBrush != null && TextFont != null) {
			if (UseOutline) {
				TextBrush.Color = new Drawing.Color(
					OutlineColor.R,
					OutlineColor.G,
					OutlineColor.B,
					Opacity / 100.0f);
				graphics.DrawTextEx(TextFont, FontStyle, FontSize * 1.3f, TextBrush, paragraphAlignment, textAlignment, 1, 0, Width, Height, Text);
				graphics.DrawTextEx(TextFont, FontStyle, FontSize * 1.3f, TextBrush, paragraphAlignment, textAlignment, -1, 0, Width, Height, Text);
				graphics.DrawTextEx(TextFont, FontStyle, FontSize * 1.3f, TextBrush, paragraphAlignment, textAlignment, 0, 1, Width, Height, Text);
				graphics.DrawTextEx(TextFont, FontStyle, FontSize * 1.3f, TextBrush, paragraphAlignment, textAlignment, 0, -1, Width, Height, Text);
			}
			TextBrush.Color = new Drawing.Color(
				TextColor.R,
				TextColor.G,
				TextColor.B,
				Opacity / 100.0f);
			graphics.DrawTextEx(TextFont, FontStyle, FontSize * 1.3f, TextBrush, paragraphAlignment, textAlignment, 0, 0, Width, Height, Text);
		}
		graphics.EndScene();
	}

	internal void LoadFontData(RealPlugin plug) {
		if (Owner.IsImGuiRenderer || _graphics == null) return;
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

	/*
	 * The ImGui renderer below keeps text layout cached because Draw is called
	 * every frame, while the legacy renderer above only redraws changed items.
	 */
	internal override void DrawImGui(ImDrawListPtr drawList) {
		if (!Owner.RenderingActive || !AdjustSurface() || string.IsNullOrEmpty(Text)) return;
		var fontHandle = EnsureFontHandle();
		using var fontScope = fontHandle?.Push();

		var origin = Position(Left, Top);
		var size = new Vector2(Width, Height);
		var opacity = Math.Clamp(Opacity, 0, 100) / 100.0f;
		if (BackgroundColor != Color.Transparent)
			drawList.AddRectFilled(origin, origin + size, ToImGuiColor(BackgroundColor, opacity));

		var globalScale = MathF.Max(ImGuiHelpers.GlobalScale, 0.01f);
		var fontSize = FontSize > 0 ? FontSize * 1.3f * globalScale : ImGui.GetFontSize();
		var defaultFontSize = MathF.Max(ImGui.GetFontSize(), 1.0f);
		var scale = fontSize / defaultFontSize;
		var lineHeight = ImGui.GetTextLineHeight() * scale;
		var fontHandleKey = FontHandleKey ?? "";
		var fontAvailable = fontHandle?.Available == true;
		if (!hasTextLayout ||
		    !string.Equals(cachedText, Text, StringComparison.Ordinal) ||
		    cachedWidth != Width ||
		    cachedFontSize != fontSize ||
		    cachedScale != scale ||
		    cachedDefaultFontSize != defaultFontSize ||
		    cachedGlobalScale != globalScale ||
		    cachedLineHeight != lineHeight ||
		    !string.Equals(cachedFontHandleKey, fontHandleKey, StringComparison.Ordinal) ||
		    cachedFontAvailable != fontAvailable) {
			cachedText = Text;
			cachedWidth = Width;
			cachedFontSize = fontSize;
			cachedScale = scale;
			cachedDefaultFontSize = defaultFontSize;
			cachedGlobalScale = globalScale;
			cachedLineHeight = lineHeight;
			cachedFontHandleKey = fontHandleKey;
			cachedFontAvailable = fontAvailable;
			RebuildTextLayout(Text.Replace("\r\n", "\n"), scale, size.X);
			hasTextLayout = true;
		}

		var totalHeight = cachedLineHeight * cachedLines.Count;
		var y = TextAlignment switch {
			ActionOld.TextAuraAlignmentEnum.TopLeft or
				ActionOld.TextAuraAlignmentEnum.TopCenter or
				ActionOld.TextAuraAlignmentEnum.TopRight => origin.Y,
			ActionOld.TextAuraAlignmentEnum.BottomLeft or
				ActionOld.TextAuraAlignmentEnum.BottomCenter or
				ActionOld.TextAuraAlignmentEnum.BottomRight => origin.Y + size.Y - totalHeight,
			_ => origin.Y + (size.Y - totalHeight) / 2.0f
		};

		drawList.PushClipRect(origin, origin + size, true);
		for (var i = 0; i < cachedLines.Count; i++) {
			var line = cachedLines[i];
			var lineWidth = cachedLineWidths[i];
			var x = TextAlignment switch {
				ActionOld.TextAuraAlignmentEnum.TopLeft or
					ActionOld.TextAuraAlignmentEnum.MiddleLeft or
					ActionOld.TextAuraAlignmentEnum.BottomLeft => origin.X,
				ActionOld.TextAuraAlignmentEnum.TopRight or
					ActionOld.TextAuraAlignmentEnum.MiddleRight or
					ActionOld.TextAuraAlignmentEnum.BottomRight => origin.X + size.X - lineWidth,
				_ => origin.X + (size.X - lineWidth) / 2.0f
			};
			var linePosition = new Vector2(x, y + i * lineHeight);

			if (UseOutline) {
				var outlineColor = ToImGuiColor(OutlineColor, opacity);
				var outlineOffset = globalScale;
				AddText(drawList, fontSize, linePosition + new Vector2(outlineOffset, 0), outlineColor, line);
				AddText(drawList, fontSize, linePosition + new Vector2(-outlineOffset, 0), outlineColor, line);
				AddText(drawList, fontSize, linePosition + new Vector2(0, outlineOffset), outlineColor, line);
				AddText(drawList, fontSize, linePosition + new Vector2(0, -outlineOffset), outlineColor, line);
			}

			var textColor = ToImGuiColor(TextColor, opacity);
			AddText(drawList, fontSize, linePosition, textColor, line);
			if ((FontStyle & FontStyle.Bold) == FontStyle.Bold && (fontHandle == null || !fontHandle.Available))
				AddText(drawList, fontSize, linePosition + new Vector2(0.6f * globalScale, 0), textColor, line);

			if ((FontStyle & FontStyle.Underline) == FontStyle.Underline) {
				var underlineY = linePosition.Y + lineHeight - 1;
				drawList.AddLine(new Vector2(linePosition.X, underlineY), new Vector2(linePosition.X + lineWidth, underlineY), textColor);
			}
			if ((FontStyle & FontStyle.Strikeout) == FontStyle.Strikeout) {
				var strikeY = linePosition.Y + lineHeight * 0.55f;
				drawList.AddLine(new Vector2(linePosition.X, strikeY), new Vector2(linePosition.X + lineWidth, strikeY), textColor);
			}
		}
		drawList.PopClipRect();
	}

	private static void AddText(ImDrawListPtr drawList, float fontSize, Vector2 position, uint color, string text) {
		drawList.AddText(ImGui.GetFont(), fontSize, position, color, text);
	}

	private IFontHandle? EnsureFontHandle() {
		if (string.IsNullOrWhiteSpace(FontName)) {
			if (FontHandle != null) {
				FontHandle.Dispose();
				FontHandle = null;
			}
			FontHandleKey = null;
			return null;
		}

		var key = $"{FontName}\0{FontSize}\0{FontStyle & (FontStyle.Bold | FontStyle.Italic)}";
		if (key == FontHandleKey) return FontHandle;

		FontHandle?.Dispose();
		FontHandle = null;
		FontHandleKey = key;

		try {
			IFontFamilyId? family = null;
			foreach (var candidate in IFontFamilyId.ListSystemFonts(false)) {
				if (string.Equals(candidate.EnglishName, FontName, StringComparison.OrdinalIgnoreCase)) {
					family = candidate;
					break;
				}
			}
			if (family == null || family.Fonts.Count == 0) return null;

			var fontIndex = family.FindBestMatch(
				(FontStyle & FontStyle.Bold) != 0 ? 700 : 400,
				5,
				(FontStyle & FontStyle.Italic) != 0 ? 1 : 0);
			if (fontIndex < 0 || fontIndex >= family.Fonts.Count) fontIndex = 0;

			var spec = new SingleFontSpec {
				FontId = family.Fonts[fontIndex],
				SizePx = MathF.Max(FontSize * 1.3f, 1.0f)
			};
			FontHandle = spec.CreateFontHandle(ProxyPlugin.PluginInterface.UiBuilder.FontAtlas);
			return FontHandle;
		} catch {
			return null;
		}
	}

	private void RebuildTextLayout(string text, float scale, float maxWidth) {
		cachedLines.Clear();
		cachedLineWidths.Clear();
		foreach (var sourceLine in text.Split('\n')) {
			if (sourceLine.Length == 0) {
				AddCachedLine(string.Empty, scale);
				continue;
			}
			if (ImGui.CalcTextSize(sourceLine).X * scale <= maxWidth) {
				AddCachedLine(sourceLine, scale);
				continue;
			}

			var current = string.Empty;
			var elements = StringInfo.GetTextElementEnumerator(sourceLine);
			while (elements.MoveNext()) {
				var element = elements.GetTextElement();
				var candidate = current + element;
				if (current.Length > 0 && ImGui.CalcTextSize(candidate).X * scale > maxWidth) {
					AddCachedLine(current, scale);
					current = element;
				} else {
					current = candidate;
				}
			}
			AddCachedLine(current, scale);
		}
	}

	private void AddCachedLine(string line, float scale) {
		cachedLines.Add(line);
		cachedLineWidths.Add(ImGui.CalcTextSize(line).X * scale);
	}

}
