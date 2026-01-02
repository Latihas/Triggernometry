using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml.Serialization;
using Triggernometry.Core.Serialization;
using Triggernometry.Core.Variables;
using Triggernometry.Localization;

namespace Triggernometry.Core.Actions;

/// <summary>
///     JSON remote request
/// </summary>
[ActionCategory(ActionCategory.CategoryTypeEnum.Networking)]
[XmlRoot(ElementName = "JsonRequest")]
internal class ActionJsonRequest : ActionBase {
    #region Properties

    /*
    /// <summary>
    /// Request method
    /// </summary>
    public enum MethodEnum
    {
        POST,
        GET
    }
    */

    /// <summary>
    ///     Remote endpoint expression
    /// </summary>
    [XmlIgnore] [Action(1)] public string Endpoint { get; set; } = "";

    [XmlAttribute("Endpoint")] public string Xml_Endpoint
    {
        get => XmlAttr.String(Endpoint);
        set => Endpoint = value;
    }
    /* todo
    /// <summary>
    /// Request method to use
    /// </summary>
    [XmlIgnore]
    [Action(order: 2)]
    public MethodEnum Method { get; set; } = MethodEnum.POST;

    [XmlAttribute("Method")]
    public string Xml_Method
    {
        get => XmlAttr.Enum(Method, MethodEnum.POST);
        set => Method = XmlAttr.Enum<MethodEnum>(value);
    }
    */

    /// <summary>
    ///     Request method to use
    /// </summary>
    [XmlIgnore] [Action(2)] public ActionOld.HTTPMethodEnum Method { get; set; } = ActionOld.HTTPMethodEnum.POST;

    [XmlAttribute("Method")] public string Xml_Method
    {
        get => XmlAttr.Enum(Method, ActionOld.HTTPMethodEnum.POST);
        set => Method = XmlAttr.Enum<ActionOld.HTTPMethodEnum>(value);
    }

    /// <summary>
    ///     Payload expression
    /// </summary>
    [XmlIgnore] [Action(3)] public string Payload { get; set; } = "";

    [XmlAttribute("Payload")] public string Xml_Payload
    {
        get => XmlAttr.String(Payload);
        set => Payload = value;
    }

    /// <summary>
    ///     Header expression
    /// </summary>
    [XmlIgnore] [Action(4)] public string Headers { get; set; } = "";

    [XmlAttribute("Headers")] public string Xml_Headers
    {
        get => XmlAttr.String(Headers);
        set => Headers = value;
    }

    /// <summary>
    ///     Scalar variable in which the result of the request will be stored
    /// </summary>
    [XmlIgnore] [Action(5)] public string ResultVariable { get; set; } = "";

    [XmlAttribute("ResultVariable")] public string Xml_ResultVariable
    {
        get => XmlAttr.String(ResultVariable);
        set => ResultVariable = value;
    }

    /// <summary>
    ///     Expression to be used when the result of the request is intended to be fired as a log event
    /// </summary>
    [XmlIgnore] [Action(6)] public string FiringExpression { get; set; } = "";

    [XmlAttribute("FiringExpression")] public string Xml_FiringExpression
    {
        get => XmlAttr.String(FiringExpression);
        set => FiringExpression = value;
    }

    /// <summary>
    ///     If set, Triggernometry will check its cache for a similar request and return that
    /// </summary>
    [XmlIgnore] [Action(7)] public bool UseCache { get; set; }

    [XmlAttribute("UseCache")] public string Xml_UseCache
    {
        get => XmlAttr.Bool(UseCache, false);
        set => UseCache = XmlAttr.Bool(value);
    }

    /// <summary>
    ///     Indicates whether referenced variable is persistent or not
    /// </summary>
    [XmlIgnore] [Action(8)] // todo need to couple this with variable on editor
    public bool Persistent { get; set; }

    [XmlAttribute("Persistent")] public string Xml_Persistent
    {
        get => XmlAttr.Bool(Persistent, false);
        set => Persistent = XmlAttr.Bool(value);
    }

    #endregion


    #region Implementation

    internal override string DescribeImplementation() {
        var cache = I18n.TrlCacheFile(UseCache);
        if (FiringExpression != null && FiringExpression.Trim().Length > 0) {
            return I18n.Translate(
                "internal/Action/descjsonsendrelay",
                "send JSON payload to endpoint ({0}){1}, and relaying response for further processing",
                Endpoint, cache
            );
        }
        return I18n.Translate(
            "internal/Action/descjsonsend",
            "send JSON payload to endpoint ({0}){1} and cache the response",
            Endpoint, cache
        );
    }

