using System.Diagnostics;
using System.Runtime.CompilerServices;
using BepInEx.Logging;
using Hearthstone.Progression;
using HsBgAI.Ai;
using HsBgAI.Enums;
using HsBgAI.Mapping;
using Newtonsoft.Json;
using UnityEngine;
using static Assets.Global;

namespace HsBgAI.Tools
{
    public class Utils
    {
        /// <summary>
        /// 缓存 LogSource，避免每次日志调用都 Create/Remove（高频调用的性能优化）
        /// </summary>
        private static ManualLogSource _logSource;

        /// <summary>
        /// 在主程序日志文件中打印本插件日志
        /// </summary>
        /// <param name="level"></param>
        /// <param name="message"></param>
        public static void MyLogger(LogLevel level, object message)
        {
            _logSource ??= BepInEx.Logging.Logger.CreateLogSource(PluginInfo.PLUGIN_GUID);
            _logSource.Log(level, message);
        }

        /// <summary>
        /// 获取调用方法的名称。
        /// 由编译期注入的 CallerMemberName/CallerFilePath 直接得到「类名.方法名」，
        /// 避免原先每次日志都 new StackTrace() 走栈的高昂开销（高频调用的性能优化）。
        /// </summary>
        /// <returns></returns>
        public static string GetCallingMethod([CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            var className = string.IsNullOrEmpty(filePath) ? "" : Path.GetFileNameWithoutExtension(filePath);
            var callingMethod = string.IsNullOrEmpty(className) ? memberName : $"{className}.{memberName}";
            if (string.IsNullOrEmpty(callingMethod))
            {
                return "UnknownMethod".PadRight(40);
            }
            return callingMethod.Length > 40 ? callingMethod.Substring(0, 40) : callingMethod.PadRight(40);
        }

        /// <summary>
        /// 保存错误监控信息
        /// </summary>
        public static void SaveErrorJsonData()
        {
            if (!Directory.Exists(HsBgAI.pluginMainPath)) Directory.CreateDirectory(HsBgAI.pluginMainPath);
            var jsonFile = Path.Combine(HsBgAI.pluginMainPath, $"Monitor{HsBgAI.monitorSuffix}.json");
            GameInfo gameInfo;
            if (File.Exists(jsonFile))
            {
                string jsonStr = File.ReadAllText(jsonFile);
                gameInfo = JsonConvert.DeserializeObject<GameInfo>(jsonStr) ?? new GameInfo();
                gameInfo.UpdateRequired = HsBgAI.updateRequired;
                gameInfo.TemporarilyClosed = HsBgAI.temporarilyClosed;
                File.Delete(jsonFile);
            }
            else
            {
                gameInfo = new GameInfo()
                {
                    UpdateRequired = HsBgAI.updateRequired,
                    TemporarilyClosed = HsBgAI.temporarilyClosed
                };
            }
            LogPrintUtils.Info(GetCallingMethod(), $"保存异常信息到Monitor{HsBgAI.monitorSuffix}.json");
            string json = JsonConvert.SerializeObject(gameInfo, Formatting.Indented);
            File.WriteAllText(jsonFile, json);
        }

        /// <summary>
        /// 转化战旗状态为中文
        /// </summary>
        /// <param name="battleGroundState"></param>
        /// <returns></returns>
        public static string TranslateBattleGroundState(GameBattleState battleGroundState)
        {
            switch (battleGroundState)
            {
                case GameBattleState.INVALID:
                    goto default;
                case GameBattleState.FINDING_GAME:
                    return "寻找游戏";
                case GameBattleState.CHOOSE_HERO:
                    return "选择英雄";
                case GameBattleState.WAITING_FOR_OWN_TURN:
                    return "等待我方回合";
                case GameBattleState.OWN_TURN:
                    return "我方回合";
                case GameBattleState.BATTLE_TURN:
                    return "对战回合";
                case GameBattleState.GAME_OVER:
                    return "游戏结束";
                case GameBattleState.FATAL_ERROR:
                    return "游戏错误";
                default:
                    return "无效";
            }
        }

        /// <summary>
        /// 计算通行证经验
        /// </summary>
        /// <param name="preLevel"></param>
        /// <param name="preXp"></param>
        /// <param name="curLevel"></param>
        /// <param name="curXp"></param>
        /// <param name="rewardTrackType"></param>
        /// <returns></returns>
        public static int CalculateIntroXpGained(int preLevel, int preXp, int curLevel, int curXp, RewardTrackType rewardTrackType)
        {
            RewardTrack rewardTrack = RewardTrackManager.Get().GetRewardTrack(rewardTrackType);
            bool leveledUp = preLevel < curLevel;

            // 本级已获得的经验：若升级则计入本级剩余经验，否则直接用当前经验差
            int preLevelXpNeeded = rewardTrack.GetRewardTrackLevelRecord(preLevel).XpNeeded;
            int totalXpGained = (leveledUp ? preLevelXpNeeded : curXp) - preXp;

            // 跨多级时，累加中间每级的满级经验
            if (curLevel > preLevel + 1)
            {
                for (int level = preLevel + 1; level < curLevel; level++)
                {
                    int? midLevelXpNeeded = rewardTrack?.GetRewardTrackLevelRecord(level)?.XpNeeded;
                    totalXpGained += midLevelXpNeeded.GetValueOrDefault();
                }
            }

            // 最终加上当前等级已经累积的经验
            if (leveledUp) totalXpGained += curXp;
            return totalXpGained;
        }

        /// <summary>
        /// 退出游戏
        /// </summary>
        /// <param name="exitCode"></param>
        public static void Quit(int exitCode = 0)
        {
            try
            {
                // 尝试保存游戏状态
                try { Ai.MainProcess.Instance.SaveGameInfoSync(); }
                catch { }
                MyLogger(LogLevel.Info, $"HsBgAI 退出中, exitCode={exitCode}");
                UnityEngine.Application.Quit(exitCode);
            }
            finally
            {
                // 给 Unity 一些时间优雅关闭，超时后强制终止
                Thread.Sleep(5000);
                Process.GetCurrentProcess().Kill();
            }
        }

        /// <summary>
        /// 生成UUID
        /// </summary>
        /// <returns></returns>
        public static string GenerateJavaCompatibleUuid()
        {
            Guid guid = Guid.NewGuid();
            return guid.ToString().ToUpper().Replace("-", "");
        }

        /// <summary>
        /// 批量分解多余的金卡
        /// </summary>
        public static void DisenchantExtraGoldenCards()
        {
            int totalSell = 0;
            int disenchantCount = 0;
            Network network = Network.Get();

            // 注册网络处理器
            network.RegisterNetHandler(PegasusUtil.BoughtSoldCard.PacketID.ID,
                new Network.NetHandler(DisenchantExtraGoldenCardsCallback), null);

            Dictionary<string, CraftingPendingTransaction> pendingTransactions =
                new Dictionary<string, CraftingPendingTransaction>();

            // 获取所有可制作的卡牌记录
            var allCards = CollectionManager.Get().GetOwnedCards();
            var recordList = allCards
                .Where(record => record != null && record.IsCraftable && record.OwnedCount > 0)
                .ToList();

            // 按卡牌ID分组，处理同一卡牌的不同品质
            var cardGroups = recordList
                .GroupBy(record => record.CardId)
                .ToList();

            foreach (var cardGroup in cardGroups)
            {
                string cardId = cardGroup.Key;

                // 获取金卡分解价值，排除金卡分解价值为1的卡牌
                CraftingManager.Get().TryGetCardSellValue(cardId, TAG_PREMIUM.GOLDEN, out int goldenSellValue);

                // 排除分解价值为1的卡牌（通常是基础卡牌）
                if (goldenSellValue <= 1) continue;

                // 获取卡牌信息以判断是否为传说
                var cardDef = DefLoader.Get().GetEntityDef(cardId);
                if (cardDef == null) continue;

                bool isLegendary = cardDef.GetRarity() == TAG_RARITY.LEGENDARY;

                // 获取各种品质的卡牌数量
                int normalCount = CollectionManager.Get().GetNumCopiesInCollection(cardId, TAG_PREMIUM.NORMAL);
                int goldenCount = CollectionManager.Get().GetNumCopiesInCollection(cardId, TAG_PREMIUM.GOLDEN);
                    
                // 判断是否需要分解金卡
                int goldenDisenchantCount = 0;

                if (isLegendary)
                {
                    // 传说卡牌：已有1张普通+1张金卡，分解金卡
                    if (normalCount >= 1 && goldenCount >= 1)
                    {
                        goldenDisenchantCount = goldenCount; // 分解所有金卡
                    }
                }
                else
                {
                    // 非传说卡牌：已有2张普通卡，分解所有金卡
                    int maxNormalCopies = 2; // 非传说卡牌最大保留2张普通卡
                    if (normalCount >= maxNormalCopies && goldenCount > 0)
                    {
                        goldenDisenchantCount = goldenCount; // 分解所有金卡
                    }
                }

                // 如果有金卡需要分解，创建交易
                if (goldenDisenchantCount > 0)
                {
                    if (!pendingTransactions.ContainsKey(cardId))
                    {
                        CraftingPendingTransaction transaction = new CraftingPendingTransaction
                        {
                            CardID = cardId,
                            Premium = TAG_PREMIUM.GOLDEN, // 分解金卡
                            NormalDisenchantCount = 0,
                            GoldenDisenchantCount = goldenDisenchantCount,
                            SignatureDisenchantCount = 0,
                            DiamondDisenchantCount = 0
                        };
                        pendingTransactions[cardId] = transaction;
                    }
                }
            }

            // 发送所有分解交易
            foreach (var transaction in pendingTransactions.Values)
            {
                if (transaction == null || transaction.GoldenDisenchantCount <= 0) continue;

                // 获取卡牌信息用于日志
                var cardDef = DefLoader.Get().GetEntityDef(transaction.CardID);
                string cardName = cardDef?.GetName() ?? transaction.CardID;
                bool isLegendary = cardDef?.GetRarity() == TAG_RARITY.LEGENDARY;

                // 计算粉尘价值
                CraftingManager.Get().TryGetCardSellValue(transaction.CardID, TAG_PREMIUM.GOLDEN, out int goldenSellValue);
                int sellValue = -(goldenSellValue * transaction.GoldenDisenchantCount);
                totalSell += -sellValue;
                disenchantCount += transaction.GoldenDisenchantCount;

                // 获取当前收藏数量
                int numNormalCopies = CollectionManager.Get().GetNumCopiesInCollection(transaction.CardID, TAG_PREMIUM.NORMAL);
                int numGoldenCopies = CollectionManager.Get().GetNumCopiesInCollection(transaction.CardID, TAG_PREMIUM.GOLDEN);
                int numSignatureCopies = CollectionManager.Get().GetNumCopiesInCollection(transaction.CardID, TAG_PREMIUM.SIGNATURE);
                int numDiamondCopies = CollectionManager.Get().GetNumCopiesInCollection(transaction.CardID, TAG_PREMIUM.DIAMOND);

                // 发送分解请求
                network.CraftingTransaction(transaction, sellValue,
                    numNormalCopies, numGoldenCopies, numSignatureCopies, numDiamondCopies);

                MyLogger(LogLevel.Info,
                    $"分解{(isLegendary ? "传说" : "非传说")}卡牌：{cardName}，" +
                    $"分解{transaction.GoldenDisenchantCount}张金卡，获得{-sellValue}粉尘。");
            }

            // 显示结果
            if (disenchantCount > 0)
            {
                MyLogger(LogLevel.Warning,
                    $"分解完成：共分解{disenchantCount}张金卡，获得{totalSell}粉尘。");
                UIStatus.Get().AddInfo(
                    $"分解完成：共分解{disenchantCount}张金卡，获得{totalSell}粉尘。");
            }
            else
            {
                MyLogger(LogLevel.Info, "没有符合分解条件的金卡。");
                UIStatus.Get().AddInfo("没有符合分解条件的金卡。");
            }
        }

        /// <summary>
        /// 分解回调方法
        /// </summary>
        public static void DisenchantExtraGoldenCardsCallback()
        {
            Network.CardSaleResult cardSaleResult = Network.Get().GetCardSaleResult();
            if (cardSaleResult.Action == Network.CardSaleResult.SaleResult.CARD_WAS_BOUGHT)
            {
                MyLogger(LogLevel.Info, "正在购买卡牌，跳过分解检查");
            }
            else if (cardSaleResult.Action != Network.CardSaleResult.SaleResult.CARD_WAS_SOLD)
            {
                MyLogger(LogLevel.Warning, $"分解失败：{cardSaleResult.Action}");
                UIStatus.Get().AddInfo("分解失败");
            }
            else
            {
                MyLogger(LogLevel.Warning, "分解成功");
            }
        }
    }
}