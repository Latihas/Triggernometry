using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using static Triggernometry.PScript.ScriptUtils;

namespace Triggernometry.PScript;

[SuppressMessage("ReSharper", "VirtualMemberNeverOverridden.Global")]
public abstract class IScriptBase
{
    public bool Enabled;
    public virtual string[]? RegionIdRegex()=>null;
    public virtual List<TargetIcon> TargetIconList => [];
    public virtual List<StartsCasting> StartsCastingList => [];
    public virtual List<StatusAdd> StatusAddList => [];
}
