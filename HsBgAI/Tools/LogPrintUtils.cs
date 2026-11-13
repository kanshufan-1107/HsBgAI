using System.Text;
using BepInEx;

namespace HsBgAI.Tools
{
    public class LogPrintUtils
    {
        /// <summary>日志写入锁：主线程与后台线程（如 SaveGameInfoAsync）会同时写同一份日志</summary>
        private static readonly object _writeLock = new object();

        /// <summary>
        /// 初始化日志文件路径
        /// </summary>
        public static string GameLogPath = Path.Combine(Paths.BepInExRootPath, PluginInfo.PARENT_PLUGIN_GUID, "GameLog", HsBgAI.hsUnitID);

        private static void EnsureLogDirExists()
        {
            if (!Directory.Exists(GameLogPath))
                Directory.CreateDirectory(GameLogPath);
        }

        /// <summary>
        /// 追加一行日志：加锁 + FileShare.ReadWrite，避免并发写或外部程序读取时抛
        /// IOException（Sharing violation）
        /// </summary>
        private static void AppendLine(string logFile, string line)
        {
            lock (_writeLock)
            {
                try
                {
                    using (var stream = new FileStream(logFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    using (var writer = new StreamWriter(stream, Encoding.UTF8))
                    {
                        writer.WriteLine(line);
                    }
                }
                catch (Exception ex)
                {
                    // 写日志失败不能影响主流程，只往 BepInEx 日志里留一条
                    Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, $"写入 {Path.GetFileName(logFile)} 失败：{ex.Message}");
                }
            }
        }

        private static void WriteGameLog(string level, string formattedMethod, string msg)
        {
            EnsureLogDirExists();
            string logFile = Path.Combine(GameLogPath, PluginInfo.PLUGIN_GUID + "@" + DateTime.Today.ToString("yyyy-MM-dd") + ".log");
            var levelStr = level.Length < 5 ? level.PadLeft(5) : level.Substring(0, 5);
            AppendLine(logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {levelStr} --- {formattedMethod} : {msg}");
        }

        /// <summary>
        /// 写入比赛日志
        /// </summary>
        /// <param name="matchInfo"></param>
        public static void WriteMatchLog(string matchInfo)
        {
            EnsureLogDirExists();
            string logFile = Path.Combine(GameLogPath, "MatchInfo.log");
            AppendLine(logFile, matchInfo);
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
        /// 打印警告日志（预期内的时序问题，如 GameState 尚未就绪，不应计入错误）
        /// </summary>
        /// <param name="formattedMethod"></param>
        /// <param name="msg"></param>
        public static void Warning(string formattedMethod, string msg)
        {
            Utils.MyLogger(BepInEx.Logging.LogLevel.Warning, msg);
            WriteGameLog("WARN", formattedMethod, msg);
        }

        /// <summary>
        /// 打印错误日志
        /// </summary>
        /// <param name="formattedMethod"></param>
        /// <param name="msg"></param>
        public static void Error(string formattedMethod, string msg)
        {
            Utils.MyLogger(BepInEx.Logging.LogLevel.Error, msg);
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