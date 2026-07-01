using System.Reflection;
using System.Text;
using Assets;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using Hearthstone.DataModels;
using Hearthstone.Progression;
using Hearthstone.UI;
using HsBgAI.Enums;
using HsBgAI.Firestone;
using HsFirestone.Models;
using PegasusShared;
using PegasusUtil;

namespace HsBgAI.Tools
{
    public class BotUtils
    {
        /// <summary>
        /// 判断当前是否投降
        /// </summary>
        /// <param name="score"></param>
        /// <param name="base_rank"></param>
        /// <returns></returns>
        public static ConcedeState CheckConcedeState(int score, out int base_rank)
        {
            // 基准分段的起点
            int baseSegment = score / 500 * 500;
            int upperLimit = baseSegment + 350;  // 当段基准+350
            int lowerLimit = baseSegment + 100;  // 基准值
            base_rank = baseSegment;
            if (score >= upperLimit)
            {
                return ConcedeState.START;  // 大于当段基准+350，开始投降
            }
            else if (score <= lowerLimit)
            {
                return ConcedeState.STOP;   // 低于基准，停止投降
            }
            else
            {
                return ConcedeState.CONTINUE;  // 其余情况保持状态
            }
        }

        /// <summary>
        /// 进入酒馆战旗
        /// </summary>
        public static void EnterBattleGrounds(bool running)
        {
            if (SceneMgr.Get().GetMode() != SceneMgr.Mode.HUB && SceneMgr.Get().GetMode() != SceneMgr.Mode.BACON)
            {
                LogPrintUtils.Info(Utils.GetCallingMethod(), "当前不在主页面");
                return;
            }
            if (GameMgr.Get().IsFindingGame())
            {
                LogPrintUtils.Info(Utils.GetCallingMethod(), "已在搜索游戏");
                return;
            }
            if (!running)
            {
                LogPrintUtils.Info(Utils.GetCallingMethod(), "用户主动停止插件运行...");
                return;
            }
            GameMgr.Get().FindGame(GameType.GT_BATTLEGROUNDS, FormatType.FT_WILD, 3459, 0, 0L, null, null, false, null, null, 0L, GameType.GT_UNKNOWN, 0);
            return;
        }

        /// <summary>
        /// 选择英雄
        /// </summary>
        /// <returns></returns>
        public static async UniTask<bool> ChooseHero()
        {
            var mulliganManager = MulliganManager.Get();
            if (mulliganManager == null)
                return false;
            var cards = mulliganManager.GetStartingCards();
            if (cards == null || cards.Count == 0)
                return false;
            LogPrintUtils.Info(Utils.GetCallingMethod(), $"英雄池【{string.Join(", ", cards.Select(card => card.GetEntity().GetName()))}】");
            LogPrintUtils.Info(Utils.GetCallingMethod(), $"种族池【{string.Join(", ", GameInfoUtils.ConvertRaceCN(GameState.Get().GetAvailableRacesInBattlegroundsExcludingAmalgam()))}】");
            string[] heroCardIdArr = [.. cards
                .Where(x => !x.GetEntity().HasTag(GAME_TAG.BACON_LOCKED_MULLIGAN_HERO))
                .Select(x => x.GetEntity().GetCardId())];
            var HeroPickStatsParams = new BattlegroundsHeroPickStatsParams
            {
                heroCardIds = [.. heroCardIdArr],
                battlegroundsRaces = [.. GameState.Get().GetAvailableRacesInBattlegroundsExcludingAmalgam().Select(x => (int)x)]
            };
            // 获取英雄选择数据
            var BattlegroundsHeroPickStats = FirestoneClient.Instance.GetHeroPickStats(HeroPickStatsParams);
            if (BattlegroundsHeroPickStats == null)
            {
                if (cards[1].GetEntity().IsHero())
                {
                    mulliganManager.ToggleHoldState(cards[1]);
                    var methodInfo = typeof(MulliganManager).GetMethod("OnMulliganButtonReleased", BindingFlags.NonPublic | BindingFlags.Instance);
                    methodInfo.Invoke(MulliganManager.Get(), [null]);
                    Traverse.Create(mulliganManager).Method("OnMulliganButtonReleased", new object[] { null }).GetValue();
                    LogPrintUtils.Info(Utils.GetCallingMethod(), $"英雄选择【{cards[1].GetEntity().GetName()}】");
                }
            }
            else
            {
                var list = new List<(Card card, float? AveragePosition, float? AveragePositionByTribe)>();
                foreach (var x in BattlegroundsHeroPickStats)
                {
                    var card = cards.Where(y => y.GetEntity().GetCardId() == x.heroCardId).FirstOrDefault();
                    LogPrintUtils.Info(Utils.GetCallingMethod(), $"英雄【{card.GetEntity().GetName()}】 平均排名【{x.averagePosition}】 当前种族平均排名【{x.averagePositionByTribe}】");
                    list.Add((card, x.averagePosition, x.conservativePositionEstimate));
                }
                var bestHero = list.OrderBy(x => x.AveragePosition).ThenBy(x => x.AveragePositionByTribe).FirstOrDefault();
                if (!bestHero.Equals(default((Card card, double AveragePosition, double AveragePositionByTribe))))
                {
                    mulliganManager.ToggleHoldState(bestHero.card);
                    await UniTask.Delay(3000);
                    var methodInfo = typeof(MulliganManager).GetMethod("OnMulliganButtonReleased", BindingFlags.NonPublic | BindingFlags.Instance);
                    methodInfo.Invoke(MulliganManager.Get(), [null]);
                    Traverse.Create(mulliganManager).Method("OnMulliganButtonReleased", new object[] { null }).GetValue();
                    LogPrintUtils.Info(Utils.GetCallingMethod(), $"英雄选择【{bestHero.card.GetEntity()?.GetName()}】");
                }
            }
            return false;
        }

