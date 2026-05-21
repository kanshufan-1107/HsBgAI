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
                foreach (var xpChange in sortedXpChanges)
                {
                    RewardTrackType rewardTrackType = (RewardTrackType)xpChange.RewardTrackType;
                    int introXpGained = Utils.CalculateIntroXpGained(xpChange.PrevLevel, xpChange.PrevXp, xpChange.CurrLevel, xpChange.CurrXp, rewardTrackType);
                    if (rewardTrackType == RewardTrackType.GLOBAL)
                    {
                        MainProcess.Instance.gameInfo.AllGetXp += introXpGained;
                        LogPrintUtils.Info(Utils.GetCallingMethod(), "通行证经验 + " + introXpGained + " 过去等级 " + xpChange.PrevLevel + " 过去经验 " + xpChange.PrevXp + " 当前等级 " + xpChange.CurrLevel + " 当前经验 " + xpChange.CurrXp);
                        string matchInfo = $"+{introXpGained},{xpChange.PrevLevel},{xpChange.PrevXp},{xpChange.CurrLevel},{xpChange.CurrXp}/{(xpChange.CurrLevel > 130 ? 1500 : GameInfoUtils.LevelExperienceThresholds[xpChange.CurrLevel - 1])}";
                        if (!HsBgAI.needConcede) LogPrintUtils.WriteMatchLog($"MatchInfo={HsBgAI.GameUid}|{matchInfo}");
                    }
                    else if (rewardTrackType == RewardTrackType.BATTLEGROUNDS)
                    {
                        LogPrintUtils.Info(Utils.GetCallingMethod(), "战棋通行证经验 + " + introXpGained);
                    }
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
                        int place = GameState.Get()?.GetFriendlySidePlayer()?.GetHero()?.GetRealTimePlayerLeaderboardPlace() ?? 0;
                        string gameRank = place >= 1 && place <= 8 ? $"第{place}名" : "未知";
                        string gameType = GameMgr.Get().GetGameType().ToString();
                        string finalResult = BaconLobbyMgr.Get()?.GetBattlegroundsActiveGameModeRating().ToString();
                        string gameStr = $"EndGameScreen={HsBgAI.GameUid}|{DateTime.Now:yyyy-MM-dd HH:mm:ss},{gameRank},{finalResult},{gameType}";
                        if (!HsBgAI.needConcede) LogPrintUtils.WriteMatchLog(gameStr);
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