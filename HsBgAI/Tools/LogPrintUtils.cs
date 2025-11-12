using System.Text;
using BepInEx;

namespace HsBgAI.Tools
{
    public class LogPrintUtils
    {
        /// <summary>
        /// 初始化日志文件路径
        /// </summary>
        public static string GameLogPath = Path.Combine(Paths.BepInExRootPath, PluginInfo.PARENT_PLUGIN_GUID, "GameLog", HsBgAI.hsUnitID);

        /// <summary>
        /// 写入游戏日志
        /// </summary>
        /// <param name="level"></param>
        /// <param name="formattedMethod"></param>
        /// <param name="msg"></param>
        private static void WriteGameLog(string level, string formattedMethod, string msg)
        {
            if (!Directory.Exists(GameLogPath))
            {
                Directory.CreateDirectory(GameLogPath);
            }
            string logFile = Path.Combine(GameLogPath, PluginInfo.PLUGIN_GUID + "@" + DateTime.Today.ToString("yyyy-MM-dd") + ".log");
            var levelStr = level.Length < 5 ? level.PadLeft(5) : level.Substring(0, 5);
            StreamWriter sw = new StreamWriter(logFile, true, Encoding.UTF8);
            sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {levelStr} --- {formattedMethod} : {msg}");
            sw.Close();
            sw.Dispose();
        }

        /// <summary>
        /// 写入比赛日志
        /// </summary>
        /// <param name="matchInfo"></param>
        public static void WriteMatchLog(string matchInfo)
        {
            if (!Directory.Exists(GameLogPath))
            {
                Directory.CreateDirectory(GameLogPath);
            }
            string logFile = Path.Combine(GameLogPath, "MatchInfo.log");
            StreamWriter sw = new StreamWriter(logFile, true, Encoding.UTF8);
            sw.WriteLine($"{matchInfo}");
            sw.Close();
            sw.Dispose();
        }

        /// <summary>
        /// 打印信息日志
        /// </summary>
        /// <param name="formattedMethod"></param>
        /// <param name="msg"></param>
        public static void Info(string formattedMethod, string msg)
        {
            Utils.MyLogger(BepInEx.Logging.LogLevel.Info, msg);
            WriteGameLog("INFO", formattedMethod, msg);
        }

        /// <summary>
        /// 打印错误日志
        /// </summary>
        /// <param name="formattedMethod"></param>
        /// <param name="msg"></param>
        public static void Error(string formattedMethod, string msg)
        {
            Utils.MyLogger(BepInEx.Logging.LogLevel.Info, msg);
            WriteGameLog("ERROR", formattedMethod, msg);
        }

        /// <summary>
        /// 打印错误日志
        /// </summary>
        /// <param name="formattedMethod"></param>
        /// <param name="ex"></param>
        public static void Exception(string formattedMethod, Exception ex)
        {
            var exStr = $"{ex.Message} \n{ex.InnerException} \n{ex.StackTrace}";
            Utils.MyLogger(BepInEx.Logging.LogLevel.Error, exStr);
            WriteGameLog("ERROR", formattedMethod, exStr);
        }
    }
}