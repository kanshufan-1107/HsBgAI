using BepInEx;
using HsFirestone.Models;
using Newtonsoft.Json;

namespace HsBgAI.Firestone
{
    public class FirestoneClient
    {
        public HeroStatsData? heroStatsData;
        public HeroStrategies? heroStrategies;
        public List<CompsStrategy>? compsStrategies;
        public CardStatsData? cardStatsData;

        private static string strategyDataPath = Path.Combine(Paths.BepInExRootPath, PluginInfo.PARENT_PLUGIN_GUID, "StrategyData");
        private string heroStatsDataPath = Path.Combine(strategyDataPath, "HeroStats.json");
        private string heroStrategiesPath = Path.Combine(strategyDataPath, "HeroStrategies.json");
        private string compsStrategiesPath = Path.Combine(strategyDataPath, "CompsStrategies.json");
        private string cardStatsDataPath = Path.Combine(strategyDataPath, "CardStats.json");
        private static FirestoneClient instance;
        /// <summary>
        /// cardId → CardStat 的快速查找缓存，数据加载后一次性构建
        /// </summary>
        private Dictionary<string, CardStat> _cardStatDict;

        private FirestoneClient()
        {
            // 创建策略文件夹
            if (!Directory.Exists(strategyDataPath)) Directory.CreateDirectory(strategyDataPath);

            // 获取本地缓存数据
            heroStatsData = LoadLocal<HeroStatsData>(heroStatsDataPath);
            compsStrategies = LoadLocal<List<CompsStrategy>>(compsStrategiesPath);
            heroStrategies = LoadLocal<HeroStrategies>(heroStrategiesPath);

            cardStatsData = LoadLocal<CardStatsData>(cardStatsDataPath);
            if (cardStatsData != null)
            {
                // 一次性构建查找字典，避免每次 GetBestCard 时重建
                _cardStatDict = cardStatsData.cardStats
                    .Where(c => !string.IsNullOrEmpty(c.cardId))
                    .ToDictionary(c => c.cardId, c => c);
            }
        }

        /// <summary>
        /// 读取并反序列化本地 JSON 文件；文件不存在或解析失败时返回 null
        /// </summary>
        private static T? LoadLocal<T>(string path) where T : class
        {
            if (!File.Exists(path)) return null;
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
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
            if (_cardStatDict == null)
                return choices.FirstOrDefault();

            Entity bestEntity = null;
            double? bestPlacement = double.MaxValue;

            foreach (var entity in choices)
            {
                var cardId = entity.GetCardId();
                if (string.IsNullOrEmpty(cardId)) continue;

                // 使用预缓存字典查找
                if (_cardStatDict.TryGetValue(cardId, out var stat))
                {
                    var placement = stat.averagePlacement;
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
            if (heroStrategies == null || currLevel > 6) return false;

            var basicCurve = heroStrategies.curves?.Find(x => x.id == "basic");
            if (basicCurve == null) return false;

            var step = basicCurve.steps?.Find(x => x.turn == currTurn);
            if (step?.actions == null || step.actions.Count == 0) return false;

            return step.actions.Any(a => a.type == "level" && a.param == currLevel + 1);
        }
    }
}