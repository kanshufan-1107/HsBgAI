namespace HsBgAI.Mapping
{
    public class GameInfo
    {
        public bool UpdateRequired = false;
        public bool TemporarilyClosed = false;
        public int Level = 0;
        public int Xp = 0;
        public int XpNeeded = 0;
        public int AllXp = 0;
        public int FullXpNeeded = 0;
        public int AllXpNeeded = 602200;
        public string FullTimeNeeded = "";
        public string Collection = "";
        public string PassportEnd = "";
        public string BattlegroundsInfo = "";
        public string StandardInfo = "";
        public string WildInfo = "";
        public string TwistInfo = "";

        private long _allRunTime = 0;
        private int _perHourXp = 0;
        private int _allGetXp = 0;
        public long AllRunTime
        {
            get => _allRunTime;
            set
            {
                _allRunTime = value;
                float t = value / 3600;
                if (t > 0) PerHourXp = (int)(AllGetXp / t);
                else PerHourXp = 0;
                if (PerHourXp <= 10) PerHourXp = 500;
                PerHourXpStr = $"{PerHourXp}/小时";
                AllRunTimeText = (value / 3600).ToString("F2");
            }
        }
        public string AllRunTimeText = "";
        public int PerHourXp
        {
            get => _perHourXp;
            set
            {
                _perHourXp = value;
                if (_perHourXp <= 10) _perHourXp = 500;
                PerHourXpStr = $"{_perHourXp}/小时";
            }
        }
        public string PerHourXpStr = "";
        public int AllGetXp
        {
            get => _allGetXp;
            set
            {
                _allGetXp = value;
                float t = (float)AllRunTime / 3600;
                if (t > 0) PerHourXp = (int)(_allGetXp / t);
                else PerHourXp = 0;
                if (PerHourXp <= 10) PerHourXp = 500;
            }
        }
    }
}