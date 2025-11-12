namespace HsBgAI.Firestone
{
    public class HeroStatsData
    {
        public List<HeroStats> heroStats;

        public string lastUpdate;
    }

    public class HeroStats
    {
        public string heroCardId;

        public int dataPoints;

        public int totalOffered;

        public int totalPicked;

        public float? averagePosition;

        public float? averagePositionByTribe;

        public float? standardDeviation;

        public float? standardDeviationOfTheMean;

        public float? conservativePositionEstimate;

        public List<PlacementDistribution> placementDistribution;

        public List<WarbandStats> warbandStats;

        public List<CombatWinrate> combatWinrate;

        public List<TribeStats> tribeStats;

        public List<object> anomalyStats;

        public int mmrPercentile;

        public string timePeriod;
        
        public HeroStats CreateNewHeroStats()
        {
            return new HeroStats
            {
                heroCardId = heroCardId,
                dataPoints = dataPoints,
                totalOffered = totalOffered,
                totalPicked = totalPicked,
                averagePosition = averagePosition,
                standardDeviation = standardDeviation,
                standardDeviationOfTheMean = standardDeviationOfTheMean,
                conservativePositionEstimate = conservativePositionEstimate,
                placementDistribution = placementDistribution,
                warbandStats = warbandStats,
                combatWinrate = combatWinrate,
                tribeStats = tribeStats,
                anomalyStats = anomalyStats,
                mmrPercentile = mmrPercentile,
                timePeriod = timePeriod
            };
        }
    }

    /// <summary>
    /// 排名以及分布
    /// </summary>
    public class PlacementDistribution
    {
        public int rank;
        public float? percentage;
    }

    /// <summary>
    /// 战队统计
    /// </summary>
    public class WarbandStats
    {
        public int turn;
        public float? averageStats;
    }

    /// <summary>
    /// 战斗胜率
    /// </summary>
    public class CombatWinrate
    {
        public int turn;
        public float? winrate;
    }

    /// <summary>
    /// 种族统计
    /// </summary>
    public class TribeStats
    {
        public int tribe;
        public int dataPoints;
        public int dataPointsOnMissingTribe;
        public int totalOffered;
        public int totalPicked;
        public float? averagePosition;
        public float? averagePositionWithoutTribe;
        public float? refAveragePosition;
        public float? impactAveragePosition;
        public float? impactAveragePositionVsMissingTribe;
    }
}