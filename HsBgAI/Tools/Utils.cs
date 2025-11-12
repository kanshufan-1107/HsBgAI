using System.Diagnostics;
using BepInEx.Logging;
using Hearthstone.Progression;
using HsBgAI.Enums;
using HsBgAI.Mapping;
using Newtonsoft.Json;
using static Assets.Global;

namespace HsBgAI.Tools
{
    public class Utils
    {
        /// <summary>
        /// 在主程序日志文件中打印本插件日志
        /// </summary>
        /// <param name="level"></param>
        /// <param name="message"></param>
        public static void MyLogger(LogLevel level, object message)
        {
            var myLogSource = BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_GUID + ".MyLogger");
            myLogSource.Log(level, message);
            BepInEx.Logging.Logger.Sources.Remove(myLogSource);
        }

        /// <summary>
        /// 获取调用方法的名称
        /// </summary>
        /// <returns></returns>
        public static string GetCallingMethod()
        {
            var stackTrace = new StackTrace();
            var frame = stackTrace.GetFrame(2);
            if (frame == null)
            {
                return "UnknownMethod".PadRight(40);
            }

            var method = frame.GetMethod();
            if (method == null || method.DeclaringType == null)
            {
                return "UnknownMethod".PadRight(40);
            }

            var str = method.DeclaringType.FullName;
            var callingMethod = $"{str}.{method.Name}";
            return callingMethod.Length > 40 ? callingMethod.Substring(0, 40) : callingMethod.PadRight(40);
        }

        /// <summary>
        /// 保存监控信息
        /// </summary>
        /// <param name="newObj"></param>
        public static void SaveJsonData(GameInfo newObj)
        {
            GameSaveDataManager.Get().GetSubkeyValue(GameSaveKeyId.BACON, GameSaveKeySubkeyId.BACON_HEROES_PICKED_COUNT, out List<long> HeroesPickedCount);
            GameSaveDataManager.Get().GetSubkeyValue(GameSaveKeyId.BACON, GameSaveKeySubkeyId.BACON_TOP_4_FINISHES, out long Top4Finishes);
            int top4Finishes = (int)Top4Finishes - 1;
            int rating = BaconLobbyMgr.Get()?.GetBattlegroundsActiveGameModeRating() ?? 0;
            newObj.BattlegroundsInfo = $"{rating} {HeroesPickedCount.Sum()}场 前四: {top4Finishes}";
            if (!Directory.Exists(HsBgAI.pluginMainPath)) Directory.CreateDirectory(HsBgAI.pluginMainPath);
            var jsonFile = Path.Combine(HsBgAI.pluginMainPath, $"Monitor{HsBgAI.monitorSuffix}.json");
            if (File.Exists(jsonFile)) File.Delete(jsonFile);
            LogPrintUtils.Info(GetCallingMethod(), $"保存信息到Monitor{HsBgAI.monitorSuffix}.json");
            string json = JsonConvert.SerializeObject(newObj, Formatting.Indented);
            File.WriteAllText(jsonFile, json);
        }

        /// <summary>
        /// 保存错误监控信息
        /// </summary>
        public static void SaveErrorJsonData()
        {
            if (!Directory.Exists(HsBgAI.pluginMainPath)) Directory.CreateDirectory(HsBgAI.pluginMainPath);
            var jsonFile = Path.Combine(HsBgAI.pluginMainPath, $"Monitor{HsBgAI.monitorSuffix}.json");
            GameInfo gameInfo;
            if (File.Exists(jsonFile))
            {
                string jsonStr = File.ReadAllText(jsonFile);
                gameInfo = JsonConvert.DeserializeObject<GameInfo>(jsonStr) ?? new GameInfo();
                gameInfo.UpdateRequired = HsBgAI.updateRequired;
                gameInfo.TemporarilyClosed = HsBgAI.temporarilyClosed;
                File.Delete(jsonFile);
            }
            else
            {
                gameInfo = new GameInfo()
                {
                    UpdateRequired = HsBgAI.updateRequired,
                    TemporarilyClosed = HsBgAI.temporarilyClosed
                };
            }
            LogPrintUtils.Info(GetCallingMethod(), $"保存异常信息到Monitor{HsBgAI.monitorSuffix}.json");
            string json = JsonConvert.SerializeObject(gameInfo, Formatting.Indented);
            File.WriteAllText(jsonFile, json);
        }

        /// <summary>
        /// 转化战旗状态为中文
        /// </summary>
        /// <param name="battleGroundState"></param>
        /// <returns></returns>
        public static string TranslateBattleGroundState(GameBattleState battleGroundState)
        {
            switch (battleGroundState)
            {
                case GameBattleState.INVALID:
                    goto default;
                case GameBattleState.FINDING_GAME:
                    return "寻找游戏";
                case GameBattleState.CHOOSE_HERO:
                    return "选择英雄";
                case GameBattleState.WAITING_FOR_OWN_TURN:
                    return "等待我方回合";
                case GameBattleState.OWN_TURN:
                    return "我方回合";
                case GameBattleState.BATTLE_TURN:
                    return "对战回合";
                case GameBattleState.GAME_OVER:
                    return "游戏结束";
                case GameBattleState.FATAL_ERROR:
                    return "游戏错误";
                default:
                    return "无效";
            }
        }

        /// <summary>
        /// 计算通行证经验
        /// </summary>
        /// <param name="preLevel"></param>
        /// <param name="preXp"></param>
        /// <param name="curLevel"></param>
        /// <param name="curXp"></param>
        /// <param name="rewardTrackType"></param>
        /// <returns></returns>
        public static int CalculateIntroXpGained(int preLevel, int preXp, int curLevel, int curXp, RewardTrackType rewardTrackType)
        {
            RewardTrack rewardTrack = RewardTrackManager.Get().GetRewardTrack(rewardTrackType);
            bool flag = preLevel < curLevel;
            int xpNeeded = rewardTrack.GetRewardTrackLevelRecord(preLevel).XpNeeded;
            int num = (flag ? xpNeeded : curXp) - preXp;
            if (curLevel > preLevel + 1)
            {
                for (int i = preLevel + 1; i < curLevel; i++)
                {
                    int num2 = num;
                    int? num3;
                    if (rewardTrack == null) num3 = null;
                    else
                    {
                        RewardTrackLevelDbfRecord rewardTrackLevelRecord = rewardTrack.GetRewardTrackLevelRecord(i);
                        num3 = (rewardTrackLevelRecord != null) ? new int?(rewardTrackLevelRecord.XpNeeded) : null;
                    }
                    int? num4 = num3;
                    num = num2 + num4.GetValueOrDefault();
                }
            }
            if (flag) num += curXp;
            return num;
        }

        /// <summary>
        /// 退出游戏
        /// </summary>
        /// <param name="exitCode"></param>
        public static void Quit(int exitCode = 0)
        {
            try
            {
                UnityEngine.Application.Quit(exitCode);
            }
            finally
            {
                Thread.Sleep(11451);
                Process.GetCurrentProcess().Kill();
            }
        }

        /// <summary>
        /// 生成UUID
        /// </summary>
        /// <returns></returns>
        public static string GenerateJavaCompatibleUuid()
        {
            Guid guid = Guid.NewGuid();
            return guid.ToString().ToUpper().Replace("-", "");
        }
    }
}