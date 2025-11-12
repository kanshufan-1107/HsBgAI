namespace HsBgAI.Firestone
{
    public class CardStatsData
    {
        public List<CardStat> cardStats;

        public int? dataPoints;

        public string timePeriod;

        public string lastUpdate;
    }

    public class CardStat
    {
        public string cardId;
        public int? totalPlayed;
        public double? averagePlacement;
        public List<AveragePlacementAtMmr> averagePlacementAtMmr;
        public List<TurnStat> turnStats;
    }

    public class AveragePlacementAtMmr
    {
        public int? mmr;
        public double? placement;
    }

    public class TurnStat
    {
        public int? turn;
        public int? totalPlayedAtTurn;
        public double? averagePlacement;
        public List<object> averagePlacementAtMmr;
    }
}