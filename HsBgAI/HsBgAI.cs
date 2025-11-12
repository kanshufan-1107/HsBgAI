using System.Diagnostics;
using BepInEx;
using BepInEx.Configuration;
using Hearthstone.DataModels;
using Hearthstone.Progression;
using Hearthstone.UI;
using HsBgAI.Ai;
using HsBgAI.Patchers;
using HsBgAI.Tools;
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
        public static string gameUUID = "";
        private static bool isRunning = true;

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
                UIStatus.Get().AddInfo($"[{allPatchNum}]" + (enablePlugin.Value ? $"{PluginInfo.PLUGIN_NAME}插件状态：运行" : $"{PluginInfo.PLUGIN_NAME}插件状态：停止"));
            }
            else if (Input.GetKeyUp(KeyCode.F11))
            {
                LogPrintUtils.Info(Utils.GetCallingMethod(), "领取成就...");
                DataModelList<AchievementDataModel> dataModelList = AchievementManager.Get().GetRecentlyCompletedAchievements().GetCurrentSortedAchievements().SortByStatusThenClaimedDate().ToDataModelList<AchievementDataModel>();
                var completedList = dataModelList.Where(achievement => achievement.Status == AchievementManager.AchievementStatus.COMPLETED).ToList();
                foreach (AchievementDataModel achievementDataModel in completedList)
                {
                    var chooseOneRewardItemId = 0;
                    RewardListDataModel rewardListDataModel = achievementDataModel.RewardList;
                    if (rewardListDataModel != null && rewardListDataModel.ChooseOne)
                    {
                        foreach (RewardItemDataModel rewardItemDataModel in rewardListDataModel.Items)
                        {
                            RewardItemDbfRecord rewardItemDbfRecord = GameDbf.RewardItem.GetRecord(rewardItemDataModel.AssetId);
                            var isClaimed = rewardItemDataModel.Card?.Owned ?? false;
                            LogPrintUtils.Info(Utils.GetCallingMethod(), $"[领取成就] 多选一 => assetid:{rewardItemDataModel.AssetId} type:{rewardItemDbfRecord.RewardType} name:{rewardItemDbfRecord.CardRecord?.Name.GetString()} {(isClaimed ? "已领取" : "未领取")}");
                            if (!isClaimed) chooseOneRewardItemId = rewardItemDataModel.AssetId;
                        }
                    }
                    AchievementManager.Get().ClaimAchievementReward(achievementDataModel.ID, chooseOneRewardItemId);
                    LogPrintUtils.Info(Utils.GetCallingMethod(), $"[领取成就] {achievementDataModel.ID}:{achievementDataModel.Name} 选择:{chooseOneRewardItemId}");
                }
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
    }
}