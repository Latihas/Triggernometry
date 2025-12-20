using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using Triggernometry.Core;
using Triggernometry.Core.Scripting;
using Triggernometry.PluginBridges;

using Triggernometry.UI.CustomControls;
using Triggernometry.UI.Forms;
using ExpressionTextBox = Triggernometry.UI.CustomControls.ExpressionTextBox;

namespace Triggernometry;

public static class U7aEntry
{
    public static GameConfigForm.ConfigInfo Info = new GameConfigForm.ConfigInfo("绝伊甸宝宝椅", "0.9.2.2", "阿洛 MnFeN", "U7a_cfg");
    static Color Blue = Color.FromArgb(0x22, 0x66, 0xff);

    public static void RunConfigForm()
    {
        try
        {
            _RunConfigForm();
        }
        catch (Exception ex)
        {
            MessageBox.Show("配置界面运行时遇到问题：\n\n" + ex, Info.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void _RunConfigForm()
    {
        GameConfigForm form = new GameConfigForm(Info);
        form.Shown += (sender, e) => RealPlugin.Instance.InvokeNamedCallback("command", "/e <se.9>");
        form.FormClosed += (sender, e) => U7aCactbotHelper.DisableTts();

        BijectDictionary<string, string> cbxItems;
        GameConfigForm.Option optionCtrl;
        Label label;
        string hint = "";
        // 在下面倒序添加各个选项组（组内的选项正序）


        #region 配置：P5

        GameConfigForm.OptionsTableLayoutPanel tableP5 = form.AddOptionGroup(" P5 潘多拉 ");

        // P5 挡枪顺序
        cbxItems = new BijectDictionary<string, string>(
            ("TMRH", " T 近 远 H（国服/美服）"),
            ("THMR", " T H 近 远"),
            ("TMRT", " T 近 远 T")
        );
        optionCtrl = new GameConfigForm.OptionCbx("　挡枪职能顺序", "P5挡枪", cbxItems, "TMRH", hint);
        form.AddOption(optionCtrl, tableP5);

        #endregion 配置：P5

        #region 配置：P4

        GameConfigForm.OptionsTableLayoutPanel tableP4 = form.AddOptionGroup(" P4 琳＆盖娅 ");
        form.AddLabel("· [P4 一运]", tableP4).ForeColor = Blue;

        // P4 一运
        cbxItems = new BijectDictionary<string, string>(
            ("翻绳THMR", " 国服翻绳（MT ST H1 H2 | D1-4）"),
            ("翻绳HTMR", " 日服翻绳（H1 H2 MT ST | D1-4）"),
            ("翻绳HTRM", " 美服翻绳（H1 H2 MT ST | D3 D4 D1 D2）"),
            ("分摊THD", " 分摊基准（MT ST H1 H2 | D1-4）（MMW 文档旧版）"),
            ("分摊HTD", " 分摊基准（H1 H2 MT ST | D1-4）"),
            ("分摊HTD41", " 分摊基准（H1 H2 MT ST | D4-1）"),
            ("分摊DHT", " 分摊基准（D4-1 | H2 H1 ST MT）（MMW 新版 上下倒转）")
        );
        hint = "翻绳打法：括号中为场地北、南两侧四人从西到东的顺序\n\n" +
               "分摊基准：括号中为场地西、东两侧四人从北到南的顺序";
        var optP4一运 = new GameConfigForm.OptionCbx("　一运解法", "P4一运", cbxItems, "翻绳THMR", hint);
        optP4一运.DataChanged += (sender, e) =>
        {
            form.GetOption("P4一运沙漏is主斜换").Enabled = optP4一运.Data.StartsWith("翻绳");
            form.GetOption("P4一运方形is左换").Enabled = optP4一运.Data.StartsWith("翻绳");
        };
        form.AddOption(optP4一运, tableP4);

        // P4 一运沙漏
        cbxItems = new BijectDictionary<string, string>(
            ("1", " 主斜向换位（左上 ↔ 右下）"),
            ("0", " 副斜向换位（右上 ↔ 左下）国/日/美服")
        );
        optionCtrl = new GameConfigForm.OptionCbx("　一运使用翻绳解法时：漏斗换位方式", "P4一运沙漏is主斜换", cbxItems, "0", hint);
        form.AddOption(optionCtrl, tableP4);

        // P4 一运方形
        cbxItems = new BijectDictionary<string, string>(
            ("1", " 左侧换位（左上 ↔ 左下）国服"),
            ("0", " 右侧换位（右上 ↔ 右下）美服/日服")
        );
        optionCtrl = new GameConfigForm.OptionCbx("　一运使用翻绳解法时：方形换位方式", "P4一运方形is左换", cbxItems, "1", hint);
        form.AddOption(optionCtrl, tableP4);

        // P4 一运分散标点
        hint = "使用其他近战 uptime 分散时可以关闭此选项，踩塔判定后还原为初始八方标点";
        optionCtrl = new GameConfigForm.OptionChk("　一运踩塔后分散时标记分散位置", "P4一运分散标点on", true, hint);
        form.AddOption(optionCtrl, tableP4);

        // P4 一运指挥模式
        cbxItems = new BijectDictionary<string, string>(
            ("2", " 为全队标点"),
            ("1", " 为全队标点（但标点后本地取消所有标点）"),
            ("0", " 不开启标点"),
            ("-1", " 不开启标点（且被标点后本地取消所有标点）"),
            ("-2", " 不开启标点（且被标点后强制非本地清除所有标点）")
        );
        hint = "· 引导组：标记攻击 1-4（方向同光暴踩塔，去对应数字标点方向引导位置）\n"
               + "· 连线组：北塔禁止 12、南塔锁链 12（同雷火线）\n\n"
               + "无论此选项是否开启，你均应该选择正确的优先级，为你播报去哪是由优先级而非标点决定的。\n如果有人给你标错了，是他自己优先级设错了的问题。\n\n"
               + "如果你不希望站位时对你毫无帮助的标点遮挡视线，可以选择本地或网络取消标点的选项。";
        optionCtrl = new GameConfigForm.OptionCbx("　一运指挥模式", "P4一运标点mode", cbxItems, "-1", hint);
        form.AddOption(optionCtrl, tableP4);

        form.AddLabel("· [P4 二运]", tableP4).ForeColor = Blue;

        // P4 二运红组优先级
        cbxItems = new BijectDictionary<string, string>(
            ("12345678", " MT-D4"),
            ("34125678", " H1 H2 MT ST D1-4（国服）"),
            ("43215678", " H2 H1 ST MT D1-4（美服）"),
            ("23184567", " H1 MT ST D1-4 H2（日服）")
        );
        optionCtrl = new GameConfigForm.OptionCbx("　二运红组优先级", "P4二运pIdx2prio", cbxItems, 0);
        form.AddOption(optionCtrl, tableP4);

        // P4 二运击退
        cbxItems = new BijectDictionary<string, string>(
            ("击退", " 正常击退"), ("防击退", " 防击退")
        );
        hint = "未适配";
        optionCtrl = new GameConfigForm.OptionCbx("　二运击退解法", "P4二运击退 [未适配] ", cbxItems, "击退", hint);
        optionCtrl.Enabled = false;
        form.AddOption(optionCtrl, tableP4);

        // P4 二运吃圈
        cbxItems = new BijectDictionary<string, string>(
            ("优先", " 按 THD 全队优先级"),
            ("摇号", " 按摇号 1-4（国服/日服）"),
            ("点名", " 按点名固定（美服）（配合下方设置）")
        );
        var optP4二运吃圈 = new GameConfigForm.OptionCbx("　二运蓝点名吃圈顺序（面向 C 从左到右）", "P4二运吃圈", cbxItems, "摇号");
        optP4二运吃圈.DataChanged += (sender, e) =>
        {
            form.GetOption("P4二运点名1").Enabled = optP4二运吃圈.Data == "点名";
            form.GetOption("P4二运点名2").Enabled = optP4二运吃圈.Data == "点名";
            form.GetOption("P4二运点名3").Enabled = optP4二运吃圈.Data == "点名";
            form.GetOption("P4二运点名4").Enabled = optP4二运吃圈.Data == "点名";
            form.GetOption("P4二运自动摇号").Enabled = optP4二运吃圈.Data == "摇号";
        };
        form.AddOption(optP4二运吃圈, tableP4);

        // P4 二运吃圈：点名
        hint = "仅在上方选项选择了根据点名摇号相关的选项时生效，否则可无视";
        cbxItems = new BijectDictionary<string, string>(("冰", " 黑暗冰封"), ("水", " 黑暗狂水"), ("圣", " 黑暗神圣（美服）"), ("暗", " 暗炎喷发"));
        form.AddOption(new GameConfigForm.OptionCbx("　二运点名 1 号（正东）", "P4二运点名1", cbxItems, 2, hint), tableP4);
        cbxItems = new BijectDictionary<string, string>(("冰", " 黑暗冰封"), ("水", " 黑暗狂水（美服）"), ("圣", " 黑暗神圣"), ("暗", " 暗炎喷发"));
        form.AddOption(new GameConfigForm.OptionCbx("　二运点名 2 号（东南）", "P4二运点名2", cbxItems, 1, hint), tableP4);
        cbxItems = new BijectDictionary<string, string>(("冰", " 黑暗冰封（美服）"), ("水", " 黑暗狂水"), ("圣", " 黑暗神圣"), ("暗", " 暗炎喷发"));
        form.AddOption(new GameConfigForm.OptionCbx("　二运点名 3 号（西南）", "P4二运点名3", cbxItems, 0, hint), tableP4);
        cbxItems = new BijectDictionary<string, string>(("冰", " 黑暗冰封"), ("水", " 黑暗狂水"), ("圣", " 黑暗神圣"), ("暗", " 暗炎喷发（美服）"));
        form.AddOption(new GameConfigForm.OptionCbx("　二运点名 4 号（正西）", "P4二运点名4", cbxItems, 3, hint), tableP4);

        // P4 二运吃圈摇号
        cbxItems = new BijectDictionary<string, string>(
            ("自身", " 自动为自己摇号"),
            ("全队优先", " 自动为全队按 THD 全队优先级摇号"),
            ("全队点名", " 自动为全队按点名摇号（配合前面的设置）"),
            ("被动", " 手动或由别人为自己摇号")
        );
        optionCtrl = new GameConfigForm.OptionCbx("　二运指挥 / 自动摇号（仅选择摇号打法时有效）", "P4二运自动摇号", cbxItems, "自身");
        form.AddOption(optionCtrl, tableP4);

        // P4 二运标点标记光波起始斜方向
        hint = "· 保留目标斜点数字点，其余三个数字点从场中指向此方向";
        optionCtrl = new GameConfigForm.OptionChk("　二运标点标记光波起始的斜方向", "P4二运指路on", true, hint);
        form.AddOption(optionCtrl, tableP4);

        #endregion 配置：P4

        #region 配置：P3

        GameConfigForm.OptionsTableLayoutPanel tableP3 = form.AddOptionGroup(" P3 暗之巫女 ");
        form.AddLabel("· [P3 一运]", tableP3).ForeColor = Blue;

        // P3 一运解法
        cbxItems = new BijectDictionary<string, string>(
            ("国", " 国服（日服也选这项，详见下面 P3 一运摇号）"),
            ("国HTDH", " 国服（但改为 HTDH 优先级）"),
            ("美", " 美服")
        );
        hint = "· 国服：\n    优先级 MT-H2 | D1-D4 \n    同组高左低右、D 北 TH 南、中火左 TH 右 D\n    详见 MMW 文档\n\n"
               + "· 美服：\n    优先级 ST MT H1 H2 | D2134 \n    同组高左低右、D 北 TH 南、中火左 D 右 TH\n    详见 LesBin / NaurFFXIV";
        var optP3一运 = new GameConfigForm.OptionCbx("　一运攻略解法", "P3一运", cbxItems, "国", hint);
        optP3一运.DataChanged += (sender, e)
            => form.GetOption("P3一运自动摇号on").Enabled = optP3一运.Data == "国";
        form.AddOption(optP3一运, tableP3);

        // P3 一运摇号
        hint = "此选项会在 P3 一运中，\n" +
               "自动按照国服解法为自己摇出正确方向的号，\n" +
               "以此实现将日服攻略转化为与国服完全一致的解法。\n" +
               "摇号被顶掉后会重新摇回来。\n" +
               "攻略选项中 P3 一运需要选择国服。\n\n" +
               "随机摇号 爬";
        optionCtrl = new GameConfigForm.OptionChk("　一运自动摇号（日服专用 详见说明）", "P3一运自动摇号on", false, hint);
        form.AddOption(optionCtrl, tableP3);

        // P3 一运标点模式
        cbxItems = new BijectDictionary<string, string>(
            ("0", " 不开启全队标点（不影响上一选项）"),
            ("-1", " 不开启全队标点（且被标点后本地取消所有标点）"),
            ("-2", " 不开启全队标点（且被标点后强制非本地清除所有标点）")
        );
        hint = "如果你不希望站位时对你毫无帮助的标点遮挡视线，可以选择本地或网络取消标点的选项。";
        optionCtrl = new GameConfigForm.OptionCbx("　一运屏蔽标点", "P3一运标点mode", cbxItems, "-1", hint);
        form.AddOption(optionCtrl, tableP3);

        // P3 一运镜子指北
        hint = "· 调用特效：一运阶段，在连线的 12 点方向生成 P2 的白镜子，指示相对北";
        optionCtrl = new GameConfigForm.OptionChk("　一运时在北侧放置镜子", "P3一运镜子on", true, hint);
        form.AddOption(optionCtrl, tableP3);

        var hintP32 = @"「分散/分摊分组」「地火安全区划分半场」二者彼此完全独立。
   MMW 攻略讲解的 P3 二运杂糅了这两项内容，特此说明：

· 「双分组」 指分散与分摊的分组方式：
   双分组法的分散和分摊的分组无关，
   与之相对的是初始换位后沿用同一分组的「单分组」解法；

· 「车头」指地火安全区如何划分半场：
   所谓的车头法以地火垂向的单人（车头）安全区为基准划分半场，
   与之相对的是以地火逆 45° 的人群安全区为基准的「人群基准」，
   或以初始地火为基准的「地火基准」解法。

· 这两项完全可以自由组合：
   MMW 的「双分组」实为「双分组」+「人群基准」；
   「车头法」实为「单分组」+「车头法」；
   还有其他攻略使用了另外两种的组合方式，自行根据说明理解。

· 关于优先级和换位：
   对于分两组的换位解法，完全等价于全队优先级。
   两侧不换位者即为最高/低优先级，同点名高去 1 组，低去 2 组。
   如低顺位（H2 D4）优先换位到对侧的解法，即不动的 MT D1 在两侧，
   等价于 MT ST H1 H2 D4 D3 D2 D1 优先级。
   因此下述选项中使用清晰直观的优先级统一表述。

看完这段还不知道你看的攻略怎么选的话，建议重看攻略。
这说明你对攻略的理解还不配进本，别来群里问问问问问问。";
        label = form.AddLabel("· [P3 二运]    关于 P3 二运打法的详细说明（双击此行查看）", tableP3);
        label.ForeColor = Color.FromArgb(0x22, 0x66, 0xff);
        label.DoubleClick += (sender, e)
            => MessageBox.Show(hintP32, "P3 二运说明", MessageBoxButtons.OK, MessageBoxIcon.Information);

        // P3 二运分组方式
        cbxItems = new BijectDictionary<string, string>(
            ("双分国", " 双分组：H12 > T12 > D1-4（MMW）"),
            ("双分HTDH", " 双分组：H1 > T12 > D1-4 > H2"),
            ("双分美", " 双分组：H21 > T21 > D1-4（美服）"),
            ("单分THD", " 单分组：T12 > H12 > D1-4"),
            ("单分莫", " 单分组：T12 > H12 > D4-1（MMW）"),
            ("单分HTDH", " 单分组：H1 > T12 > D1-4 > H2"),
            ("单分日", " 单分组：H21 > T21 > D1-4（日服）"),
            ("单分科", " 单分组：根据科技标点（自动分配每组双近双远）")
        );
        hint = "· 双分组：\n    分摊分组按优先级确定，分散分组按职能固定 1/2 组\n    每人分散分摊都去固定位置，详见 MMW 文档\n\n"
               + "· 单分组：\n    分摊分组按指定优先级确定，分散时按分摊的分组\n\n"
               + "· 科技解法（属于单分组）：\n    按照指定规则，确保每组双近双远\n    按头顶标记去指定位置";
        var optP3二运 = new GameConfigForm.OptionCbx("　二运分散与分摊的分组及优先级", "P3二运", cbxItems, "双分国", hint);
        optP3二运.DataChanged += (sender, e)
            => form.GetOption("P3二运isCmd").Enabled = optP3二运.Data == "单分科";
        form.AddOption(optP3二运, tableP3);

        // P3 二运分散找半场
        cbxItems = new BijectDictionary<string, string>(
            ("地火", " 初始地火基准（然后反向找安全区）（美服）"),
            ("地火-", " 人群安全区基准（地火逆 45° 的安全区）"),
            ("地火--", " 单人（车头）安全区基准（地火垂直 90° 的安全区）")
        );
        hint = "人群安全区基准是 MMW 视频双分组介绍的分半场方式；\n" +
               "单人安全区基准是 MMW 视频 MT 车头法介绍的分半场方式。\n" +
               "不知道选哪个就好好看你用的攻略怎么讲的 别来群里问问问。\n\n";
        optionCtrl = new GameConfigForm.OptionCbx("　二运地火分散划分半场的方式", "P3二运分半场", cbxItems, "地火--");
        form.AddOption(optionCtrl, tableP3);

        // P3 二运指挥模式
        cbxItems = new BijectDictionary<string, string>(
            ("1", " 为全队标点"),
            ("0", " 由别人为自己标点")
        );
        hint = "· 一组 攻击 1 2 3 4\n" +
               "· 二组 锁链 1 2 3 方块\n" +
               " MT 固定为攻击 1（去一组引导位）";
        optionCtrl = new GameConfigForm.OptionCbx("　二运指挥模式（单分组科技打法）", "P3二运isCmd", cbxItems, "0", hint);
        form.AddOption(optionCtrl, tableP3);

        // P3 二运未来观测
        hint = "· 允许聊天文本和 TTS 在地火出现时立刻播报结果，不需等待动画";
        optionCtrl = new GameConfigForm.OptionChk("　二运地火未来观测", "P3二运未来观测on", true, hint);
        form.AddOption(optionCtrl, tableP3);

        #endregion 配置：P3

        #region 配置：P2

        GameConfigForm.OptionsTableLayoutPanel tableP2 = form.AddOptionGroup(" P2 希瓦 · 米特隆 ");

        form.AddLabel("· [P2 钻石星尘]", tableP2).ForeColor = Blue;

        // P2 钻石星尘需要逆时针起跑时是否均逆时针
        cbxItems = new BijectDictionary<string, string>(
            ("1", " 两组均逆时针反向起跑"), ("0", " 仅分身边上的一组逆时针起跑")
        );
        optionCtrl = new GameConfigForm.OptionCbx("　钻石星尘 需要反向起跑时：", "P2DD冰is双逆", cbxItems, "1");
        form.AddOption(optionCtrl, tableP2);

        // P2 DD 连线背对
        hint = "· 调用特效：钻石星尘阶段，背对判定前，将自身与释放背对的分身连线";
        optionCtrl = new GameConfigForm.OptionChk("　开启冰、光分身连线特效", "P2背对连线on", true, hint);
        form.AddOption(optionCtrl, tableP2);

        form.AddLabel("· [P2 镜之国]", tableP2).ForeColor = Blue;

        // P2 红镜子变白
        hint = "· 调用特效：在白镜子判定后，将剩余的两个红镜子变为白镜子，以防巨大的红色特效影响观察站位";
        optionCtrl = new GameConfigForm.OptionChk("　红镜子判定前变白", "P2镜子变白on", true, hint);
        form.AddOption(optionCtrl, tableP2);

        form.AddLabel("· [P2 光之暴走]", tableP2).ForeColor = Blue;

        // P2 光之暴走
        cbxItems = new BijectDictionary<string, string>(
            ("灰九", " 灰九式 斜向六角星（MMW）"),
            ("翻绳THD", " 翻绳 南北向六角星（MMW，THD）"),
            ("翻绳HTD", " 翻绳 南北向六角星（日服，HTD）"),
            ("翻绳美", " 翻绳 南北向六角星（美服，见说明）")
        );
        hint = "· 灰九：\n    正北开始 顺时针 正八方站位确定优先级：\n    MT D4 ST D2 H2 D1 H1 D3\n    六人根据优先级分别去 C1243A\n    正东西拉线\n\n"
               + "· 翻绳（MMW/日野）：\n    正西开始 顺时针 斜八方站位确定优先级：\n    THD：MT ST H1 H2 D4 D3 D2 D1\n    HTD：H1 H2 MT ST D4 D3 D2 D1\n    六人根据优先级分别去 1C24A3\n    正南北偏顺 30° 拉线\n\n"
               + "· 翻绳（美服）：\n    正西开始 顺时针 斜八方站位：\n    H1 H2 MT ST D2 D1 D4 D3\n    南北两侧为 2/4 时，多的一侧最偏顺时针的人顺时针旋转补位（无法等效于单一优先级）\n    六边形所去站位同上\n    正南北偏顺 30° 拉线";
        optionCtrl = new GameConfigForm.OptionCbx("　光之暴走解法", "P2光暴", cbxItems, "灰九", hint);
        form.AddOption(optionCtrl, tableP2);

        // P2 光暴指挥模式
        cbxItems = new BijectDictionary<string, string>(
            ("2", " 为全队标点"),
            ("1", " 为全队标点（但标点后本地取消所有标点）"),
            ("0", " 不开启标点"),
            ("-1", " 不开启标点（且被标点后本地取消所有标点）"),
            ("-2", " 不开启标点（且被标点后强制非本地清除所有标点）")
        );
        hint = "· 为连线的六人标点指示踩哪个塔：\n    攻击 1 2 3 4 去对应场地标点 1-4 方向；\n    锁链 1 2 去 A C\n\n" +
               "无论是否开启，该去的位置均由选用的打法决定，与标点无关。";
        optionCtrl = new GameConfigForm.OptionCbx("　光之暴走指挥模式", "P2光暴标点mode", cbxItems, "-1", hint);
        form.AddOption(optionCtrl, tableP2);

        form.AddLabel("· [P2.5 光暗水晶]", tableP2).ForeColor = Blue;

        // P2.5 消除冰地板
        hint = "· 调用特效：在地板冻结后立刻解除冰冻，此功能用于避免冰地板导致扇形看不见的 bug。\n目前 SE 已修复此 bug，但开启此选项可降低光污染。\n\n· 注：开启此选项会导致水晶被实际打碎前可以走进其目标圈。\n  （这意味着可以去正中间打 15 米 AoE 贪四个伤害）";
        optionCtrl = new GameConfigForm.OptionChk("　结冰后消除冰地板", "P2碎冰on", true, hint);
        form.AddOption(optionCtrl, tableP2);

        // P2.5 暗水晶不可选中
        hint = "· 将四个暗水晶实体设置为无法选中的状态";
        optionCtrl = new GameConfigForm.OptionChk("　暗水晶不可选中", "P2不可选暗on", true, hint);
        form.AddOption(optionCtrl, tableP2);

        #endregion 配置：P2

        #region 配置：P1

        GameConfigForm.OptionsTableLayoutPanel tableP1 = form.AddOptionGroup(" P1 绝命战士 ");

        // P1 雾龙连线两人去的位置
        form.AddLabel("· [P1 光轮召唤]", tableP1).ForeColor = Blue;

        cbxItems = new BijectDictionary<string, string>(
            ("THD", " 高优先级去北、低优先级去南（THD）（国服）"),
            ("HTD", " 高优先级去北、低优先级去南（HTD）（日服）"),
            ("HTDH", " 高优先级去北、低优先级去南（HTDH）"),
            ("长短", " 长线去北、短线去南（美服）"),
            ("短长", " 短线去北、长线去南"),
            ("左右", " 左下分身连线去北、右下分身连线去南"),
            ("右左", " 右下分身连线去北、左下分身连线去南")
        );
        optionCtrl = new GameConfigForm.OptionCbx("　连线两人去的位置", "P1雾龙线", cbxItems, "THD");
        form.AddOption(optionCtrl, tableP1);

        // P1 光轮缩放
        hint = "· 改变特效：八个光轮初始缩小且半透明，以便观察站位；后续判定时放大到实际判定范围";
        optionCtrl = new GameConfigForm.OptionChk("　开启光轮缩小特效", "P1光轮缩放on", true, hint);
        form.AddOption(optionCtrl, tableP1);

        form.AddLabel("· [P1 雷火四线]", tableP1).ForeColor = Blue;

        // P1 雷火线谁固定站位
        cbxItems = new BijectDictionary<string, string>(
            ("线", " 连线固定（国/日/美服）"),
            ("闲", " 闲人固定（旧版）"),
            ("闲新", " 闲人固定（仅可横排，MMW 新版）")
        );
        hint = "· 连线组固定：\n    连线者固定前后站位\n    闲人根据线去左右或正后\n\n" +
               "· 闲人组固定：\n    闲人固定左右站位\n    连线者根据线去中间或左前\n\n" +
               "· 闲人组固定（新）：场地基准\n    闲人固定正左、左下、右下、正右\n    连线者根据线去中间或北侧";
        var optP1四线固定 = new GameConfigForm.OptionCbx("　雷火线攻略解法", "P1四线固定", cbxItems, "1", hint);
        form.AddOption(optP1四线固定, tableP1);

        // P1 雷火线南北/东西
        cbxItems = new BijectDictionary<string, string>(
            ("1", " 1/3 线北、2/4 线南（国服）"), ("0", " 1/3 线西、2/4 线东（美服/日服）")
        );
        var optP1线is南北 = new GameConfigForm.OptionCbx("　雷火线横向 / 纵向处理机制", "P1线is南北", cbxItems, "1");
        form.AddOption(optP1线is南北, tableP1);
        optP1四线固定.DataChanged += (sender, e) => optP1线is南北.Enabled = optP1四线固定.Data != "闲新";

        // P1 雷火线优先级
        cbxItems = new BijectDictionary<string, string>(
            ("HTD", " H > T > D（国服/日服）"), ("THD", " T > H > D"), ("HTDH", " H1 > T > D > H2"),
            ("TMHR", " D3 H1 D1 MT ST D2 H2 D4（美服）"), ("TMRH", " H1 D3 D1 MT ST D2 D4 H2"),
            ("MTHR", " D3 H1 MT D1 D2 ST H2 D4"), ("MTRH", " H1 D3 MT D1 D2 ST D4 H2")
        );
        hint = "注：单排双排与优先级完全无关，爱站哪站哪";
        optionCtrl = new GameConfigForm.OptionCbx("　雷火线全队优先级", "P1线Prio", cbxItems, "HTD", hint);
        form.AddOption(optionCtrl, tableP1);

        // P1 四线指挥模式
        cbxItems = new BijectDictionary<string, string>(
            ("2", " 为全队标点"),
            ("1", " 为全队标点（但标点后本地取消所有标点）"),
            ("0", " 不开启标点"),
            ("-1", " 不开启标点（且被标点后本地取消所有标点）"),
            ("-2", " 不开启标点（且被标点后强制非本地清除所有标点）")
        );
        hint = "· 闲人：按优先级攻击 1-4（1 2 为一组，3 4 为二组）\n"
               + "· 连线：按顺序禁止 1、锁链 1、禁止 2、锁链 2（禁止去一组，锁链去二组）\n\n"
               + "无论此选项是否开启，你均应该选择正确的优先级，为你播报去哪是由优先级而非标点决定的。\n如果有人给你闲人组顺序标错了，是他自己优先级设错了的问题。\n\n"
               + "如果你不希望站位时对你毫无帮助的标点遮挡视线，可以选择本地或网络取消标点的选项。";
        optionCtrl = new GameConfigForm.OptionCbx("　雷火线指挥模式", "P1四线标点mode", cbxItems, "-1", hint);
        form.AddOption(optionCtrl, tableP1);

        form.AddLabel("· [P1 六人塔]", tableP1).ForeColor = Blue;

        // P1 六人踩塔解法
        cbxItems = new BijectDictionary<string, string>(
            ("固定HHD", " 3 固定 3 换位：H1 D1 | H2 D2 | D4 D3（MMW）"),
            ("固定HDH", " 3 固定 3 换位：H1 D1 | D4 D3 | H2 D2（MMW 旧版）"),
            ("结合HHD", " 3 固定 3 优先级：H1 | H2 | D4（日服）"),
            ("结合HDH", " 3 固定 3 优先级：H1 | D4 | H2（美服）"),
            ("优先", " 6 人优先级（日服旧版）")
        );
        hint = "· 三人固定、三人换位：\n    法系从上到下固定、D1-3 按需补位（类似龙诗 P3）\n\n"
               + "· 三人固定、三人优先级：\n    法系从上到下固定、D1-3 按优先级从上到下补塔\n\n"
               + "· 六人优先级：\n    H1 H2 D1-4 优先级从上到下踩塔";
        optionCtrl = new GameConfigForm.OptionCbx("　踩塔解法", "P1塔", cbxItems, "固定", hint);
        form.AddOption(optionCtrl, tableP1);

        #endregion 配置：P1

        /* 实体缩放
                #region 配置：实体缩放
                OptionsTableLayoutPanel tableScaling = form.AddOptionGroup(" 实体缩放倍率（1 代表原始大小）  注：以下选项目前无效，会应用内置的固定缩放倍率");
                form.AddOption(new OptionTxt("[实体缩放] 桑克瑞德本体", "Scaling桑", "1"), tableP1);
                form.AddOption(new OptionTxt("[实体缩放] 桑克瑞德分身（雾龙阶段）", "Scaling桑2", "1"), tableP1);
                form.AddOption(new OptionTxt("[未适配] 琳（P2）", "Scaling琳", "1"), tableScaling);
                form.AddOption(new OptionTxt("[未适配] 盖娅（P3）", "Scaling盖", "1"), tableScaling);
                form.AddOption(new OptionTxt("[未适配] 琳（P4）", "Scaling琳2", "1"), tableScaling);
                form.AddOption(new OptionTxt("[未适配] 盖娅（P4）", "Scaling盖2", "1"), tableScaling);
                form.AddOption(new OptionTxt("[未适配] 潘多拉", "Scaling潘", "1"), tableScaling);
                #endregion 配置：实体缩放
        */

        #region 配置：通用选项

        GameConfigForm.OptionsTableLayoutPanel tableGeneral = form.AddOptionGroup(" 通用选项 ");

        form.AddLabel("未提供本地场地标点的开关。如果要关闭，去鲶鱼精邮差临时关闭 WayMark。", tableGeneral);

        // 开场自动标点
        cbxItems = new BijectDictionary<string, string>(
            ("2", " 公开标点（北侧顺时针 1A2、半径 10.0 m）"),
            ("1", " 本地标点（同上，队里使用其他标点时可以本地覆盖掉）"),
            ("0", " 不开启标点")
        );
        optionCtrl = new GameConfigForm.OptionCbx("开场自动应用标准场地标点（防内鬼标点）", "P0场地标点mode", cbxItems, "2");
        form.AddOption(optionCtrl, tableGeneral);

        // P1 校准小队
        hint = "默认站位：正北逆 MT D3 H1 D1 [H2/ST] D2 [ST/H2] D4\n会在开场八方分散时检查全员站位，如果与设置不符则报错并自动修正。\n魔改站位时不要开。";
        optionCtrl = new GameConfigForm.OptionChk("开场八方时检查并修正小队", "P1修复小队on", true, hint);
        form.AddOption(optionCtrl, tableGeneral);

        // 划分半场
        cbxItems = new BijectDictionary<string, string>(
            ("1", " 正北至西南为 MT 组（国/美服）"),
            ("0", " 东北至正西为 MT 组（日服）")
        );
        hint = "触发器所有逻辑均基于 1A2 标点播报，仅 P1、P3 半场划分不同。\n如果你一定要使用日服的 4A1，可以下方选项中开启本地标点覆盖，使 TTS 与标点一致。";
        optionCtrl = new GameConfigForm.OptionCbx("半场分界线", "半场is1A2", cbxItems, "1", hint);
        form.AddOption(optionCtrl, tableGeneral);

        #endregion 配置：通用选项

        #region 预设

        GameConfigForm.OptionsTableLayoutPanel tablePresets = form.AddOptionGroup(" 预设 ");

        label = form.AddLabel("此分组的选项用于读取或记录预设，本身不是配置选项。读取后依然需要在下方保存。", tablePresets);
        label.ForeColor = Blue;

        // 读取预设
        cbxItems = new BijectDictionary<string, string>(
            ("MMW", " 国服 MMW：光暴灰九 + P3 双分组人群基准 + P4 国服翻绳"),
            ("EN", " 美服攻略：NaurFFXIV / LesBin 整合"),
            ("JP", " 日服攻略"),
            ("User1", " 自定义预设 1"),
            ("User2", " 自定义预设 2"),
            ("P1四线横排", "- [部分] P1 雷火四线改为新版横排闲人固定"),
            ("P2光暴翻绳", "- [部分] P2 光暴改为正六角星翻绳"),
            ("P3二运科技指挥", "- [部分] P3 二运改为科技单分组（指挥）"),
            ("P3二运科技被指挥", "- [部分] P3 二运改为科技单分组（被指挥）"),
            ("P4一运分摊基准", "- [部分] P4 一运改为分摊基准")
        );
        hint = "标记为 [部分] 的选项仅会影响相应的部分，需要先配置好其他部分的攻略再覆盖应用。";
        var optionLoadPreset = new GameConfigForm.OptionCbx("· [双击此行] 应用右侧的攻略或自定义预设配置", null, cbxItems, "MMW", hint);
        form.AddOption(optionLoadPreset, tablePresets);
        optionLoadPreset.Lbl.MouseDoubleClick += (sender, e) =>
        {
            var preset = (PresetEnum)Enum.Parse(typeof(PresetEnum), optionLoadPreset.Data);
            ApplyPreset(preset, form);
        };

        // 保存自定义预设
        cbxItems = new BijectDictionary<string, string>(
            ("User1", " 自定义预设 1"),
            ("User2", " 自定义预设 2")
        );
        var optionSavePreset = new GameConfigForm.OptionCbx("· [双击此行] 记录当前选项到右侧自定义预设", null, cbxItems, "User1");
        form.AddOption(optionSavePreset, tablePresets);
        optionSavePreset.Lbl.MouseDoubleClick += (sender, e) =>
        {
            var preset = (PresetEnum)Enum.Parse(typeof(PresetEnum), optionSavePreset.Data);
            SavePreset(preset, form);
        };

        // 导入导出
        var lbl = form.AddLabel("· [双击此行] 导入/导出攻略部分的预设（和其他人分享）", tablePresets);
        lbl.MouseDoubleClick += (sender, e) => ImportExportStrats(form);

        #endregion 预设

        #region 配置：文本频道

        GameConfigForm.OptionsTableLayoutPanel tableChannel = form.AddOptionGroup(" 文本频道 ");
        var cnlItems = new BijectDictionary<string, string>(
            ("", " （禁用）"), ("p", " 小队"), ("e", " 默语"), ("fc", " 部队"),
            ("l1", " 通讯贝1"), ("l2", " 通讯贝2"), ("l3", " 通讯贝3"), ("l4", " 通讯贝4"),
            ("l5", " 通讯贝5"), ("l6", " 通讯贝6"), ("l7", " 通讯贝7"), ("l8", " 通讯贝8"),
            ("cwl1", " 跨服通讯贝1"), ("cwl2", " 跨服通讯贝2"), ("cwl3", " 跨服通讯贝3"), ("cwl4", " 跨服通讯贝4"),
            ("cwl5", " 跨服通讯贝5"), ("cwl6", " 跨服通讯贝6"), ("cwl7", " 跨服通讯贝7"), ("cwl8", " 跨服通讯贝8")
        );
        var cnlPublicItems = cnlItems.ShallowCopy();
        cnlPublicItems.RemoveKey("");
        var cnlDebugItems = cnlItems.ShallowCopy();
        cnlDebugItems.RemoveKey("p");
        cnlDebugItems.RemoveKey(""); // 暂时
        var cnlPrivateItems = cnlItems.ShallowCopy();
        cnlPrivateItems.RemoveKey("p");
        cnlPrivateItems.RemoveKey("");

        GameConfigForm.OptionCbx cnlPublic = new GameConfigForm.OptionCbx("公共频道", "publicChannel", cnlPublicItems, "e",
                                                                          hint: "本频道用于播报部分机制、示意图、复盘信息等机制相关内容。\n\n默认设为自己可见的默语频道，\n如有需要可设为小队等。\n\n当开启了某个阶段的指挥模式时，相关播报所用的频道会无视此项设置，覆盖为小队频道。");
        GameConfigForm.OptionCbx cnlPrivate = new GameConfigForm.OptionCbx("个人频道", "privateChannel", cnlPrivateItems, "e",
                                                                           hint: "本频道用于发送仅应自己可见的系统提示消息。\n\n本项设置不应选择任何其他人可见的频道。");
        GameConfigForm.OptionCbx cnlDebug = new GameConfigForm.OptionCbx("调试频道", "debugChannel", cnlDebugItems, "",
                                                                         hint: "本频道用于发送额外调试信息。\n\n请设置为仅自己可见的频道，或将其关闭。\n\n（测试版暂不提供关闭选项）");
        form.AddOption(cnlPublic, tableChannel);
        form.AddOption(cnlPrivate, tableChannel);
        form.AddOption(cnlDebug, tableChannel);

        #endregion 配置：文本频道

        #region 使用须知

        GameConfigForm.OptionsTableLayoutPanel tableInfo = form.AddOptionGroup(" 使用须知 ");
        var hintCactbot = "Cactbot 官库攻略与国服不兼容。\n" +
                          "如果你当前未关闭 Cactbot 官库 TTS，关闭此界面时会自动帮你关闭。\n\n" +
                          "如果你想将文本显示一起关闭，可以手动操作：\n" +
                          "ACT 页面上方「OverlayPlugin 悬浮窗插件」选项卡 - \n" +
                          "左侧「Cactbot 设置」选项卡 - \n" +
                          "展开右侧菜单「RAIDBOSS / 时间轴与触发器提示」- \n" +
                          "下方 7.x 中找到「光暗未来绝境战」- \n" +
                          "将首个设置更改为「禁用」";
        label = form.AddLabel("· 如果安装了 Cactbot，使用前必须关闭 Cactbot 的播报（双击此行查看说明）", tableInfo);
        label.ForeColor = Blue;
        label.DoubleClick += (sender, e)
            => MessageBox.Show(hintCactbot, "关闭 Cactbot 默认播报", MessageBoxButtons.OK, MessageBoxIcon.Information);

        form.AddLabel("· 使用前必须查看使用说明，见绝伊甸触发器分组下的「使用说明」触发器。", tableInfo)
            .ForeColor = Blue;

        label = form.AddLabel("· 下方部分选项上悬停鼠标可以显示详细说明。", tableInfo);
        label.ForeColor = Blue;

        #endregion 使用须知

        #region 配置：队员顺序

        if (BridgeFFXIV.ZoneID == int.Parse("01238") && Triggernometry.FFXIV.Entity.GetEntities().Any(e => e.InParty))
        {
            string[] playerDescriptions = { "MT", "ST", "H1", "H2", "D1", "D2", "D3", "D4" };
            var pListPanel = new PartyListPanel(playerDescriptions);
            form.AddPartyGroup(" 队员顺序（拖拽调整） ", pListPanel);
        }

        #endregion 配置：队员顺序

        Application.OpenForms.OfType<GameConfigForm>().ToList().ForEach(f => f.Close());
        form.Run();
    }

    enum PresetEnum
    {
        MMW,
        P1四线横排,
        P2光暴翻绳,
        P3二运科技指挥,
        P3二运科技被指挥,
        P4一运分摊基准,
        EN,
        JP,
        User1,
        User2
    }

    private static void ApplyPreset(PresetEnum preset, GameConfigForm cfgForm)
    {
        var cfgDict = new Dictionary<string, string>();
        var presetDesc = "";
        var additionalInfo = "";
        switch (preset)
        {
            case PresetEnum.MMW:
                cfgDict["半场is1A2"] = "1";
                cfgDict["P1修复小队on"] = "1";
                cfgDict["P1雾龙线"] = "THD";
                cfgDict["P1线Prio"] = "HTD";
                cfgDict["P1四线固定"] = "线";
                cfgDict["P1线is南北"] = "1";
                cfgDict["P1塔"] = "固定HHD";
                cfgDict["P2DD冰is双逆"] = "1";
                cfgDict["P2光暴"] = "灰九";
                cfgDict["P3一运"] = "国";
                cfgDict["P3二运"] = "双分国";
                cfgDict["P3二运分半场"] = "地火-";
                cfgDict["P4一运"] = "翻绳THMR";
                cfgDict["P4一运沙漏is主斜换"] = "0";
                cfgDict["P4一运方形is左换"] = "1";
                cfgDict["P4二运pIdx2prio"] = "34125678";
                cfgDict["P4二运击退"] = "击退";
                cfgDict["P4二运吃圈"] = "摇号";
                cfgDict["P5挡枪"] = "TMRH";
                cfgDict["P3一运自动摇号on"] = "0";
                cfgDict["P4二运自动摇号"] = "自身";
                presetDesc = "MMW 预设";
                additionalInfo = "对于光暴、P3 二运、P4 一运等有分歧的选项，你依然需要自行设置。\n\n配置无法保证实时跟随攻略更新，进本前需要检查配置选项。";
                break;
            case PresetEnum.P1四线横排:
                cfgDict["P1线Prio"] = "HTD";
                cfgDict["P1四线固定"] = "闲新";
                cfgDict["P1线is南北"] = "0";
                presetDesc = "P1 雷火线新版横排闲人固定预设";
                additionalInfo = "此项预设仅修改 P1 雷火线解法。";
                break;
            case PresetEnum.P2光暴翻绳:
                cfgDict["P2光暴"] = "翻绳THD";
                presetDesc = "P2 光暴翻绳预设（MMW 视频攻略版）";
                additionalInfo = "此项预设仅修改 P2 光暴解法。";
                break;
            case PresetEnum.P3二运科技指挥:
                cfgDict["P3二运"] = "单分科";
                cfgDict["P3二运isCmd"] = "1";
                presetDesc = "P3 二运科技单分组预设（指挥）";
                additionalInfo = "此项预设仅修改 P3 二运解法。此外你需要手动指定地火分半场的基准。";
                break;
            case PresetEnum.P3二运科技被指挥:
                cfgDict["P3二运"] = "单分科";
                cfgDict["P3二运isCmd"] = "0";
                presetDesc = "P3 二运科技单分组预设（被指挥）";
                additionalInfo = "此项预设仅修改 P3 二运解法。此外你需要手动指定地火分半场的基准。";
                break;
            case PresetEnum.P4一运分摊基准:
                cfgDict["P4一运"] = "分摊DHT";
                presetDesc = "P4 一运分摊基准预设（MMW 攻略视频版本）";
                additionalInfo = "此项预设仅修改 P4 一运解法。";
                break;
            case PresetEnum.EN:
                cfgDict["半场is1A2"] = "1";
                cfgDict["P1修复小队on"] = "1";
                cfgDict["P1雾龙线"] = "长短";
                cfgDict["P1线Prio"] = "TMHR";
                cfgDict["P1四线固定"] = "线";
                cfgDict["P1线is南北"] = "0";
                cfgDict["P1塔"] = "结合HDH";
                cfgDict["P2DD冰is双逆"] = "1";
                cfgDict["P2光暴"] = "翻绳美";
                cfgDict["P3一运"] = "美";
                cfgDict["P3二运"] = "双分美";
                cfgDict["P3二运分半场"] = "地火";
                cfgDict["P4一运"] = "翻绳HTRM";
                cfgDict["P4一运沙漏is主斜换"] = "0";
                cfgDict["P4一运方形is左换"] = "0";
                cfgDict["P4二运pIdx2prio"] = "43215678";
                cfgDict["P4二运击退"] = "击退";
                cfgDict["P4二运吃圈"] = "点名";
                cfgDict["P4二运点名1"] = "圣";
                cfgDict["P4二运点名2"] = "水";
                cfgDict["P4二运点名3"] = "冰";
                cfgDict["P4二运点名4"] = "暗";
                cfgDict["P5挡枪"] = "TMRH";
                cfgDict["P3一运自动摇号on"] = "0";
                presetDesc = "美服预设";
                additionalInfo = "配置无法保证实时跟随攻略更新，进本前需要检查配置选项。";
                break;
            case PresetEnum.JP:
                cfgDict["半场is1A2"] = "0";
                cfgDict["P1修复小队on"] = "1";
                cfgDict["P1雾龙线"] = "HTD";
                cfgDict["P1线Prio"] = "HTD";
                cfgDict["P1四线固定"] = "线";
                cfgDict["P1线is南北"] = "0";
                cfgDict["P1塔"] = "结合HHD";
                cfgDict["P2DD冰is双逆"] = "0";
                cfgDict["P2光暴"] = "翻绳HTD";
                cfgDict["P3一运"] = "国";
                cfgDict["P3二运"] = "单分日";
                cfgDict["P3二运分半场"] = "地火-";
                cfgDict["P4一运"] = "翻绳HTMR";
                cfgDict["P4一运沙漏is主斜换"] = "0";
                cfgDict["P4一运方形is左换"] = "0";
                cfgDict["P4二运pIdx2prio"] = "23184567";
                cfgDict["P4二运击退"] = "击退";
                cfgDict["P4二运吃圈"] = "摇号";
                cfgDict["P5挡枪"] = "TMRH";
                cfgDict["P3一运自动摇号on"] = "1";
                cfgDict["P4二运自动摇号"] = "自身";
                presetDesc = "日服预设";
                additionalInfo = "配置无法保证实时跟随攻略更新，进本前需要检查配置选项。";
                break;
            case PresetEnum.User1:
            case PresetEnum.User2:
                var idx = preset == PresetEnum.User1 ? 1 : 2;
                if (cfgForm.TryGetPreset(idx, out var vd))
                {
                    cfgDict = vd.Values.ToDictionary(p => p.Key, p => p.Value.ToString());
                    var presetName = cfgDict.TryGetValue("PresetName", out var pn) ? pn : "未命名";
                    presetDesc = $"预设 #{idx} ({presetName})";
                }
                else
                {
                    MessageBox.Show($"未保存过自定义预设 {idx}。", Info.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                break;
            default:
                return;
        }
        var result = MessageBox.Show($"是否应用：{presetDesc}？", Info.Name, MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
        if (result != DialogResult.OK) return;
        cfgForm.ApplyPreset(cfgDict);
        MessageBox.Show($"已应用：{presetDesc}。\n\n{additionalInfo}", Info.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void SavePreset(PresetEnum preset, GameConfigForm cfgForm)
    {
        var idx = preset == PresetEnum.User1 ? 1 : 2;
        var nameForm = new SimpleInputForm($"输入预设 #{idx} 名称：", ExpressionTextBox.SupportedExpressionTypeEnum.String);
        string name;
        do
        {
            name = nameForm.GetInput();
            if (name == null) return;
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("预设名称不能为空。", $"输入预设 #{idx} 名称：", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
        while (string.IsNullOrWhiteSpace(name));
        cfgForm.SaveToPreset(idx, name);
        MessageBox.Show($"已保存自定义预设 #{idx}：{name}。", Info.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static string GetStratData(GameConfigForm cfgForm)
    {
        var stratNames = new HashSet<string>
        {
            "半场is1A2", "P1雾龙线", "P1线Prio", "P1四线固定", "P1线is南北", "P1塔",
            "P2DD冰is双逆", "P2光暴",
            "P3一运", "P3二运", "P3二运分半场",
            "P4一运", "P4一运沙漏is主斜换", "P4一运方形is左换",
            "P4二运pIdx2prio", "P4二运击退", "P4二运吃圈", "P4二运点名1", "P4二运点名2", "P4二运点名3", "P4二运点名4",
            "P5挡枪"
        };
        cfgForm.SaveToPreset(0, ""); // 以储存临时配置的形式导出
        var export = ScriptHelper.GetDictVariable(true, $"{Info.ConfigName}0").Values
                                 .Where(p => stratNames.Contains(p.Key))
                                 .ToDictionary(p => p.Key, p => p.Value.ToString());
        ScriptHelper.SetDictVariable(true, $"{Info.ConfigName}0", null); // 删除临时配置
        return string.Join("|", export.Select(pair => $"{pair.Key}={pair.Value}"));
    }

    private static void ImportExportStrats(GameConfigForm cfgForm)
    {
        var currentData = GetStratData(cfgForm);
        var stratForm = new SimpleInputForm($"导出：全选复制； 导入：粘贴确认", ExpressionTextBox.SupportedExpressionTypeEnum.String, currentData);
        var importData = stratForm.GetInput();
        if (importData == null) return;
        var result = MessageBox.Show($"是否导入文本框中的攻略预设？\n\n如果你只是在导出预设，直接关闭窗口即可。", Info.Name, MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
        if (result != DialogResult.OK) return;
        ApplyPreset(cfgForm, importData);
        MessageBox.Show($"已导入粘贴的攻略预设。", Info.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void ApplyPreset(GameConfigForm cfgForm, string cfgData)
    {
        var cfgDict = cfgData.Split('|')
                             .Select(part => part.Split(new char[] { '=' }, 2))
                             .Where(split => split.Length == 2)
                             .ToDictionary(split => split[0], split => split[1]);
        cfgForm.ApplyPreset(cfgDict);
    }

    [STAThread]
    public static void Start()
    {
        try
        {
            Application.OpenForms.OfType<GameConfigForm>().ToList().ForEach(f => f.Close());
            Thread staThread = new Thread(new ThreadStart(RunConfigForm));
            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();
        }
        catch (Exception ex)
        {
            MessageBox.Show("用户配置表单运行错误：\n" + ex, Info.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

public static class U7aCactbotHelper
{
    public static JToken CallOverlayHandler(JObject o) => (JToken)ModuleEvents.CallOverlayHandler(o);

    public static void DisableTts()
    {
        bool changed = false;
        JObject config = null;
        try
        {
            config = (JObject)CallOverlayHandler(new JObject
            {
                ["call"] = "cactbotLoadData",
                ["overlay"] = "options"
            });
            if (config == null) return;
        }
        catch
        {
            return;
        }
        try
        {
            JObject fruTriggerSet = config
                                    .GetOrCreate("data")
                                    .GetOrCreate("raidboss")
                                    .GetOrCreate("triggerSets")
                                    .GetOrCreate("FuturesRewrittenUltimate");
            var ttsSetting = fruTriggerSet.Value<string>("Output");
            string newTtsSetting = null;
            switch (ttsSetting)
            {
                case "ttsOnly":
                    newTtsSetting = "disabled";
                    break;
                case null:
                case "default":
                case "ttsAndText":
                    newTtsSetting = "textOnly";
                    break;
                default:
                    break;
            }
            if (newTtsSetting != null)
            {
                fruTriggerSet["Output"] = newTtsSetting;
                CallOverlayHandler(new JObject
                {
                    ["call"] = "cactbotSaveData",
                    ["overlay"] = "options",
                    ["data"] = config["data"]
                });
                CallOverlayHandler(new JObject
                {
                    ["call"] = "cactbotReloadOverlays",
                });

                MessageBox.Show(
                    "检测到 Cactbot 官库中绝伊甸副本 TTS 未关闭。\n" +
                    "已自动关闭相应分组的 TTS 播报：\n" +
                    "  更改前：" + (ttsSetting ?? "(null)") + "\n" +
                    "  更改后：" + newTtsSetting,
                    U7aEntry.Info.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "尝试关闭 Cactbot 官库中绝伊甸副本 TTS 时遇到问题。\n" +
                "你需要手动关闭 Cactbot 的 TTS。错误详情：\n" +
                ex.ToString(),
                U7aEntry.Info.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}

public class Script_91145FA6785DC67890CAA64093C48486A6A050C36D6646CDFA0D921CD7E69DD3 : Triggernometry.ITrnNamedCallback
{
    public void Load()
    {
        RealPlugin.Instance.RegisterNamedCallback(U7aEntry.Info.ConfigName, new Action<object, string>((_, __) => U7aEntry.Start()), null);
    }
}

static class Script_91145FA6785DC67890CAA64093C48486A6A050C36D6646CDFA0D921CD7E69DD3_Functions
{
    internal static JObject GetOrCreate(this JObject parent, string property)
    {
        if (parent == null)
            throw new ArgumentNullException(nameof(parent));
        if (parent[property] is JObject child)
            return child;
        child = new JObject();
        parent[property] = child;
        return child;
    }
}
