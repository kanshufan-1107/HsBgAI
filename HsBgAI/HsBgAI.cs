using System.Diagnostics;
using Assets;
using BepInEx;
using BepInEx.Configuration;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using Hearthstone.DataModels;
using Hearthstone.Progression;
using Hearthstone.UI;
using HsBgAI.Ai;
using HsBgAI.Mapping;
using HsBgAI.Patchers;
using HsBgAI.Tools;
using PegasusShared;
using PegasusUtil;
using UnityEngine;
using static HsBgAI.PluginConfig;

namespace HsBgAI
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class HsBgAI : BaseUnityPlugin
    {
        public static string hsUnitID = "";
        public static string monitorSuffix = "";
        public static string pluginMainPath = Path.Combine(Paths.BepInExRootPath, PluginInfo.PARENT_PLUGIN_GUID);
        public static readonly Stopwatch stopwatch = new Stopwatch();
        public bool hasBegan = false;
        public static bool loginCompleted = false;
        public static bool findingGameFirstSave = false;
        public static bool lazzyGameFirstPrint = false;
        public static int lazzyGameCount = 0;
        public static bool updateRequired = false;
        public static bool needConcede = false;
        public static bool matchFirstRecord = false;
        public static int gameStartConcedeTimes = 0;
        public static bool chooseHeroPrev = false;
        public static int upgradePrintTurn1 = 0;
        public static int upgradePrintTurn2 = 0;
        public static bool xpChangedFirstSave = false;
        public static bool endGameScreenFirstSave = false;
        public static bool temporarilyClosed = false;
        private static bool isRunning = true;
        private static bool showHelpMessage = false;
        private static float helpMessageStartTime = 0f;
        private static string helpMessageText = "";
        private Texture2D _cachedBgTex;
        private GUIStyle _cachedHelpStyle;

        /// <summary>
        /// 游戏信息
        /// </summary>
        public static Dictionary<string, string> gameInfo = new Dictionary<string, string>();

        /// <summary>
        /// 游戏唯一标识符
        /// </summary>
        public static string GameUid
        {
            get
            {
                if (!gameInfo.ContainsKey(hsUnitID))
                {
                    gameInfo[hsUnitID] = Utils.GenerateJavaCompatibleUuid();
                }
                return gameInfo[hsUnitID];
            }

            set => gameInfo[hsUnitID] = value;
        }

        /// <summary>
        /// 插件加载
        /// </summary>
        private void Awake()
        {
            if (ArguUtils.Instance.Exists("hsunitid"))
                hsUnitID = ArguUtils.Instance.Single("hsunitid");
            monitorSuffix = string.IsNullOrEmpty(hsUnitID) ? "" : $"-{hsUnitID}";
            if (hsUnitID.Length <= 0)
                ConfigBind(Config);
            else
            {
                var cfgPath = Path.Combine(Paths.ConfigPath, hsUnitID, PluginInfo.PLUGIN_GUID + ".cfg");
                ConfigBind(new ConfigFile(cfgPath, false, new BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)));
            }
            isRunning = enablePlugin.Value;
            if (!isRunning)
            {
                return;
            }
            LogPrintUtils.Info($"{PluginInfo.PLUGIN_GUID}.Awake".PadRight(40), $"插件 {PluginInfo.PLUGIN_NAME} 加载成功......");
            PatchManager.PatchAll();
        }

        /// <summary>
        /// 插件启动
        /// </summary>
        private void Start()
        {
            isRunning = enablePlugin.Value;
            if (!isRunning)
            {
                return;
            }
            LogPrintUtils.Info($"{PluginInfo.PLUGIN_GUID}.Start".PadRight(40), $"插件 {PluginInfo.PLUGIN_NAME} 启动成功......");
        }

        /// <summary>
        /// 游戏刷新
        /// </summary>
        private void Update()
        {
            if (Input.GetKeyUp(KeyCode.F9))
            {
                var mainProcess = MainProcess.Instance;
                if (mainProcess.Running)
                {
                    LogPrintUtils.Info($"{PluginInfo.PLUGIN_GUID}.Update".PadRight(40), "停止战旗AI...");
                    UIStatus.Get().AddInfo("停止战旗AI...", 5f * Time.timeScale);
                    stopwatch.Stop();
                    mainProcess.Running = false;
                    enableAutoRun.Value = false;
                }
                else
                {
                    LogPrintUtils.Info($"{PluginInfo.PLUGIN_GUID}.Update".PadRight(40), "启动战旗AI...");
                    UIStatus.Get().AddInfo("启动战旗AI...", 5f * Time.timeScale);
                    stopwatch.Restart();
                    mainProcess.Running = true;
                    mainProcess.Start().Forget();
                }
            }
            else if (Input.GetKeyUp(KeyCode.F10))
            {
                int allPatchNum = 0;
                foreach (var tempatch in PatchManager.AllHarmony)
                {
                    allPatchNum += tempatch.GetPatchedMethods().Count();
                }

                // 构建简单的帮助信息（去掉富文本标签）
                helpMessageText = $"{PluginInfo.PLUGIN_NAME} 快捷键帮助\n" +
                                "───────────────────────\n" +
                                "F9 : 启动/停止战旗AI\n" +
                                "F10 : 显示帮助信息\n" +
                                (enableClaimReward.Value ? "F11 : 领取成就奖励\n" : "") +
                                (enableClaimReward.Value ? "F12 : 领取战令奖励\n" : "") +
                                "───────────────────────\n" +
                                $"插件状态: {(enablePlugin.Value ? "运行中" : "已停止")}\n" +
                                $"当前补丁数: {allPatchNum}\n" +
                                $"AI状态: {(MainProcess.Instance.Running ? "运行中" : "已停止")}";

                // 设置显示标志和时间
                showHelpMessage = true;
                helpMessageStartTime = Time.unscaledTime;
            }
            else if (Input.GetKeyUp(KeyCode.F11) && enableClaimReward.Value)
            {
                BotUtils.ClaimAchievementReward().Forget();
                UIStatus.Get().AddInfo("领取成就奖励...", 5f);
            } 
            else if (Input.GetKeyUp(KeyCode.F12) && enableClaimReward.Value)
            {
                BotUtils.ClaimBattlePassReward().Forget();
                UIStatus.Get().AddInfo("领取战令奖励...", 5f);
            }

            // 检查是否超时
            if (showHelpMessage && Time.unscaledTime - helpMessageStartTime > 8f)
            {
                showHelpMessage = false;
            }

            isRunning = enablePlugin.Value;
            if (!isRunning)
            {
                return;
            }
            if (!loginCompleted && GameInfoUtils.IsPlaying)
            {
                loginCompleted = true;
            }
            if (GameMgr.Get() != null && loginCompleted && !hasBegan && enableAutoRun.Value)
            {
                LogPrintUtils.Info($"{PluginInfo.PLUGIN_GUID}.Update".PadRight(40), "开始启动AI...");
                UIStatus.Get().AddInfo("开始启动战旗AI!", 5f * Time.timeScale);
                hasBegan = true;
                stopwatch.Restart();
                var mainProcess = MainProcess.Instance;
                mainProcess.Running = true;
                mainProcess.Start().Forget();
            }
        }

        /// <summary>
        /// 插件卸载
        /// </summary>
        private void OnDestroy()
        {
            enableAutoRun.Value = false;
            LogPrintUtils.Info($"{PluginInfo.PLUGIN_GUID}.OnDestroy".PadRight(40), $"插件 {PluginInfo.PLUGIN_NAME} 卸载成功......");
            PatchManager.UnPatchAll();
        }

        private void OnGUI()
        {
            if (showHelpMessage)
            {
                // 缓存纹理和样式，避免每帧重建造成内存泄漏
                if (_cachedBgTex == null)
                    _cachedBgTex = MakeTex(2, 2, new Color(0.8f, 0.8f, 0.8f, 0.7f));

                if (_cachedHelpStyle == null)
                {
                    _cachedHelpStyle = new GUIStyle("box");
                    _cachedHelpStyle.fontSize = 16;
                    _cachedHelpStyle.alignment = TextAnchor.UpperLeft;
                    _cachedHelpStyle.wordWrap = true;
                    _cachedHelpStyle.padding = new RectOffset(10, 10, 10, 10);
                    _cachedHelpStyle.normal.textColor = Color.black;
                    _cachedHelpStyle.normal.background = _cachedBgTex;
                }

                GUIStyle helpStyle = _cachedHelpStyle;

                // 计算位置和大小
                float width = 300f;  // 稍微增加宽度以适应内容
                float padding = 20f;
                float x = Screen.width - width - padding;
                float y = padding;

                // 分割文本行以计算准确高度
                string[] lines = helpMessageText.Split('\n');

                // 根据行数计算高度
                float lineHeight = 20f;  // 行高稍微增加
                float titleHeight = 24f;  // 标题行高
                float separatorHeight = 16f;  // 分隔线行高

                // 计算总高度
                float height = 0f;
                foreach (string line in lines)
                {
                    if (line.Contains("快捷键帮助"))
                        height += titleHeight;
                    else if (line.Contains("──"))
                        height += separatorHeight;
                    else
                        height += lineHeight;
                }

                height += 20f;  // 添加一些额外空间

                // 绘制外部背景框
                GUI.Box(new Rect(x - 5, y - 5, width + 10, height + 10), "");

                // 绘制帮助文本
                GUI.Label(new Rect(x, y, width, height), helpMessageText, helpStyle);

                // 绘制倒计时
                float remainingTime = 8f - (Time.unscaledTime - helpMessageStartTime);
                GUI.Label(new Rect(x, y + height + 5, width, 20),
                    "自动隐藏: " + remainingTime.ToString("F1") + "秒",
                    new GUIStyle("label")
                    {
                        fontSize = 12,
                        alignment = TextAnchor.MiddleRight,
                        normal = { textColor = Color.yellow }  // 改为黄色更显眼
                    });
            }
        }

        /// <summary>
        /// 创建纹理的工具方法
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = col;
            }

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}