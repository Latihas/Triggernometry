﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Numerics;
using System.Windows.Forms;
using Dalamud.Bindings.ImGui;
using Triggernometry.Core;
using Triggernometry.Localization;

namespace Triggernometry.Forms;

public class AuraContainerForm
{
    public enum AuraTypeEnum
    {
        Image,
        Text
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public AuraTypeEnum AuraType { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal string XExpression { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal string YExpression { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal string WExpression { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal string HExpression { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal string OExpression { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal string TTLExpression { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal PictureBoxSizeMode Display { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal Context ctx { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string TextExpression { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal ActionOld.TextAuraAlignmentEnum TextAlignment { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal Color TextColor { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal Color OutlineColor { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BackgroundColor { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal bool UseOutline { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal string CurrentText { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal string NewText { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string AuraName { get; set; }
    private Vector4 TextColorVec => new(TextColor.R / 255f, TextColor.G / 255f, TextColor.B / 255f, TextColor.A / 255f);
    private Vector4 OutlineColorVec => new(OutlineColor.R / 255f, OutlineColor.G / 255f, OutlineColor.B / 255f, OutlineColor.A / 255f);
    public Vector4 BgColorVec => new(BackgroundColor.R / 255f, BackgroundColor.G / 255f, BackgroundColor.B / 255f, BackgroundColor.A / 255f);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal double PresentableOpacity { get; set; }
    internal RealPlugin plug;

    public Vector2 _position = Vector2.Zero;
    public Vector2 _size = Vector2.Zero;

    public AuraContainerForm(AuraTypeEnum at)
    {
        AuraType = at;
        PresentableOpacity = 50;
        _position = Vector2.Zero;
        _size = new Vector2(100, 100);
    }

    internal void AuraDeactivate()
    {
        if (AuraType == AuraTypeEnum.Text)
            ctx.Plugin.textauras.Remove(AuraName);
    }

    private bool NextPending;
    int NextLeft, NextTop, NextWidth, NextHeight;
    double NextPresentableOpacity;

    private string PreprocessExpression(string exp)
    {
        if (NextPending)
        {
            exp = exp.Replace("${_x}", NextLeft.ToString());
            exp = exp.Replace("${_y}", NextTop.ToString());
            exp = exp.Replace("${_opacity}", NextPresentableOpacity.ToString());
        }
        else
        {
            exp = exp.Replace("${_x}", _position.X.ToString());
            exp = exp.Replace("${_y}", _position.Y.ToString());
            exp = exp.Replace("${_opacity}", PresentableOpacity.ToString());
        }
        return exp;
    }

    internal int EvaluateNumericExpression(Context c, string exp)
    {
        return (int)c.EvaluateNumericExpression((c.Trigger != null) ? c.Trigger.TriggerContextLogger : null, c.Plugin, PreprocessExpression(exp));
    }

    internal bool UpdateAura(int numTicks)
    {
        NextPending = false;
        bool chLeft = false, chTop = false, chWidth = false, chHeight = false, chOpacity = false;
        try
        {
            while (numTicks > 0)
            {
                int i;
                if (!string.IsNullOrEmpty(XExpression))
                {
                    NextLeft = EvaluateNumericExpression(ctx, XExpression);
                    chLeft = (int)_position.X != NextLeft;
                }
                if (!string.IsNullOrEmpty(YExpression))
                {
                    NextTop = EvaluateNumericExpression(ctx, YExpression);
                    chTop = (int)_position.Y != NextTop;
                }
                if (!string.IsNullOrEmpty(WExpression))
                {
                    i = EvaluateNumericExpression(ctx, WExpression);
                    i = Math.Max(0, i);
                    NextWidth = i;
                    chWidth = (int)_size.X != NextWidth;
                }
                if (!string.IsNullOrEmpty(HExpression))
                {
                    i = EvaluateNumericExpression(ctx, HExpression);
                    i = Math.Max(0, i);
                    NextHeight = i;
                    chHeight = (int)_size.Y != NextHeight;
                }
                if (AuraType == AuraTypeEnum.Text && !string.IsNullOrEmpty(TextExpression))
                {
                    NewText = ctx.EvaluateStringExpression(ctx.Trigger != null ? ctx.Trigger.TriggerContextLogger : null, ctx.Plugin, TextExpression);
                    if (NewText != CurrentText) CurrentText = NewText;
                }
                if (!string.IsNullOrEmpty(OExpression))
                {
                    i = EvaluateNumericExpression(ctx, OExpression);
                    i = Math.Clamp(i, 0, 100);
                    NextPresentableOpacity = i;
                    chOpacity = Math.Abs(PresentableOpacity - NextPresentableOpacity) > 0.1;
                }
                if (!string.IsNullOrEmpty(TTLExpression))
                {
                    if (EvaluateNumericExpression(ctx, TTLExpression) < 0)
                    {
                        if (ctx.Trigger != null) ctx.Trigger.AddToLog(RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/AuraContainer/deactaurattl", "Deactivating aura due to TTL expression"));
                        else
                            plug.FilteredAddToLog(RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/AuraContainer/deactaurattl", "Deactivating aura due to TTL expression"));
                        AuraDeactivate();
                        return false;
                    }
                }
                numTicks--;
                NextPending = true;
            }
        }
        catch (Exception ex)
        {
            if (ctx.Trigger != null)
                ctx.Trigger.AddToLog( RealPlugin.DebugLevelEnum.Error,
                                     I18n.Translate("internal/AuraContainer/updateerror", String.Format("Deactivating aura '{0}' from trigger '{1}' due to update exception: {2}", AuraName, ctx.Trigger.LogName, ex.Message)));
            else
                plug.FilteredAddToLog(RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/AuraContainer/updateerror", String.Format("Deactivating aura '{0}' due to update exception: {1}", AuraName, ex.Message)));
            AuraDeactivate();
            return false;
        }
        if (chLeft) _position.X = NextLeft;
        if (chTop) _position.Y = NextTop;
        if (chWidth) _size.X = NextWidth;
        if (chHeight) _size.Y = NextHeight;
        if (chOpacity) PresentableOpacity = NextPresentableOpacity;
        return true;
    }
}
