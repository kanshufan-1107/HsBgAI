using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using BepInEx;
using Newtonsoft.Json;

namespace HsBgAI.Tools
{
    /// <summary>
    /// 版本周期维护。
    /// 每天 05:00 抓取官网资料片页面，刷新 <see cref="PluginInfo"/> 中的
    /// GAME_VERSION_START_TIME（当前资料片正式上线日）/ GAME_VERSION_END_TIME（下个资料片正式上线日）/
    /// CURRENT_VERSION_DURATION_STR。
    /// 抓取失败或官方尚未公布下个版本时，保留上一次成功的结果（缓存于 BepInEx/HsAI/GameVersion.json）；
    /// 从未成功过则沿用 PluginInfo 中的回退值。
    /// </summary>
    public class GameVersionService
    {
        /// <summary>
        /// 每天刷新时刻（本地时间，24 小时制）
        /// </summary>
        private const int RefreshHour = 5;
        /// <summary>
        /// 定时检查周期（分钟），用于命中刷新时刻
        /// </summary>
        private const int TickMinutes = 5;
        /// <summary>
        /// 抓取失败后的退避时长（分钟）
        /// </summary>
        private const int RetryMinutes = 30;
        /// <summary>
        /// 每年主资料片数量，用于「去年同序号版本 +1 年」推算下个版本
        /// </summary>
        private const int SetsPerYear = 3;
        /// <summary>
        /// 最多抓取的资料片页面数量（总览页按从新到旧排列）
        /// </summary>
        private const int MaxSetPages = 12;
        /// <summary>
        /// 版本周期的最小天数，用于校验推算出的结束日期是否合理
        /// </summary>
        private const int MinVersionSpanDays = 60;

        /// <summary>
        /// 官方卡牌系列总览页，其上的链接指向各资料片子页面
        /// </summary>
        private const string IndexUrl = "https://hearthstone.blizzard.com/en-us/expansions-adventures";

        private static readonly string LogTag = (PluginInfo.PLUGIN_GUID + ".GameVersion").PadRight(40);

