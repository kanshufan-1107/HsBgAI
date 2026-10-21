namespace HsBgAI
{
    public class PluginInfo
    {
        public const string PARENT_PLUGIN_GUID = "HsAI";
        public const string PLUGIN_GUID = "HsBgAI";
        public const string PLUGIN_NAME = "HsBgAI";
        public const string PLUGIN_AUTHOR = "kanshufan";
        public const string PLUGIN_VERSION = "1.0.0.0";

        // 以下三项由 GameVersionService 在每天 05:00 抓取官网后刷新（改为静态字段以支持运行时赋值），
        // 这里的初始值仅在从未成功抓取过时作为回退使用。
        /// <summary>
        /// 当前版本开始时间（本地时间 yyyy-MM-dd），即当前资料片正式上线日期
        /// </summary>
        public static string GAME_VERSION_START_TIME = "2026-10-21";
        /// <summary>
        /// 当前版本结束时间（本地时间 yyyy-MM-dd），即下个资料片正式上线日期
        /// </summary>
        public static string GAME_VERSION_END_TIME = "2027-03-18";
        /// <summary>
        /// 当前版本周期文本，格式 yyyy.MM.dd ~ yyyy.MM.dd
        /// </summary>
        public static string CURRENT_VERSION_DURATION_STR = "2026.10.21 ~ 2027.03.18";
    }
}