        /// <summary>
        /// 选择卡牌
        /// </summary>
        /// <returns></returns>
        public static bool ChooseBattlegroundCard()
        {
            GameState gameState = GameState.Get();
            if (gameState == null || !gameState.IsInChoiceMode())
                return false;
            var entities = gameState.GetFriendlyEntityChoices()?.Entities?
                .Select(id => gameState.GetEntity(id))
                .Where(e => e != null) // 过滤无效实体
                .ToList() ?? new List<Entity>();
            if (entities.Count >= 1)
            {
                var entity = FirestoneClient.Instance.GetBestCard(entities);
                var nameList = entities.Select(entity => entity.GetName()).ToList();
                LogPrintUtils.Info(Utils.GetCallingMethod(), $"卡牌多选一 [{string.Join(",", nameList)}] => 选择【{entity.GetName()}】");
                gameState.AddChosenEntity(entity);
                gameState.SendChoices();
            }
            else LogPrintUtils.Info(Utils.GetCallingMethod(), "卡牌多选一不存在!");
            return true;
        }

        /// <summary>
        /// 卖出所有卡牌
        /// </summary>
        /// <returns></returns>
        public static bool SellAllCard()
        {
            GameState gameState = GameState.Get();
            if (gameState == null)
            {
                LogPrintUtils.Error(Utils.GetCallingMethod(), "GameState is null");
                return false;
            }
            List<Card> sellCards = GameInfoUtils.GetBuyOrSellCards(2);
            if (sellCards == null || sellCards.Count == 0)
                return false;
            if (GameInfoUtils.OwnMinions == null || GameInfoUtils.OwnHands == null)
                return false;
            var options = gameState.GetOptionsPacket()?.List;
            if (options == null) return false;

            bool anySold = false;
            foreach (Card card in sellCards)
            {
                if (TrySellCard(gameState, options, card))
                    anySold = true;
            }
            return anySold;
        }