        /// <summary>
        /// 资料片子页面 FAQ 首条即上线时间，问句动词有 launch / release 两种写法：
        /// "When will CATACLYSM launch? March 17, 2026."
        /// "When will Escape from Violet Hold release? July 7, 2026 at 10:00 a.m. PT."
        /// </summary>
        private static readonly Regex FaqLaunchRegex = new Regex(
            @"When\s+(?:will|does)\s+(?<name>[^?]{1,120}?)\s+(?:launch|release|be\s+available|come\s+out)\s*\?\s*" +
            @"(?<month>January|February|March|April|May|June|July|August|September|October|November|December)\s+(?<day>\d{1,2}),\s*(?<year>\d{4})" +
            @"(?:\s+at\s+(?<hour>\d{1,2}):(?<minute>\d{2})\s*(?<ampm>a\.m\.|p\.m\.)\s*PT)?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// 页面中带年份的日期，FAQ 问句改版解析失败时作为兜底（仅在全文唯一时才采用）
        /// </summary>
        private static readonly Regex DateWithYearRegex = new Regex(
            @"(?<month>January|February|March|April|May|June|July|August|September|October|November|December)\s+(?<day>\d{1,2}),\s*(?<year>\d{4})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// 总览页中的资料片子页面链接，如 href="/en-us/expansions-adventures/cataclysm/"
        /// </summary>
        private static readonly Regex SetLinkRegex = new Regex(
            @"/en-us/expansions-adventures/(?<slug>[a-z0-9][a-z0-9\-]*)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ScriptStyleRegex = new Regex(@"(?s)<(script|style)\b.*?</\1>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex TagRegex = new Regex(@"<[^>]+>", RegexOptions.Compiled);
        private static readonly Regex SpaceRegex = new Regex(@"\s+", RegexOptions.Compiled);

        private static readonly object _sync = new object();
        private static readonly HttpClient _http = CreateHttpClient();
        private static readonly Lazy<GameVersionService> _instance = new Lazy<GameVersionService>(() => new GameVersionService());

        private static TimeZoneInfo _pacificZone;
        private static bool _pacificZoneResolved;

        /// <summary>
        /// 上一次成功刷新的日期，同一天内不重复抓取
        /// </summary>
        private DateTime _lastRefreshDate = DateTime.MinValue;
        /// <summary>
        /// 失败退避截止时间
        /// </summary>
        private DateTime _retryAfter = DateTime.MinValue;
        /// <summary>
        /// 是否有抓取任务正在执行，用于防止定时器重入
        /// </summary>
        private bool _refreshing;

        private Timer _timer;

        static GameVersionService()
        {
            try
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }
            catch
            {
                // 老系统不支持 TLS1.2 枚举时忽略，交由系统默认协商
            }
        }

        private GameVersionService() { }

        public static GameVersionService Instance => _instance.Value;

        /// <summary>
        /// 载入本地缓存并启动每日刷新（每 5 分钟检查一次，命中每天 05:00）
        /// </summary>
        public void Start()
        {
            LoadCache();
            _timer ??= new Timer(_ => Tick(), null, TimeSpan.Zero, TimeSpan.FromMinutes(TickMinutes));
        }

        /// <summary>
        /// 停止每日刷新（插件卸载时调用）
        /// </summary>
        public void Stop()
        {
            _timer?.Dispose();
            _timer = null;
        }

        /// <summary>
        /// 定时检查：今天是否已刷新、是否已到 05:00、是否处于失败退避期
        /// </summary>
        private void Tick()
        {
            lock (_sync)
            {
                // 上一次抓取若因网络慢而超时，跳过本轮，避免重入
                if (_refreshing) return;
                var now = DateTime.Now;
                if (_lastRefreshDate == now.Date) return;
                if (now.Hour < RefreshHour) return;
                if (now < _retryAfter) return;
                _refreshing = true;
            }

            try
            {
                if (Refresh())
                {
                    lock (_sync)
                    {
                        _lastRefreshDate = DateTime.Now.Date;
                        _retryAfter = DateTime.MinValue;
                    }
                }
                else
                {
                    lock (_sync)
                    {
                        _retryAfter = DateTime.Now.AddMinutes(RetryMinutes);
                    }
                    Log($"刷新失败，{RetryMinutes} 分钟后重试，本次沿用 {PluginInfo.CURRENT_VERSION_DURATION_STR}", false);
                }
            }
            catch (Exception ex)
            {
                lock (_sync)
                {
                    _retryAfter = DateTime.Now.AddMinutes(RetryMinutes);
                }
                Log("刷新异常：" + ex.Message, true);
            }
            finally
            {
                lock (_sync)
                {
                    _refreshing = false;
                }
            }
        }

        /// <summary>
        /// 抓取官网并刷新版本时间
        /// </summary>
        /// <returns>是否成功</returns>
        private bool Refresh()
        {
            var sets = FetchSets();
            if (sets.Count == 0)
                return false;

            var (start, end, source) = ComputeWindow(sets);
            Apply(start, end);
            SaveCache(start, end, source);

            var current = sets.FindLast(s => s.LaunchLocal.Date == start);
            Log($"版本时间已刷新（{source}，共解析 {sets.Count} 个资料片）：" +
                $"{current?.Name ?? "未知版本"} {PluginInfo.CURRENT_VERSION_DURATION_STR}", false);
            return true;
        }

        /// <summary>
        /// 计算当前版本窗口。
        /// 开始时间 = 最近一个已上线的资料片上线日；
        /// 结束时间 = 下个资料片上线日，若官方尚未公布则按「去年同序号版本 +1 年」推算。
        /// </summary>
        private static (DateTime Start, DateTime End, string Source) ComputeWindow(List<SetEntry> sets)
        {
            var today = DateTime.Now.Date;
            int current = sets.FindLastIndex(s => s.LaunchLocal.Date <= today);
            if (current < 0)
                current = 0; // 全部还是未来版本（刚公布时），退化为最近的已知版本
            var start = sets[current].LaunchLocal.Date;

            if (current + 1 < sets.Count)
                return (start, sets[current + 1].LaunchLocal.Date, "official");

            // 每年 3 个主资料片，下个版本同序号的上一年版本即往前数 3 个
            int sameSlotLastYear = current + 1 - SetsPerYear;
            if (sameSlotLastYear >= 0)
            {
                var estimated = sets[sameSlotLastYear].LaunchLocal.Date.AddYears(1);
                // 版本次序号错乱时（如历史页面缺失）会推出明显不合理的日期，这里做区间保护
                if (estimated >= start.AddDays(MinVersionSpanDays) && estimated < start.AddMonths(9))
                    return (start, estimated, "estimated");
            }

            // 历史数据不足时按上一个版本周期长度顺延
            if (current > 0)
            {
                var span = start - sets[current - 1].LaunchLocal.Date;
                if (span > TimeSpan.Zero)
                    return (start, start + span, "estimated");
            }

            return (start, start.AddMonths(4), "estimated");
        }

        /// <summary>
        /// 写入 PluginInfo，供 PassportEnd 等处读取
        /// </summary>
        private static void Apply(DateTime start, DateTime end)
        {
            PluginInfo.GAME_VERSION_START_TIME = start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            PluginInfo.GAME_VERSION_END_TIME = end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            PluginInfo.CURRENT_VERSION_DURATION_STR =
                $"{start.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture)} ~ {end.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture)}";
        }

        /// <summary>
        /// 抓取总览页的全部资料片子页面并解析上线时间，按上线时间升序返回
        /// </summary>
        private static List<SetEntry> FetchSets()
        {
            return FetchSetsAsync().GetAwaiter().GetResult();
        }

        private static async Task<List<SetEntry>> FetchSetsAsync()
        {
            var sets = new List<SetEntry>();
            var index = await _http.GetStringAsync(IndexUrl).ConfigureAwait(false);

            var slugs = new List<string>();
            foreach (Match m in SetLinkRegex.Matches(index))
            {
                var slug = m.Groups["slug"].Value.ToLowerInvariant();
                if (!slugs.Contains(slug))
                    slugs.Add(slug);
            }
            if (slugs.Count == 0)
            {
                Log("总览页未解析到任何资料片链接，可能页面结构已变更", true);
                return sets;
            }

            foreach (var slug in slugs.Take(MaxSetPages))
            {
                try
                {
                    var html = await _http.GetStringAsync($"{IndexUrl}/{slug}").ConfigureAwait(false);
                    var entry = ParseSet(slug, html);
                    if (entry == null)
                    {
                        Log($"资料片 {slug} 未解析到上线时间（可能尚未公布），已跳过", false);
                        continue;
                    }
                    sets.Add(entry);
                }
                catch (Exception ex)
                {
                    Log($"抓取资料片 {slug} 失败：{ex.Message}", true);
                }
            }

            sets.Sort((a, b) => a.LaunchLocal.CompareTo(b.LaunchLocal));
            return sets;
        }

        /// <summary>
        /// 从资料片子页面解析上线时间（太平洋时间），并换算为本地时间
        /// </summary>
        private static SetEntry ParseSet(string slug, string html)
        {
            var text = HtmlToText(html);

            var match = FaqLaunchRegex.Match(text);
            if (match.Success)
            {
                var launch = ParseDate(match);
                if (!launch.HasValue)
                    return null;
                return new SetEntry
                {
                    Slug = slug,
                    Name = WebUtility.HtmlDecode(match.Groups["name"].Value).Trim(),
                    LaunchLocal = launch.Value,
                };
            }

            // FAQ 问句改版时兜底：全文唯一一个带年份的日期即上线日
            var dates = DateWithYearRegex.Matches(text);
            if (dates.Count == 1)
            {
                var launch = ParseDate(dates[0]);
                if (launch.HasValue)
                {
                    return new SetEntry
                    {
                        Slug = slug,
                        Name = slug,
                        LaunchLocal = launch.Value,
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// 解析匹配到的日期与可选时刻（太平洋时间），换算为本地时间
        /// </summary>
        private static DateTime? ParseDate(Match match)
        {
            var month = MonthIndex(match.Groups["month"].Value);
            if (month <= 0)
                return null;
            if (!int.TryParse(match.Groups["year"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var year))
                return null;
            if (!int.TryParse(match.Groups["day"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var day))
                return null;

            // 官网 FAQ 未给时刻时按惯例视为 10:00 a.m. PT
            int hour = 10, minute = 0;
            if (match.Groups["hour"].Success && match.Groups["minute"].Success)
            {
                int.TryParse(match.Groups["hour"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out hour);
                int.TryParse(match.Groups["minute"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out minute);
                var ampm = match.Groups["ampm"].Value;
                if (ampm.StartsWith("p", StringComparison.OrdinalIgnoreCase) && hour < 12) hour += 12;
                else if (ampm.StartsWith("a", StringComparison.OrdinalIgnoreCase) && hour == 12) hour = 0;
            }

            return ToLocalTime(year, month, day, hour, minute);
        }

        /// <summary>
        /// 太平洋时间换算为本地时间（自动处理夏令时）
        /// </summary>
        private static DateTime ToLocalTime(int year, int month, int day, int hour, int minute)
        {
            var pacific = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);
            var zone = PacificZone;
            DateTime utc;
            if (zone != null && !zone.IsInvalidTime(pacific))
                utc = TimeZoneInfo.ConvertTimeToUtc(pacific, zone);
            else
                utc = DateTime.SpecifyKind(pacific.AddHours(8), DateTimeKind.Utc); // 兜底按 UTC-8 处理
            return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.Local);
        }

        private static TimeZoneInfo PacificZone
        {
            get
            {
                lock (_sync)
                {
                    if (!_pacificZoneResolved)
                    {
                        _pacificZoneResolved = true;
                        foreach (var id in new[] { "Pacific Standard Time", "America/Los_Angeles" })
                        {
                            try
                            {
                                _pacificZone = TimeZoneInfo.FindSystemTimeZoneById(id);
                                break;
                            }
                            catch
                            {
                                // 换下一个时区 ID
                            }
                        }
                    }
                    return _pacificZone;
                }
            }
        }

        private static int MonthIndex(string month)
        {
            try
            {
                return DateTime.ParseExact(month, "MMMM", CultureInfo.InvariantCulture).Month;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 去除脚本、样式与标签，得到便于正则匹配的纯文本
        /// </summary>
        private static string HtmlToText(string html)
        {
            var text = ScriptStyleRegex.Replace(html, " ");
            text = TagRegex.Replace(text, " ");
            text = WebUtility.HtmlDecode(text);
            return SpaceRegex.Replace(text, " ");
        }

        private static string CachePath =>
            Path.Combine(Paths.BepInExRootPath, PluginInfo.PARENT_PLUGIN_GUID, "GameVersion.json");

        /// <summary>
        /// 载入上一次成功刷新的结果，若为今天刷新的则今天不再重复抓取
        /// </summary>
        private void LoadCache()
        {
            try
            {
                if (!File.Exists(CachePath))
                    return;

                var model = JsonConvert.DeserializeObject<CacheModel>(File.ReadAllText(CachePath));
                if (model == null)
                    return;
                if (!DateTime.TryParseExact(model.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var start))
                    return;
                if (!DateTime.TryParseExact(model.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
                    return;
                if (end <= start)
                    return;

                Apply(start, end);
                if (DateTime.TryParse(model.UpdatedAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var updatedAt)
                    && updatedAt.Date == DateTime.Today)
                {
                    _lastRefreshDate = DateTime.Today;
                }
                Log($"已载入缓存的版本时间：{PluginInfo.CURRENT_VERSION_DURATION_STR}", false);
            }
            catch (Exception ex)
            {
                Log("读取版本时间缓存失败：" + ex.Message, true);
            }
        }

        private static void SaveCache(DateTime start, DateTime end, string source)
        {
            try
            {
                var dir = Path.GetDirectoryName(CachePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                var model = new CacheModel
                {
                    StartDate = start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    EndDate = end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Source = source,
                    UpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                };
                File.WriteAllText(CachePath, JsonConvert.SerializeObject(model, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Log("写入版本时间缓存失败：" + ex.Message, true);
            }
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) HsBgAI-GameVersion/1.0");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
            return client;
        }

        private static void Log(string msg, bool error)
        {
            try
            {
                if (error)
                    LogPrintUtils.Error(LogTag, msg);
                else
                    LogPrintUtils.Info(LogTag, msg);
            }
            catch
            {
                // 日志失败不影响主流程
            }
        }

        /// <summary>
        /// 单个资料片的上线信息
        /// </summary>
        private sealed class SetEntry
        {
            public string Slug;
            public string Name;
            /// <summary>
            /// 上线时间（本地时间，含时刻）
            /// </summary>
            public DateTime LaunchLocal;
        }

        /// <summary>
        /// 本地缓存结构
        /// </summary>
        private sealed class CacheModel
        {
            public string StartDate { get; set; }
            public string EndDate { get; set; }
            public string Source { get; set; }
            public string UpdatedAt { get; set; }
        }
    }
}