    internal override void ExecuteImplementation(ActionInstance ai) {
        var ctx = ai?.ctx ?? Context.Unbound;
        var plug = ctx.Plugin;

        var response = "";
        var responseCode = 0;
        var endpoint = ctx.EvaluateStringExpression(ActionContextLogger, ctx, Endpoint);
        var payload = ctx.EvaluateStringExpression(ActionContextLogger, ctx, Payload);
        var headers = ctx.EvaluateStringExpression(ActionContextLogger, ctx, Headers).Trim();
        var varname = ctx.EvaluateStringExpression(ActionContextLogger, ctx, ResultVariable);
        var persist = I18n.TrlVarPersist(Persistent);
        var headerslist = new List<string>();
        if (headers.Length > 0) {
            headerslist.AddRange(headers.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
        }
        if (UseCache) {
            var endpointh = RealPlugin.GenerateHash(endpoint);
            var payloadh = RealPlugin.GenerateHash(payload);
            var headersh = RealPlugin.GenerateHash(headers);
            var fh = RealPlugin.GenerateHash(endpointh + payloadh + headersh);
            var fn = Path.Combine(plug.ConfigPath, "TriggernometryJsonCache");
            if (!Directory.Exists(fn)) {
                Directory.CreateDirectory(fn);
            }
            fn = Path.Combine(fn, fh + ".json");
            var fromcache = false;
            if (File.Exists(fn)) {
                var fi = new FileInfo(fn);
                var dt = DateTime.Now.AddMinutes(0 - plug.cfg.CacheJsonExpiry);
                if (fi.LastWriteTime > dt) {
                    responseCode = (int)HttpStatusCode.OK;
                    response = File.ReadAllText(fn);
                    fromcache = true;
                }
            }
            if (!fromcache) {
                var resp = SendJson(ctx, Method, endpoint, payload, headerslist, false); // todo
                responseCode = resp.Item1;
                response = resp.Item2;
                File.WriteAllText(fn, response);
            }
        }
        else {
            var resp = SendJson(ctx, Method, endpoint, payload, headerslist, false); // todo
            responseCode = resp.Item1;
            response = resp.Item2;
        }
        if (varname != "") {
            var vs = plug.GetVariableStore(Persistent);
            lock (vs.Scalar) // verified
            {
                if (!vs.Scalar.ContainsKey(varname)) {
                    vs.Scalar[varname] = new VariableScalar();
                }
                var x = vs.Scalar[varname];
                x.Value = response;
                if (ctx.Trigger != null) {
                    x.LastChanger = I18n.Translate("internal/Action/changetagtrigaction", "Trigger '{0}' action '{1}'", ctx.Trigger.LogName, Describe());
                }
                else {
                    x.LastChanger = I18n.Translate("internal/Action/changetagtestmode", "Action '{0}' test mode", Describe());
                }
                x.LastChanged = DateTime.Now;
            }
            AddToLog(ctx, RealPlugin.DebugLevelEnum.Verbose, I18n.Translate("internal/Action/scalarsetjson",
                "{1}Scalar variable ({0}) value set to JSON response", varname, persist));
        }
        ctx.contextResponse = response;
        ctx.contextResponseCode = responseCode;
        if (FiringExpression != null && FiringExpression.Trim().Length > 0) {
            var firing = ctx.EvaluateStringExpression(ActionContextLogger, ctx, FiringExpression);
            if (firing.Length > 0) {
                plug.LogLineQueuer(firing, "", LogEvent.SourceEnum.Log);
            }
        }
    }

    #endregion

    #region Old Action Converter

    // (this)ActionOld
    public static explicit operator ActionJsonRequest(ActionOld oldAction) {
        var action = new ActionJsonRequest();
        oldAction.CopyCommonPropertiesTo(action);
        action.Endpoint = oldAction._JsonEndpointExpression;
        action.Method = oldAction._JsonOperationType;
        action.Payload = oldAction._JsonPayloadExpression;
        action.Headers = oldAction._JsonHeaderExpression;
        action.ResultVariable = oldAction._JsonResultVariable;
        action.FiringExpression = oldAction._JsonFiringExpression;
        action.UseCache = oldAction._JsonCacheRequest;
        action.Persistent = oldAction._JsonResultVariablePersist;
        return action;
    }

    // (ActionOld)this
    public static explicit operator ActionOld(ActionJsonRequest action) {
        var oldAction = new ActionOld();
        action.CopyCommonPropertiesTo(oldAction);
        oldAction.ActionType = ActionOld.ActionTypeEnum.GenericJson;
        oldAction._JsonEndpointExpression = action.Endpoint;
        oldAction._JsonOperationType = action.Method;
        oldAction._JsonPayloadExpression = action.Payload;
        oldAction._JsonHeaderExpression = action.Headers;
        oldAction._JsonResultVariable = action.ResultVariable;
        oldAction._JsonFiringExpression = action.FiringExpression;
        oldAction._JsonCacheRequest = action.UseCache;
        oldAction._JsonResultVariablePersist = action.Persistent;
        return oldAction;
    }

    #endregion Old Action Converter
}