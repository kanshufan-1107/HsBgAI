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
                if (HsBgAI.xpChangedFirstSave)
                {
                    HsBgAI.xpChangedFirstSave = false;
                    LogPrintUtils.WriteMatchLog($"MatchInfo={HsBgAI.gameUUID}|{MatchInfo}");
                }
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
                        if (HsBgAI.endGameScreenFirstSave)
                        {
                            HsBgAI.endGameScreenFirstSave = false;
                            LogPrintUtils.WriteMatchLog($"EndGameScreen={HsBgAI.gameUUID}|{DateTime.Now:yyyy-MM-dd HH:mm:ss},{gameRank},{finalResult},{gameType}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogPrintUtils.Exception("HsBgAI.Patchers.RewardXpPatcher.PatchShowEndGameScreen".PadRight(40), ex);
            }
        }
    }
}