        /// <summary>
        /// 升本
        /// </summary>
        /// <returns></returns>
        public static bool Upgrade()
        {
            GameState gameState = GameState.Get();
            if (gameState == null)
            {
                LogPrintUtils.Error(Utils.GetCallingMethod(), "GameState is null");
                return false;
            }
            var optionsPacket = gameState.GetOptionsPacket();
            if (optionsPacket == null) return false;
            var options = optionsPacket.List;
            if (options == null || options.Count == 0)
                return false;
            if (GameInfoUtils.UpgradeButtonCard == null) return false;
            bool canUpgrade = FirestoneClient.Instance.DetermineUpgradeRound(GameInfoUtils.BattleTurnCounter, GameInfoUtils.OwnTavernTier);
            if (!canUpgrade)
            {
                if (HsBgAI.upgradePrintTurn1 != GameInfoUtils.BattleTurnCounter)
                {
                    HsBgAI.upgradePrintTurn1 = GameInfoUtils.BattleTurnCounter;
                    LogPrintUtils.Info(Utils.GetCallingMethod(), $"当前第{GameInfoUtils.BattleTurnCounter}回合，不是{GameInfoUtils.OwnHero.GetName()}的最佳升{GameInfoUtils.OwnTavernTier + 1}本回合...");
                }
                return false;
            }
            if (HsBgAI.upgradePrintTurn2 != GameInfoUtils.BattleTurnCounter)
            {
                HsBgAI.upgradePrintTurn2 = GameInfoUtils.BattleTurnCounter;
                LogPrintUtils.Info(Utils.GetCallingMethod(), $"可用:{GameInfoUtils.OwnMana} 需求:{GameInfoUtils.UpgradeButtonCard.GetEntity().GetCost()} 可升本:{GameInfoUtils.UpgradeButtonCard.HasEnoughManaToPlay()}");
            }
            if (!GameInfoUtils.UpgradeButtonCard.HasEnoughManaToPlay())
                return false;
            int optionIndex = GameInfoUtils.GetOptionIndexFromCard(options, GameInfoUtils.UpgradeButtonCard);
            if (optionIndex == -1)
                return false;
            gameState.SetSelectedOption(optionIndex);
            gameState.SetSelectedSubOption(-1);
            gameState.SetSelectedOptionTarget(0);
            gameState.SetSelectedOptionPosition(0);
            gameState.SendOption();
            return true;
        }

