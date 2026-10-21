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
using HsBgAI.Helpers;
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
        private readonly HelpOverlay _helpOverlay = new HelpOverlay();

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
            // 每天 05:00 抓取官网刷新版本起止时间（PluginInfo.GAME_VERSION_*）
            GameVersionService.Instance.Start();
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
                var text = $"{PluginInfo.PLUGIN_NAME} 快捷键帮助\n" +
                                "───────────────────────\n" +
                                "F9 : 启动/停止战旗AI\n" +
                                "F10 : 显示帮助信息\n" +
                                (enableClaimReward.Value ? "F11 : 领取成就奖励\n" : "") +
                                (enableClaimReward.Value ? "F12 : 领取战令奖励\n" : "") +
                                "───────────────────────\n" +
                                $"插件状态: {(enablePlugin.Value ? "运行中" : "已停止")}\n" +
                                $"当前补丁数: {allPatchNum}\n" +
                                $"AI状态: {(MainProcess.Instance.Running ? "运行中" : "已停止")}";

                _helpOverlay.Show(text);
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

            _helpOverlay.Update();

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
            GameVersionService.Instance.Stop();
            LogPrintUtils.Info($"{PluginInfo.PLUGIN_GUID}.OnDestroy".PadRight(40), $"插件 {PluginInfo.PLUGIN_NAME} 卸载成功......");
            PatchManager.UnPatchAll();
        }

        private void OnGUI()
        {
            _helpOverlay.OnGUI();
        }
    }
}