using BepInEx;
using Newtonsoft.Json;

namespace HsBgAI.Firestone
{
    public class FirestoneClient
    {
        public HeroStatsData heroStatsData;
        public HeroStrategies heroStrategies;
        public List<CompsStrategy> compsStrategies;
        public CardStatsData cardStatsData;

        private static string strategyDataPath = Path.Combine(Paths.BepInExRootPath, PluginInfo.PARENT_PLUGIN_GUID, "StrategyData");
        private string heroStatsDataPath = Path.Combine(strategyDataPath, "HeroStats.json");
        private string heroStrategiesPath = Path.Combine(strategyDataPath, "HeroStrategies.json");
        private string compsStrategiesPath = Path.Combine(strategyDataPath, "CompsStrategies.json");
        private string cardStatsDataPath = Path.Combine(strategyDataPath, "CardStats.json");
        private static FirestoneClient instance;

        private FirestoneClient()
        {
            // 创建策略文件夹
            if (!Directory.Exists(strategyDataPath)) Directory.CreateDirectory(strategyDataPath);
            // 获取本地的英雄数据
            if (File.Exists(heroStatsDataPath))
            {
                var json = File.ReadAllText(heroStatsDataPath);
                var heroStatsObj = JsonConvert.DeserializeObject<HeroStatsData>(json);
                if (heroStatsObj != null)
                {
                    heroStatsData = heroStatsObj;
                }
            }
            // 获取本地的流派策略数据
            if (File.Exists(compsStrategiesPath))
            {
                var json = File.ReadAllText(compsStrategiesPath);
                var compsStrategiesArr = JsonConvert.DeserializeObject<List<CompsStrategy>>(json);
                if (compsStrategiesArr != null)
                {
                    compsStrategies = compsStrategiesArr;
                }
            }
            // 获取本地的英雄策略数据
            if (File.Exists(heroStrategiesPath))
            {
                var json = File.ReadAllText(heroStrategiesPath);
                var heroStrategiesObj = JsonConvert.DeserializeObject<HeroStrategies>(json);
                if (heroStrategiesObj != null)
                {
                    heroStrategies = heroStrategiesObj;
                }
            }
            // 获取本地的卡牌数据
            if (File.Exists(cardStatsDataPath))
            {
                var json = File.ReadAllText(cardStatsDataPath);
                var cardStatsObj = JsonConvert.DeserializeObject<CardStatsData>(json);
                if (cardStatsObj != null)
                {
                    cardStatsData = cardStatsObj;
                }
            }
        }

        public static FirestoneClient Instance
        {
            get
            {
                instance ??= new FirestoneClient();
                return instance;
            }
        }

        /// <summary>
        /// 获取英雄选择统计
        /// </summary>
        /// <param name="statsParams"></param>
        public List<HeroStats> GetHeroPickStats(BattlegroundsHeroPickStatsParams statsParams)
        {
            List<HeroStats> heroStats = new List<HeroStats>();
            if (heroStatsData != null)
            {
                // 根据 statsParams 的 heroCardIds 筛选 heroStatsData.heroStats.heroCardId
                var filteredHeroStats = heroStatsData.heroStats
                    .Where(h => statsParams.heroCardIds.Contains(h.heroCardId))
                    .Select(h =>
                        {
                            HeroStats hs = h.CreateNewHeroStats();
                            hs.tribeStats = [.. hs.tribeStats.Where(t => statsParams.battlegroundsRaces.Contains(t.tribe))];
                            hs.averagePositionByTribe = hs.tribeStats.Average(t => t.averagePosition);
                            return hs;
                        })
                    .ToList();
                heroStats.AddRange(filteredHeroStats);
            }
            return heroStats;
        }

        /// <summary>
        /// 获取选择卡牌的最合适的卡牌
        /// </summary>
        /// <param name="choices"></param>
        /// <returns></returns>
        public Entity GetBestCard(List<Entity> choices)
        {
            if (choices == null || choices.Count == 0)
                return null;

            // 创建cardId到CardStat的快速查找字典
            var cardStatDict = cardStatsData.cardStats
                .Where(c => !string.IsNullOrEmpty(c.cardId))
                .ToDictionary(c => c.cardId, c => c);

            Entity bestEntity = null;
            double? bestPlacement = double.MaxValue;

            foreach (var entity in choices)
            {
                var cardId = entity.GetCardId();
                if (string.IsNullOrEmpty(cardId)) continue;

                // 查找对应的统计信息
                if (cardStatDict.TryGetValue(cardId, out var stat))
                {
                    var placement = stat.averagePlacement;

                    // 比较并更新最佳选择
                    if (placement.HasValue && placement < bestPlacement)
                    {
                        bestPlacement = placement;
                        bestEntity = entity;
                    }
                }
            }

            return bestEntity ?? choices.FirstOrDefault(); // 保底返回第一个选项
        }

        /// <summary>
        /// 根据当前酒馆等级，判断当前回合是否是该英雄的最佳升本回合
        /// </summary>
        /// <param name="currTurn"></param>
        /// <param name="currLevel"></param>
        /// <returns></returns>
        public bool DetermineUpgradeRound(int currTurn, int currLevel)
        {
            if (heroStrategies != null && currLevel <= 6)
            {
                List<HeroCurve> heroCurves = heroStrategies.curves;
                if (heroCurves != null && heroCurves.Count > 0)
                {
                    HeroCurve fastBasic = heroCurves.Find(x => x.id == "basic");
                    if (fastBasic != null)
                    {
                        List<HeroCurveStep> heroCurveSteps = fastBasic.steps;
                        if (heroCurveSteps != null && heroCurveSteps.Count > 0)
                        {
                            HeroCurveStep step = heroCurveSteps.Find(x => x.turn == currTurn);
                            if (step != null && step.actions != null && step.actions.Count > 0)
                            {
                                List<HeroCurveActionExtended> upgradeActions = step.actions;
                                // 检查是否存在符合条件的升级动作（根据实际字段调整判断逻辑）
                                bool hasUpgrade = upgradeActions?.Any(a => a.type == "level" && a.param == currLevel + 1) ?? false;

                                return hasUpgrade;
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}