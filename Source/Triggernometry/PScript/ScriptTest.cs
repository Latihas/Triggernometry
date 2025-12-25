using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Triggernometry.PScript.ScriptUtils;

namespace Triggernometry.PScript;

public class ScriptTest : IScriptBase
{
    public override string RegionIdRegex() => "779|1318";

    public override List<TargetIcon> TargetIconList =>
    [
        new( TTS("点你分散", 1000),targetId:MeHexID,id: "0017"),
        // new(null, MeName, "0083", TTS("点你陨石")),
    ];
    public override List<TargetIcon> StartsCastingList =>
    [
        // new(null, MeName, "0017", TTS("点你分散", 1000)),
        // new(null, MeName, "0083", TTS("点你陨石")),
    ];

    public override void Main(string log)
    {
        MatchTargetIcon(log, TargetIconList);
    }
}
