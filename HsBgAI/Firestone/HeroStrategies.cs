namespace HsBgAI.Firestone
{
    public class HeroStrategies
    {
        public List<HeroStratHero> heroes;

        public List<HeroCurve> curves;
    }

    public class HeroStratHero
    {
        public string cardId;

        public List<HeroStratTip> tips;
    }

    public class HeroStratTip
    {
        public string author;

        public string language;

        public string summary;

        public string date;

        public int patch;
    }

    public class HeroCurve
    {
        public string id;

        public string name;

        public string notes;

        public List<HeroCurveStep> steps;
    }

    public class HeroCurveStep
    {
        public int turn;

        public List<HeroCurveActionExtended> actions;
    }

    public class HeroCurveActionExtended
    {
        public string type;

        public int param;
    }
}