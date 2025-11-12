namespace HsBgAI.Firestone
{
    public class CompsStrategy
    {
        public string compId;

        public string name;

        public List<CardData> cards;

        public string difficulty;

        public string powerLevel;

        public List<object> tips;
    }

    public class CardData
    {
        public string cardId;
        public string name;
        public List<object> tips;
        public string status;
    }
}