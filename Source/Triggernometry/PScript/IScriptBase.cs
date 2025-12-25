using System.Collections.Generic;
using static Triggernometry.PScript.ScriptUtils;

namespace Triggernometry.PScript;

public abstract class IScriptBase
{
    public bool Enabled;
    public abstract string RegionIdRegex();
    public abstract void Main(string log);
    public virtual List<TargetIcon> TargetIconList => [];
    public virtual List<TargetIcon> StartsCastingList => [];
}
