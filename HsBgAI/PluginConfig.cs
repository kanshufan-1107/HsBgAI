using BepInEx.Configuration;

namespace HsBgAI
{
    public class PluginConfig
    {
        /// <summary>
        /// 是否启用插件
        /// </summary>
        public static ConfigEntry<bool> enablePlugin;
        /// <summary>
        /// 是否启用自动运行
        /// </summary>
        public static ConfigEntry<bool> enableAutoRun;
        /// <summary>
        /// 前四摆烂
        /// </summary>
        public static ConfigEntry<bool> lazzyAfterTop4;
        /// <summary>
        /// 是否启用自动领取奖励
        /// </summary>
        public static ConfigEntry<bool> enableClaimReward;
        /// <summary>
        /// 维持分段，-1表示不维持
        /// </summary>
        public static ConfigEntry<int> battlegroundsLevelCap;

        /// <summary>
        /// 设置绑定
        /// </summary>
        /// <param name="config"></param>
        public static void ConfigBind(ConfigFile config)
        {
            enablePlugin = config.Bind($"{PluginInfo.PLUGIN_GUID}配置", "EnablePlugin", true, "是否启用插件");
            enableAutoRun = config.Bind($"{PluginInfo.PLUGIN_GUID}配置", "EnableAutoRun", false, "是否启用自动运行");
            lazzyAfterTop4 = config.Bind($"战旗配置", "LazzyAfterTop4", false, "前四摆烂");
            enableClaimReward = config.Bind($"{PluginInfo.PLUGIN_GUID}配置", "EnableClaimReward", false, "是否启用自动领取奖励");
            battlegroundsLevelCap = config.Bind($"战旗配置", "BattlegroundsLevelCap", -1, "维持分段");
        }
    }
}