        /// <summary>
        /// 购买卡牌
        /// </summary>
        /// <returns></returns>
        public static bool BuyCard()
        {
            GameState gameState = GameState.Get();
            if (gameState == null)
            {
                LogPrintUtils.Error(Utils.GetCallingMethod(), "GameState is null");
                return false;
            }
            List<Card> buyCards = GameInfoUtils.GetBuyOrSellCards(1);
            if (buyCards == null || buyCards.Count == 0)
                return false;
            var optionsPacket = gameState.GetOptionsPacket();
            if (optionsPacket == null) return false;
            var options = optionsPacket.List;
            foreach (Card card in buyCards)
            {
                int optionIndex = GameInfoUtils.GetOptionIndexFromCard(options, card);
                if (optionIndex < 0)
                    continue;
                int subOptionId = -1;
                if (!GameInfoUtils.PlayOption(options[optionIndex], out int targetEntityId, out subOptionId, cardsList =>
                {
                    var result_card = cardsList.FirstOrDefault();
                    return (result_card != null, result_card);
                }))
                {
                    continue;
                }
                var targetEntity = gameState.GetEntity(targetEntityId);
                LogPrintUtils.Info(Utils.GetCallingMethod(), $"购买卡牌【{targetEntity.GetName()}】");
                gameState.SetSelectedOption(optionIndex);
                gameState.SetSelectedSubOption(subOptionId);
                gameState.SetSelectedOptionTarget(targetEntityId);
                gameState.SetSelectedOptionPosition(0);
                gameState.SendOption();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 满场面卖随从
        /// </summary>
        /// <returns></returns>
        public static bool SellCardWhenFull()
        {
            GameState gameState = GameState.Get();
            if (gameState == null)
            {
                LogPrintUtils.Error(Utils.GetCallingMethod(), "GameState is null");
                return false;
            }
            List<Card> sellCards = GameInfoUtils.GetBuyOrSellCards(2);
            if (sellCards == null) return false;
            List<Card> ownMinions = GameInfoUtils.OwnMinions;
            List<Card> ownHands = GameInfoUtils.OwnHands;
            if (ownMinions == null || ownHands == null) return false;
            // 场上小于7个或者手上没有随从，不用卖随从
            if (ownMinions.Count < 7 || !ownHands.Any(x => x.GetEntity().IsMinion()))
                return false;
            var options = gameState.GetOptionsPacket()?.List;
            if (options == null) return false;

            foreach (Card card in sellCards)
            {
                if (TrySellCard(gameState, options, card))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 尝试卖出指定卡牌（卖身材最小的）
        /// </summary>
        /// <param name="gameState"></param>
        /// <param name="options"></param>
        /// <param name="card"></param>
        /// <returns></returns>
        private static bool TrySellCard(GameState gameState, List<Network.Options.Option> options, Card card)
        {
            int optionIndex = GameInfoUtils.GetOptionIndexFromCard(options, card);
            if (optionIndex < 0) return false;

            // 卖怪卖身材最小的，必须有目标
            if (!GameInfoUtils.PlayOption(options[optionIndex], out int targetEntityId, out int subOptionId, cardsList =>
            {
                cardsList = [.. cardsList.OrderBy(c => c.GetEntity().GetCurrentHealth() + c.GetEntity().GetATK())];
                var result = cardsList.FirstOrDefault();
                return (result != null, result);
            }))
            {
                return false;
            }

            var targetEntity = gameState.GetEntity(targetEntityId);
            LogPrintUtils.Info(Utils.GetCallingMethod(), $"卖出卡牌【{targetEntity.GetName()}】");
            LogPrintUtils.Info(Utils.GetCallingMethod(), $"剩余铸币：{GameInfoUtils.OwnMana}");
            gameState.SetSelectedOption(optionIndex);
            gameState.SetSelectedSubOption(subOptionId);
            gameState.SetSelectedOptionTarget(targetEntityId);
            gameState.SetSelectedOptionPosition(0);
            gameState.SendOption();
            return true;
        }

        /// <summary>
        /// 出牌
        /// </summary>
        /// <returns></returns>
        public static bool PlayCard()
        {
            GameState gameState = GameState.Get();
            if (gameState == null)
            {
                LogPrintUtils.Error(Utils.GetCallingMethod(), "GameState is null");
                return false;
            }
            List<Card> ownHands = GameInfoUtils.OwnHands;
            if (ownHands == null)
                return false;
            var optionsPacket = gameState.GetOptionsPacket();
            if (optionsPacket == null) return false;
            var options = optionsPacket.List;
            if (options == null || options.Count == 0) return false;
            var ownMinions = GameInfoUtils.OwnMinions;
            var enemyMinions = GameInfoUtils.EnemyMinions;
            if (ownMinions == null || enemyMinions == null)
                return false;
            foreach (Card ownHandCard in ownHands.Where(c => c != null))
            {
                var entity = ownHandCard.GetEntity();
                if (entity == null)
                {
                    LogPrintUtils.Error(Utils.GetCallingMethod(), $"发现空实体手牌: {ownHandCard}");
                    continue;
                }
                // 场面满了后先卖一个随从
                if (ownMinions.Count >= 7 && entity.IsMinion())
                {
                    LogPrintUtils.Info(Utils.GetCallingMethod(), "场面满了，先卖一个随从");
                    SellCardWhenFull();
                }

                int optionIndex = GameInfoUtils.GetOptionIndexFromCard(options, ownHandCard);
                if (optionIndex < 0)
                    continue;

                if (!GameInfoUtils.PlayOption(options[optionIndex], out int targetEntityId, out int subOptionId, cards =>
                {
                    var result = cards.FirstOrDefault();
                    return (true, result);
                }))
                {
                    continue;
                }
                var targetEntity = gameState.GetEntity(targetEntityId);
                var targetInfo = targetEntity != null ? $" 目标【{targetEntity.GetName()}】" : "";
                LogPrintUtils.Info(Utils.GetCallingMethod(), $"使用卡牌【{entity.GetName()}】{targetInfo}");
                gameState.SetSelectedOption(optionIndex);
                gameState.SetSelectedSubOption(subOptionId);
                gameState.SetSelectedOptionTarget(targetEntityId);
                gameState.SetSelectedOptionPosition(0);
                gameState.SendOption();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 刷新商店
        /// </summary>
        /// <returns></returns>
        public static bool Refresh()
        {
            GameState gameState = GameState.Get();
            if (gameState == null)
            {
                LogPrintUtils.Error(Utils.GetCallingMethod(), "GameState is null");
                return false;
            }
            var optionsPacket = gameState.GetOptionsPacket();
            if (optionsPacket == null) return false;
            var options = optionsPacket.List;
            if (options == null)
                return false;
            if (GameInfoUtils.RefreshButtonCard == null) return false;
            List<Card> enemyMinions = GameInfoUtils.EnemyMinions;
            if (enemyMinions == null)
                return false;
            // 商店还有怪，或者刷新完没有铸币了，不刷新
            if (enemyMinions.Count > 0 || GameInfoUtils.OwnMana - GameInfoUtils.RefreshButtonCard.GetEntity().GetCost() <= 0)
                return false;
            int optionIndex = GameInfoUtils.GetOptionIndexFromCard(options, GameInfoUtils.RefreshButtonCard);
            if (optionIndex < 0)
                return false;
            LogPrintUtils.Info(Utils.GetCallingMethod(), $"刷新商店...");
            gameState.SetSelectedOption(optionIndex);
            gameState.SetSelectedSubOption(-1);
            gameState.SetSelectedOptionTarget(0);
            gameState.SetSelectedOptionPosition(0);
            gameState.SendOption();
            return true;
        }

        /// <summary>
        /// 使用英雄技能
        /// </summary>
        /// <returns></returns>
        public static bool UseHeroPower()
        {
            GameState gameState = GameState.Get();
            if (gameState == null)
            {
                LogPrintUtils.Error(Utils.GetCallingMethod(), "GameState is null");
                return false;
            }
            var optionsPacket = gameState.GetOptionsPacket();
            if (optionsPacket == null) return false;
            var options = optionsPacket.List;
            if (options == null)
                return false;
            var ownHeroAbility = GameInfoUtils.OwnHeroAbility;
            if (ownHeroAbility == null)
                return false;
            int optionIndex = GameInfoUtils.GetOptionIndexFromCard(options, ownHeroAbility);
            if (optionIndex == -1)
                return false;
            if (ownHeroAbility.GetEntity().GetTag(GAME_TAG.EXHAUSTED) != 0 || GameInfoUtils.OwnMana < ownHeroAbility.GetEntity().GetRealTimeCost()) return false;
            int targetEntityId;
            int subOptionId;
            if (!GameInfoUtils.PlayOption(options[optionIndex], out targetEntityId, out subOptionId, cards =>
            {
                var result = GameInfoUtils.GetBestTargetByUseHeroPower(cards);
                return (true, result);
            }))
            {
                return false;
            }
            if (ownHeroAbility.GetEntity().GetCardId() == "BG20_HERO_301p" && options.Count < 2)
            {
                return false;
            }
            var targetEntity = gameState.GetEntity(targetEntityId);
            LogPrintUtils.Info(Utils.GetCallingMethod(), $"使用英雄技能【{ownHeroAbility.GetEntity().GetName()}】 目标 【{targetEntity?.GetName()}】");
            gameState.SetSelectedOption(optionIndex);
            gameState.SetSelectedSubOption(-1);
            gameState.SetSelectedOptionTarget(targetEntityId);
            gameState.SetSelectedOptionPosition(0);
            gameState.SendOption();
            return false;
        }

        /// <summary>
        /// 判断己方有能打出的卡牌
        /// </summary>
        /// <returns></returns>
        public static bool HandCardCanPlay()
        {
            GameState gameState = GameState.Get();
            if (gameState == null)
            {
                LogPrintUtils.Error(Utils.GetCallingMethod(), "GameState is null");
                return false;
            }
            var hands = GameInfoUtils.OwnHands;
            if (hands == null || hands.Count == 0) return false;
            foreach (var item in hands)
            {
                var entity = item.GetEntity();
                if (entity == null) continue;
                LogPrintUtils.Info(Utils.GetCallingMethod(), $"手牌 {entity.GetCardId()} {entity.GetName()}");
            }
            var optionsPacket = gameState.GetOptionsPacket();
            if (optionsPacket == null) return false;
            var options = optionsPacket.List;
            if (options == null)
                return false;
            int canPlayCount = 0;
            foreach (var ownHandCard in hands)
            {
                if (!GameInfoUtils.CardIsCanBeUsed(ownHandCard)) continue;
                int optionIndex = GameInfoUtils.GetOptionIndexFromCard(options, ownHandCard);
                if (optionIndex < 0)
                    continue;
                var option = options[optionIndex];
                // 如果有subOption
                if (option.Subs != null && option.Subs.Count != 0)
                {
                    for (int i = 0; i < option.Subs.Count; i++)
                    {
                        // 如果不需要目标，直接释放
                        if (option.Subs[i].Targets.Count == 0)
                            canPlayCount++;
                        foreach (var target in option.Subs[i].Targets)
                        {
                            if (option.Subs[i].IsValidTarget(target.ID))
                            {
                                canPlayCount++;
                            }
                        }
                    }
                }
            }
            return canPlayCount > 0;
        }

        /// <summary>
        /// 刷新未完成的任务
        /// </summary>
        public async static UniTask RefreshUnfinishedQuest()
        {
            LogPrintUtils.Info(Utils.GetCallingMethod(), $"刷新未完成的任务...");
            QuestManager quest = QuestManager.Get();
            Random random = new Random();
            if (quest != null)
            {
                // 每日任务
                QuestListDataModel questDay = quest.CreateActiveQuestsDataModel(QuestPool.QuestPoolType.DAILY, QuestPool.RewardTrackType.GLOBAL, true);

                // 筛选0进度每日任务
                List<QuestDataModel> questValidDay = new List<QuestDataModel>();
                foreach (QuestDataModel item in questDay.Quests)
                {
                    if (item == null) break;
                    if (item.QuestId > 0 && item.Progress == 0 && item.Quota > 0 && item.RerollCount > 0)
                    {
                        questValidDay.Add(item);
                    }
                }

                // 随机更换1个每日任务
                if (questValidDay.Count > 0)
                {
                    int idx = random.Next(questValidDay.Count);
                    QuestDataModel questRe = questValidDay[idx];
                    quest.RerollQuest(questRe.QuestId);
                    await UniTask.Delay(2000);
                    LogPrintUtils.Info(Utils.GetCallingMethod(), $"随机更换无法完成的每日任务{questRe.QuestId}：{questRe.Description}");
                }

                // 每周任务
                QuestListDataModel quesWeek = quest.CreateActiveQuestsDataModel(QuestPool.QuestPoolType.WEEKLY, QuestPool.RewardTrackType.GLOBAL, true);

                // 筛选0进度每周任务
                List<QuestDataModel> questValidWeek = new List<QuestDataModel>();
                foreach (QuestDataModel item in quesWeek.Quests)
                {
                    if (item == null) break;
                    if (item.QuestId > 0 && item.Progress == 0 && item.Quota > 0 && item.RerollCount > 0)
                    {
                        questValidWeek.Add(item);
                    }
                }

                // 随机更换1个每周任务
                if (questValidWeek.Count > 0)
                {
                    int idx = random.Next(questValidWeek.Count);
                    QuestDataModel questRe = questValidWeek[idx];
                    quest.RerollQuest(questRe.QuestId);
                    await UniTask.Delay(2000);
                    LogPrintUtils.Info(Utils.GetCallingMethod(), $"随机更换无法完成的每周任务{questRe.QuestId}：{questRe.Description}");
                }
            }
        }

        /// <summary>
        /// 领取成就
        /// </summary>
        public async static UniTask ClaimAchievementReward()
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
                await UniTask.Delay(2000);
            }
        }

        /// <summary>
        /// 领取战令奖励
        /// </summary>
        /// <returns></returns>
        public static async UniTask ClaimBattlePassReward()
        {
            LogPrintUtils.Info(Utils.GetCallingMethod(), "领取战令奖励...");
            foreach (Global.RewardTrackType rewardTrackType in Enum.GetValues(typeof(Global.RewardTrackType)))
            {
                var reward = RewardTrackManager.Get()?.GetRewardTrack(rewardTrackType);
                if (null != reward)
                {
                    // 付费免费
                    foreach (RewardTrackPaidType paidType in ProgressUtils.AllValidRewardTrackPaidType)
                    {
                        // 校验是否有付费免费授权
                        if (false == reward.HasOwnedRewardTrackPaidType(paidType))
                            continue;
                        // 每等级遍历
                        for (int i = 1; i <= reward.TrackDataModel.Level; ++i)
                        {
                            var levelReward = reward.GetRewardTrackLevelRecord(i);
                            RewardListDbfRecord rewardListRecord = ProgressUtils.GetRewardListRecord(levelReward, paidType);
                            ProgressUtils.GetRewardListAssetId(levelReward, paidType);
                            //如果本级没有奖励 过
                            if (null == rewardListRecord || rewardListRecord.RewardItems.Count <= 0 || rewardListRecord.Locked)
                                continue;

                            //如果领取过了 过
                            var m_rewardTrackLevelState = (PlayerState<PlayerRewardTrackLevelState>)Traverse.Create(reward).Field("m_rewardTrackLevelState").GetValue();
                            PlayerRewardTrackLevelState state = m_rewardTrackLevelState.GetState(levelReward.Level);
                            if (ProgressUtils.HasClaimedRewardTrackReward(ProgressUtils.GetRewardStatus(state, paidType)))
                            {
                                continue;
                            }
                            int chooseOneRewardItemId = 0;
                            RewardListDataModel rewardListDataModel = RewardUtils.CreateRewardListDataModelFromRewardListId(rewardListRecord.ID);
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
                            reward.ClaimReward(reward.RewardTrackId, levelReward.Level, paidType, chooseOneRewardItemId);
                            LogPrintUtils.Info(Utils.GetCallingMethod(), $"[奖励领取] 领取奖励 通行证[{rewardTrackType}] 付费类型[{paidType}] 等级{i} {rewardListRecord.Description.GetString()} {ProgressUtils.GetRewardStatus(m_rewardTrackLevelState.GetState(levelReward.Level), paidType)} 选择:{chooseOneRewardItemId}");
                            await UniTask.Delay(2000);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 获取当前段位信息
        /// </summary>
        /// <param name="visualsFormatType"></param>
        /// <returns></returns>
        public static string GetRankInfo(VisualsFormatType visualsFormatType)
        {
            string result = "未知";

            RankMgr rank = RankMgr.Get();
            if (rank != null)
            {
                MedalInfoTranslator translator = rank.GetLocalPlayerMedalInfo();
                TranslatedMedalInfo medalInfoData;
                switch (visualsFormatType)
                {
                    case VisualsFormatType.VFT_STANDARD:
                        medalInfoData = translator.GetCurrentMedal(FormatType.FT_STANDARD);
                        break;
                    case VisualsFormatType.VFT_WILD:
                        medalInfoData = translator.GetCurrentMedal(FormatType.FT_WILD);
                        break;
                    case VisualsFormatType.VFT_TWIST:
                        medalInfoData = translator.GetCurrentMedal(FormatType.FT_TWIST);
                        break;
                    default:
                        medalInfoData = translator.GetCurrentMedal(FormatType.FT_WILD);
                        break;
                }
                StringBuilder stringBuilder = new StringBuilder();
                string text = medalInfoData.GetRankName();
                string text2 = medalInfoData.IsLegendRank() ? medalInfoData.legendIndex + "名" : $" {medalInfoData.earnedStars}星";
                stringBuilder.Append($"{text}{text2} {medalInfoData.seasonWins}(W) {medalInfoData.seasonGames - medalInfoData.seasonWins}(L) {((medalInfoData.seasonGames > 0) ? $"{100f * (float)medalInfoData.seasonWins / (float)medalInfoData.seasonGames:0.00}%" : "0.00%")}");
                result = stringBuilder.ToString();
            }
            return result;
        }
    }
}