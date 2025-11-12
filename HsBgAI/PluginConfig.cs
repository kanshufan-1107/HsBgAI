using BepInEx.Configuration;

namespace HsBgAI
{
    public class PluginConfig
    {
        // 核心设置
        public static ConfigEntry<bool> enablePlugin;
        public static ConfigEntry<bool> enableAutoRun;
        public static ConfigEntry<bool> lazzyAfterTop4;
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
            battlegroundsLevelCap = config.Bind($"战旗配置", "BattlegroundsLevelCap", -1, "维持分段");
        }
    }
}