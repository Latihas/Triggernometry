using System;
using System.Collections.Generic;
using System.Drawing;
using Dalamud.Plugin.Services;
using Scarborough;
using Triggernometry.Forms;
using Triggernometry.Localization;
using TriggernometryProxy;
using static Triggernometry.UI.Scarborough;
using Color = System.Drawing.Color;
using ExpressionTextBox = Triggernometry.UI.CustomControls.ExpressionTextBox;

// ReSharper disable once CheckNamespace
namespace Triggernometry.Core;

public partial class RealPlugin {
    internal UI.Scarborough sc;
    public Dictionary<string, AuraContainerForm> textauras = new();

    public void InitAura() {
        ProxyPlugin.Framework.Update += AuraUpdateThreadProc;
        sc = new UI.Scarborough();
        sc.plug = this;
    }

    public void DeInitAura() {
        ProxyPlugin.Framework.Update -= AuraUpdateThreadProc;
        sc?.Dispose();
        textauras.Clear();
    }

    private void ProcessAuraControl(bool hideAuras) {
        if (hideAuras) sc.HideAllItems();
        else sc.ShowAllItems();
    }

    private DateTime prevTick = DateTime.Now;
    private double lag;

    private void AuraUpdateThreadProc(IFramework framework) {
        var tickTime = DateTime.Now;
        var msSince = (tickTime - prevTick).TotalMilliseconds + lag;
        var numTicks = (int)Math.Floor(msSince / 20.0);
        lag = msSince - numTicks * 20;
        prevTick = tickTime;
        UpdateAuras(numTicks);
    }

    internal void UpdateAuras(int numTicks) {
        var toRem = new List<string>();
        foreach (var kp in textauras)
            if (!kp.Value.UpdateAura(numTicks))
                toRem.Add(kp.Key);
        foreach (var rem in toRem) textauras.Remove(rem);
    }

    internal void TextAuraManagement(Context ctx, ActionOld a) {
        switch (a._TextAuraOp) {
            case ActionOld.AuraOpEnum.ActivateAura: {
                var ax = ctx.EvaluateStringExpression(a.ActionContextLogger, ctx, a._TextAuraName);
                FilteredAddToLog(DebugLevelEnum.Info, I18n.Translate("internal/Plugin/acttextaura", "Activating text aura '{0}'", ax));
                try {
                    var si = new ScarboroughText(sc);
                    si.InitXExpression = a._TextAuraXIniExpression;
                    si.InitYExpression = a._TextAuraYIniExpression;
                    si.InitWExpression = a._TextAuraWIniExpression;
                    si.InitHExpression = a._TextAuraHIniExpression;
                    si.InitOExpression = a._TextAuraOIniExpression;
                    si.UpdateXExpression = a._TextAuraXTickExpression;
                    si.UpdateYExpression = a._TextAuraYTickExpression;
                    si.UpdateWExpression = a._TextAuraWTickExpression;
                    si.UpdateHExpression = a._TextAuraHTickExpression;
                    si.UpdateOExpression = a._TextAuraOTickExpression;
                    si.TTLExpression = a._TextAuraTTLTickExpression;
                    var fs = FontStyle.Regular;
                    if ((a._TextAuraEffect & ActionOld.TextAuraEffectEnum.Bold) != 0) fs |= FontStyle.Bold;
                    if ((a._TextAuraEffect & ActionOld.TextAuraEffectEnum.Italic) != 0) fs |= FontStyle.Italic;
                    if ((a._TextAuraEffect & ActionOld.TextAuraEffectEnum.Underline) != 0) fs |= FontStyle.Underline;
                    if ((a._TextAuraEffect & ActionOld.TextAuraEffectEnum.Strikeout) != 0) fs |= FontStyle.Strikeout;
                    si.TextExpression = a._TextAuraExpression;
                    si.TextAlignment = a._TextAuraAlignment;
                    si.TextColor = ExpressionTextBox.ParseColor(
                        ctx.EvaluateStringExpression(a.ActionContextLogger, ctx, a._TextAuraForegroundClInt),
                        Color.Black);
                    si.OutlineColor = ExpressionTextBox.ParseColor(
                        ctx.EvaluateStringExpression(a.ActionContextLogger, ctx, a._TextAuraOutlineClInt),
                        Color.Empty);
                    si.UseOutline = si.OutlineColor != Color.Empty;
                    si.FontName = a._TextAuraFontName;
                    si.FontSize = a._TextAuraFontSize;
                    si.FontStyle = fs;
                    si.ctx = ctx;
                    si.BackgroundColor = ExpressionTextBox.ParseColor(
                        ctx.EvaluateStringExpression(a.ActionContextLogger, ctx, a._TextAuraBackgroundClInt),
                        Color.Transparent);
                    sc.Activate(ax, si);
                }
                catch (Exception ex) {
                    FilteredAddToLog(DebugLevelEnum.Error, I18n.Translate("internal/Plugin/exacttextaura", "Exception '{0}' when activating text aura '{1}'", ex.Message, ax));
                }
            }
                break;
            case ActionOld.AuraOpEnum.DeactivateAura: {
                var ax = ctx.EvaluateStringExpression(a.ActionContextLogger, ctx, a._TextAuraName);
                FilteredAddToLog(DebugLevelEnum.Info, I18n.Translate("internal/Plugin/deacttextaura", "Deactivating text aura '{0}'", ax));
                sc.Deactivate(ax, ItemAction.ItemTypeEnum.Text);
            }
                break;
            case ActionOld.AuraOpEnum.DeactivateAllAura: {
                FilteredAddToLog(DebugLevelEnum.Info, I18n.Translate("internal/Plugin/deactalltextaura", "Deactivating all text auras"));
                sc.DeactivateAllText();
            }
                break;
            case ActionOld.AuraOpEnum.DeactivateAuraRegex: {
                var ax = a._TextAuraName;
                FilteredAddToLog(DebugLevelEnum.Info, I18n.Translate("internal/Plugin/deacttextaurarex", "Deactivating text auras matching '{0}'", ax));
                sc.DeactivateRegex(ax, ItemAction.ItemTypeEnum.Text);
            }
                break;
            case ActionOld.AuraOpEnum.DeactivateAuraTrigger: {
                FilteredAddToLog(DebugLevelEnum.Info, I18n.Translate("internal/Plugin/deacttextauratrig", "Deactivating text auras from trigger '{0}' ({1})", ctx.Trigger.LogName, ctx.Trigger.Id));
                sc.DeactivateTrigger(ctx.Trigger, ItemAction.ItemTypeEnum.Text);
            }
                break;
        }
    }
}