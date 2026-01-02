using System;
using Triggernometry.Core;
using Triggernometry.Localization;
using Triggernometry.UI.Aura.Renderer;

namespace Triggernometry.UI.Aura;

internal abstract class Aura : IDisposable {
    internal long Ordinal { get; set; }

    internal RendererBase Renderer { get; set; }

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

    private int _Left;
    internal int Left
    {
        get => _Left;
        set
        {
            if (value != _Left) {
                Changed = true;
                _Left = value;
            }
        }
    }

    private int _Top;
    internal int Top
    {
        get => _Top;
        set
        {
            if (value != _Top) {
                Changed = true;
                _Top = value;
            }
        }
    }

    private int _Width;
    internal int Width
    {
        get => _Width;
        set
        {
            if (value != _Width) {
                Changed = true;
                _Width = value;
            }
        }
    }

    private int _Height;
    internal int Height
    {
        get => _Height;
        set
        {
            if (value != _Height) {
                Changed = true;
                _Height = value;
            }
        }
    }

    private int _Opacity;
    internal int Opacity
    {
        get => _Opacity;
        set
        {
            if (value != _Opacity) {
                Changed = true;
                _Opacity = value;
            }
        }
    }

    public virtual void Dispose() {
        if (Renderer != null) {
            Renderer.Dispose();
            Renderer = null;
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

    internal void Render() {
        if (Renderer != null) {
            Renderer.Render(this);
        }
    }

    public bool GenericLogic() {
        if (UpdateXExpression != null && UpdateXExpression.Length > 0) {
            Left = EvaluateNumericExpression(ctx, UpdateXExpression);
        }
        if (UpdateYExpression != null && UpdateYExpression.Length > 0) {
            Top = EvaluateNumericExpression(ctx, UpdateYExpression);
        }
        if (UpdateWExpression != null && UpdateWExpression.Length > 0) {
            var newval = EvaluateNumericExpression(ctx, UpdateWExpression);
            if (newval < 0) {
                newval = 0;
            }
            Width = newval;
        }
        if (UpdateHExpression != null && UpdateHExpression.Length > 0) {
            var newval = EvaluateNumericExpression(ctx, UpdateHExpression);
            if (newval < 0) {
                newval = 0;
            }
            Height = newval;
        }
        if (UpdateOExpression != null && UpdateOExpression.Length > 0) {
            var newval = EvaluateNumericExpression(ctx, UpdateOExpression);
            if (newval < 0) {
                newval = 0;
            }
            if (newval > 100) {
                newval = 100;
            }
            Opacity = newval;
        }
        if (TTLExpression != null && TTLExpression.Length > 0) {
            if (EvaluateNumericExpression(ctx, TTLExpression) < 0) {
                if (ctx.Trigger != null) {
                    ctx.Trigger.AddToLog(RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/AuraContainer/deactaurattl", "Deactivating aura due to TTL expression"));
                }
                else {
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
        catch (Exception ex) {
            if (ctx.Trigger != null) {
                ctx.Trigger.AddToLog(RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/AuraContainer/updateerror", string.Format("Deactivating aura '{0}' from trigger '{1}' due to update exception: {2}", Name, ctx.Trigger.LogName, ex.Message)));
            }
            else {
                plug.FilteredAddToLog(RealPlugin.DebugLevelEnum.Error, I18n.Translate("internal/AuraContainer/updateerror", string.Format("Deactivating aura '{0}' due to update exception: {1}", Name, ex.Message)));
            }
            return false;
        }
    }

    internal virtual bool InternalLogic(int numTicks) {
        while (numTicks > 0) {
            if (!GenericLogic()) {
                return false;
            }
            numTicks--;
        }
        return true;
    }
}