using HarmonyLib;
using Hearthstone.Progression;
using HsBgAI.Ai;
using HsBgAI.Tools;
using PegasusUtil;
using static Assets.Global;

namespace HsBgAI.Patchers
{
    public class RewardXpPatcher
    {
        /// <summary>
        /// 记录通行证经验
        /// </summary>
        /// <param name="__instance"></param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(RewardXpNotificationManager), "OnRewardTrackXpNotification")]
        public static void PatchOnRewardTrackXpNotification(RewardXpNotificationManager __instance)
        {
            if (MainProcess.Instance.Running)
            {
                List<RewardTrackXpChange> xpChanges = (List<RewardTrackXpChange>)Traverse.Create(__instance).Field("m_xpChanges").GetValue();
                var sortedXpChanges = xpChanges.OrderBy(x => x.CurrLevel)
                        .ThenBy(x => x.CurrXp)
                        .ToList();
                string MatchInfo = "";
                int index = 1;
                foreach (var xpChange in sortedXpChanges)
                {
                    RewardTrackType rewardTrackType = (RewardTrackType)xpChange.RewardTrackType;
                    int introXpGained = Utils.CalculateIntroXpGained(xpChange.PrevLevel, xpChange.PrevXp, xpChange.CurrLevel, xpChange.CurrXp, rewardTrackType);
                    if (rewardTrackType == RewardTrackType.GLOBAL)
                    {
                        MainProcess.Instance.gameInfo.AllGetXp += introXpGained;
                        LogPrintUtils.Info(Utils.GetCallingMethod(), "通行证经验 + " + introXpGained + " 过去等级 " + xpChange.PrevLevel + " 过去经验 " + xpChange.PrevXp + " 当前等级 " + xpChange.CurrLevel + " 当前经验 " + xpChange.CurrXp);
                        if (sortedXpChanges.Count == index)
                        {
                            MatchInfo += $"+{introXpGained},{xpChange.PrevLevel},{xpChange.PrevXp},{xpChange.CurrLevel},{xpChange.CurrXp}/{(xpChange.CurrLevel > 130 ? 1500 : GameInfoUtils.LevelExperienceThresholds[xpChange.CurrLevel - 1])}";
                        }
                    }
                    else if (rewardTrackType == RewardTrackType.BATTLEGROUNDS)
                    {
                        LogPrintUtils.Info(Utils.GetCallingMethod(), "战棋通行证经验 + " + introXpGained);
                    }
                    index++;
                }

                // 记录通行证经验信息
                if (!string.IsNullOrEmpty(MatchInfo))
                {
                    HsBgAI.xpMatchInfo = MatchInfo;
                    HsBgAI.hasXpInfo = true;
                }

                // 检查是否可以写入日志
                CheckAndWriteLog();
            }
        }

        /// <summary>
        /// 显示游戏结束界面
        /// </summary>
        /// <param name="playState"></param>
        /// <param name="enemyBlowUpSpell"></param>
        /// <param name="friendlyBlowUpSpell"></param>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameEntity), "ShowEndGameScreen")]
        public static void PatchShowEndGameScreen(ref TAG_PLAYSTATE playState, ref Spell enemyBlowUpSpell, ref Spell friendlyBlowUpSpell)
        {
            try
            {
                if (!GameMgr.Get().IsSpectator())
                {
                    if (GameMgr.Get().GetGameType() == PegasusShared.GameType.GT_BATTLEGROUNDS)
                    {
                        string gameRank = "未知";
                        switch (GameState.Get()?.GetFriendlySidePlayer()?.GetHero()?.GetRealTimePlayerLeaderboardPlace())
                        {
                            case 1: gameRank = "第一名"; break;
                            case 2: gameRank = "第二名"; break;
                            case 3: gameRank = "第三名"; break;
                            case 4: gameRank = "第四名"; break;
                            case 5: gameRank = "第五名"; break;
                            case 6: gameRank = "第六名"; break;
                            case 7: gameRank = "第七名"; break;
                            case 8: gameRank = "第八名"; break;
                            default: break;
                        }
                        string gameType = GameMgr.Get().GetGameType().ToString();
                        string finalResult = BaconLobbyMgr.Get()?.GetBattlegroundsActiveGameModeRating().ToString();

                        // 记录游戏结束信息
                        HsBgAI.gameEndInfo = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss},{gameRank},{finalResult},{gameType}";
                        HsBgAI.hasGameEndInfo = true;

                        // 检查是否可以写入日志
                        CheckAndWriteLog();
                    }
                }
            }
            catch (Exception ex)
            {
                LogPrintUtils.Exception("HsBgAI.Patchers.RewardXpPatcher.PatchShowEndGameScreen".PadRight(40), ex);
            }
        }

        /// <summary>
        /// 检查并写入日志
        /// </summary>
        private static void CheckAndWriteLog()
        {
            // 确保只写入一次
            if (HsBgAI.logWritten)
                return;

            // 只有在有游戏结束信息时才写入日志
            if (HsBgAI.hasGameEndInfo)
            {
                if (!string.IsNullOrEmpty(HsBgAI.xpMatchInfo))
                {
                    LogPrintUtils.WriteMatchLog($"MatchInfo={HsBgAI.gameUUID}|{HsBgAI.xpMatchInfo}");
                    LogPrintUtils.WriteMatchLog($"EndGameScreen={HsBgAI.gameUUID}|{HsBgAI.gameEndInfo}");
                }
                else
                {
                    LogPrintUtils.WriteMatchLog($"EndGameScreen={HsBgAI.gameUUID}|{HsBgAI.gameEndInfo}");
                }

                // 标记日志已写入
                HsBgAI.logWritten = true;

                // 清理状态
                HsBgAI.hasGameEndInfo = false;
                HsBgAI.hasXpInfo = false;
                HsBgAI.gameEndInfo = "";
                HsBgAI.xpMatchInfo = "";
            }
        }
    }
}