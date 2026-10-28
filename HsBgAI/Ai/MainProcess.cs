using System.Threading;
using Blizzard.T5.Services;
using Cysharp.Threading.Tasks;
using Hearthstone.Progression;
using HsBgAI.Enums;
using HsBgAI.Firestone;
using HsBgAI.Mapping;
using HsBgAI.Tools;
using Newtonsoft.Json;
using static HsBgAI.PluginConfig;

namespace HsBgAI.Ai
{
    public class MainProcess
    {
        public static MainProcess Instance
        {
            get
            {
                _instance ??= new MainProcess();
                return _instance;
            }
        }
        public bool Running = false;
        public GameInfo gameInfo;


        private static MainProcess _instance;
        private GameBattleState _state;
        private GameBattleState _preState;
        private GameBattleState _nextState;
        private int _curStateTimes = 0;
        private bool _hasOpitonToDo = true;
        private static readonly string JsonFilePath = Path.Combine(HsBgAI.pluginMainPath, $"Monitor{HsBgAI.monitorSuffix}.json");
        private readonly object _saveLock = new object();
        private DateTime _lastSaveTime = DateTime.MinValue;
        private bool _isDirty = false;
        private CancellationTokenSource _delaySaveCts;

        /// <summary>
        /// 初始化新对局的全局状态标志
        /// </summary>
        private static void InitNewGame()
        {
            HsBgAI.xpChangedFirstSave = true;
            HsBgAI.endGameScreenFirstSave = true;
            HsBgAI.findingGameFirstSave = false;
            HsBgAI.lazzyGameCount = 0;
        }

        /// <summary>
        /// 确保 GameUid 已生成
        /// </summary>
        private static void EnsureGameUid()
        {
            if (string.IsNullOrEmpty(HsBgAI.GameUid))
                HsBgAI.GameUid = Utils.GenerateJavaCompatibleUuid();
        }

        private MainProcess()
        {
            _state = GameBattleState.INVALID;
            _nextState = GameBattleState.INVALID;
            _preState = GameBattleState.INVALID;

            gameInfo = new GameInfo();
            LoadGameInfo();
        }

        /// <summary>
        /// 加载游戏信息
        /// </summary>
        private void LoadGameInfo()
        {
            try
            {
                if (!Directory.Exists(HsBgAI.pluginMainPath))
                    Directory.CreateDirectory(HsBgAI.pluginMainPath);

                if (File.Exists(JsonFilePath))
                {
                    var json = File.ReadAllText(JsonFilePath);
                    var gameInfoObj = JsonConvert.DeserializeObject<GameInfo>(json);
                    if (gameInfoObj != null)
                    {
                        gameInfo.AllRunTime = gameInfoObj.AllRunTime;
                        gameInfo.AllGetXp = gameInfoObj.AllGetXp;
                    }
                    LogPrintUtils.Info(Utils.GetCallingMethod(), "游戏信息加载成功");
                }
            }
            catch (Exception ex)
            {
                LogPrintUtils.Exception(Utils.GetCallingMethod(), ex);
            }
        }

        /// <summary>
        /// 异步保存游戏信息
        /// </summary>
        /// <param name="forceSave"></param>
        /// <returns></returns>
        /// <summary>
        /// 同步保存游戏信息（用于退出等异步不可用的场景）
        /// </summary>
        public void SaveGameInfoSync()
        {
            try
            {
                var json = JsonConvert.SerializeObject(gameInfo, Formatting.Indented);
                var tempFilePath = JsonFilePath + ".tmp";
                File.WriteAllText(tempFilePath, json);
                if (File.Exists(JsonFilePath))
                    File.Delete(JsonFilePath);
                File.Move(tempFilePath, JsonFilePath);
            }
            catch { }
        }

        public async UniTask SaveGameInfoAsync(bool forceSave = false)
        {
            if (!forceSave && !_isDirty) return;

            var now = DateTime.Now;
            if (!forceSave && (now - _lastSaveTime).TotalSeconds < 5) return; // 5秒内只保存一次

            await UniTask.RunOnThreadPool(() =>
            {
                lock (_saveLock)
                {
                    try
                    {
                        var tempFilePath = JsonFilePath + ".tmp";
                        string json = JsonConvert.SerializeObject(gameInfo, Formatting.Indented);
                        File.WriteAllText(tempFilePath, json);

                        // 原子性替换文件
                        if (File.Exists(JsonFilePath))
                            File.Delete(JsonFilePath);
                        File.Move(tempFilePath, JsonFilePath);

                        _lastSaveTime = now;
                        _isDirty = false;

                        LogPrintUtils.Info(Utils.GetCallingMethod(), "游戏信息已保存");
                    }
                    catch (Exception ex)
                    {
                        LogPrintUtils.Exception(Utils.GetCallingMethod(), ex);
                    }
                }
            });
        }

        /// <summary>
        /// 标记数据为脏数据，需要保存。
        /// 重复调用会取消上一次延迟，重新计时，避免叠加多个 UniTask。
        /// </summary>
        public void MarkDirty()
        {
            _isDirty = true;
            _delaySaveCts?.Cancel();
            _delaySaveCts = new CancellationTokenSource();
            _ = DelaySave(_delaySaveCts.Token);
        }

        /// <summary>
        /// 延迟保存（带取消令牌）
        /// </summary>
        private async UniTask DelaySave(CancellationToken ct)
        {
            try
            {
                await UniTask.Delay(2000, cancellationToken: ct);
                await SaveGameInfoAsync();
            }
            catch (OperationCanceledException)
            {
                // 被新的 MarkDirty 调用取消，属正常流程，忽略
            }
        }

        /// <summary>
        /// 开始
        /// </summary>
        /// <returns></returns>
        public async UniTaskVoid Start()
        {
            _ = FirestoneClient.Instance;
            while (Running)
            {
                await NextStep();
                await UniTask.Delay(1000);
            }
            if (!Running)
            {
                _state = GameBattleState.INVALID;
                _nextState = GameBattleState.INVALID;
                _preState = GameBattleState.INVALID;
                // 程序停止时强制保存
                await SaveGameInfoAsync(true);
            }
            return;
        }

        /// <summary>
        /// 下一步执行
        /// </summary>
        /// <returns></returns>
        public async UniTask NextStep()
        {
            if (!Running) return;
            if (GameState.Get()?.IsGameOverNowOrPending() == true)
            {
                LogPrintUtils.Info(Utils.GetCallingMethod(), "切换到回合结束逻辑");
                _nextState = GameBattleState.GAME_OVER;
            }
            else if (SceneMgr.Get()?.GetMode() == SceneMgr.Mode.FATAL_ERROR)
            {
                LogPrintUtils.Info(Utils.GetCallingMethod(), "切换到报错逻辑");
                _nextState = GameBattleState.FATAL_ERROR;
            }
            if (_state != _nextState)
            {
                _preState = _state;
                _state = _nextState;
                _curStateTimes = 0;
                LogPrintUtils.Info(Utils.GetCallingMethod(), $"现在是 {Utils.TranslateBattleGroundState(_state)} 状态");
            }
            else
            {
                _curStateTimes += 1;
            }
            try
            {
                if (Running)
                {
                    switch (_state)
                    {
                        case GameBattleState.INVALID:
                            await FindingBattlegroundsGame();
                            break;
                        case GameBattleState.FINDING_GAME:
                            await FindingBattlegroundsGame();
                            break;
                        case GameBattleState.CHOOSE_HERO:
                            InitNewGame();
                            HsBgAI.GameUid = Utils.GenerateJavaCompatibleUuid();
                            await ChooseHero();
                            break;
                        case GameBattleState.WAITING_FOR_OWN_TURN:
                            InitNewGame();
                            EnsureGameUid();
                            await WaittingForOwnTurn();
                            break;
                        case GameBattleState.OWN_TURN:
                            InitNewGame();
                            EnsureGameUid();
                            await OwnTurn();
                            break;
                        case GameBattleState.BATTLE_TURN:
                            InitNewGame();
                            EnsureGameUid();
                            await BattleTurn();
                            break;
                        case GameBattleState.GAME_OVER:
                            HsBgAI.findingGameFirstSave = false;
                            HsBgAI.lazzyGameFirstPrint = false;
                            HsBgAI.lazzyGameCount = 0;
                            await GameOver();
                            break;
                        case GameBattleState.FATAL_ERROR:
                            FatalError();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogPrintUtils.Exception(Utils.GetCallingMethod(), ex);
                _nextState = GameBattleState.INVALID;
            }
        }

        /// <summary>
        /// 战旗对战-寻找游戏
        /// </summary>
        /// <returns></returns>
        private async UniTask FindingBattlegroundsGame()
        {
            _nextState = GameBattleState.FINDING_GAME;
            if (!HsBgAI.findingGameFirstSave)
            {
                HsBgAI.findingGameFirstSave = true;
                gameInfo = GetGameInfo();
                // 标记需要保存
                MarkDirty();
            }
            var gameMgr = GameMgr.Get();
            if (GameInfoUtils.IsPlaying)
            {
                _nextState = GameBattleState.CHOOSE_HERO;
                LogPrintUtils.Info(Utils.GetCallingMethod(), "进入选择英雄状态");
                return;
            }
            if (!gameMgr.IsFindingGame())
            {
                await UniTask.Delay(8000);
                // 维持分段
                BaconLobbyMgr baconLobbyMgr = BaconLobbyMgr.Get();
                if (baconLobbyMgr != null)
                {
                    int gameRank = baconLobbyMgr.GetBattlegroundsActiveGameModeRating();
                    LogPrintUtils.Info(Utils.GetCallingMethod(), $"战旗当前分数{gameRank}");
                    if (battlegroundsLevelCap.Value >= 0)
                    {
                        switch (BotUtils.CheckConcedeState(Math.Max(battlegroundsLevelCap.Value, gameRank), out int base_rank))
                        {
                            case ConcedeState.START:
                                HsBgAI.needConcede = true;
                                break;
                            case ConcedeState.STOP:
                                HsBgAI.needConcede = false;
                                break;
                        }
                        LogPrintUtils.Info(Utils.GetCallingMethod(), $"[分数维持] 当前分数[{gameRank}]，当前基准[{base_rank}]，投降状态[{(HsBgAI.needConcede ? "投降" : "不投降")}]");
                    }
                }

                LogPrintUtils.Info(Utils.GetCallingMethod(), "开始寻找战棋游戏");
                HsBgAI.matchFirstRecord = true;
                LogPrintUtils.Info(Utils.GetCallingMethod(), "查找比赛......");
                BotUtils.EnterBattleGrounds(Running);
                return;
            }
            else if (gameMgr.IsFindingGame() && _curStateTimes % 5 == 0)
            {
                LogPrintUtils.Info(Utils.GetCallingMethod(), "正在寻找战棋游戏");
                return;
            }
        }

        /// <summary>
        /// 战旗对战-选择英雄
        /// </summary>
        /// <returns></returns>
        private async UniTask ChooseHero()
        {
            if (HsBgAI.needConcede)
            {
                if (HsBgAI.gameStartConcedeTimes > 10)
                {
                    HsBgAI.gameStartConcedeTimes = 0;
                    int prevScore = battlegroundsLevelCap.Value;
                    battlegroundsLevelCap.Value += 500;
                    LogPrintUtils.Info(Utils.GetCallingMethod(), $"[对局中] 投降次数超过10次，维持分段提升 [ {prevScore} ▲ {battlegroundsLevelCap.Value}]");
                }
                else if (_preState == GameBattleState.FINDING_GAME)
                {
                    HsBgAI.gameStartConcedeTimes++;
                }
                _nextState = GameBattleState.GAME_OVER;
                LogPrintUtils.Info(Utils.GetCallingMethod(), "[对局中] 触发自动投降");
                GameState.Get().Concede();
                return;
            }
            _nextState = GameBattleState.CHOOSE_HERO;
            await AsyncUtil.WaitForAsync(async () =>
            {
                if (GameState.Get()?.GetOptionsPacket() != null)
                {
                    return true;
                }
                if (GameState.Get() != null && GameState.Get().IsInChoiceMode())
                {
                    if (MulliganManager.Get() != null)
                    {
                        var cards = MulliganManager.Get().GetStartingCards();
                        if (cards != null && cards.Count > 0)
                        {
                            LogPrintUtils.Info("HsBgAI.Ai.ProcessTree.ChooseHero".PadRight(40), "选择英雄");
                            await UniTask.Delay(3000);
                            HsBgAI.chooseHeroPrev = true;
                            await BotUtils.ChooseHero();
                            return true;
                        }
                    }
                    else
                    {
                        LogPrintUtils.Info("HsBgAI.Ai.ProcessTree.ChooseHero".PadRight(40), "选择卡牌");
                        return true;
                    }
                }
                return false;
            }, TimeSpan.FromSeconds(15)).SuppressCancellationThrow();
            LogPrintUtils.Info(Utils.GetCallingMethod(), "切换到等待我方回合状态");
            _nextState = GameBattleState.WAITING_FOR_OWN_TURN;
            _hasOpitonToDo = true;
            return;
        }

        /// <summary>
        /// 等待己方回合开始
        /// </summary>
        /// <returns></returns>
        private async UniTask WaittingForOwnTurn()
        {
            _nextState = GameBattleState.WAITING_FOR_OWN_TURN;
            await UniTask.Delay(3000);
            if (HsBgAI.chooseHeroPrev)
            {
                HsBgAI.chooseHeroPrev = false;
                if (GameState.Get() != null && GameState.Get().IsMulliganManagerActive())
                {
                    _nextState = GameBattleState.CHOOSE_HERO;
                    LogPrintUtils.Info(Utils.GetCallingMethod(), "切换到选择英雄状态");
                }
                else if (GameInfoUtils.IsBattlegroundShoppingPhase)
                {
                    LogPrintUtils.Info(Utils.GetCallingMethod(), "切换到我方回合状态");
                    _nextState = GameBattleState.OWN_TURN;
                    return;
                }
            }
            else
            {
                if (GameInfoUtils.IsBattlegroundShoppingPhase)
                {
                    LogPrintUtils.Info(Utils.GetCallingMethod(), "切换到我方回合状态");
                    _nextState = GameBattleState.OWN_TURN;
                    return;
                }
                else if (GameInfoUtils.IsBattlegroundBattlePhase)
                {
                    LogPrintUtils.Info(Utils.GetCallingMethod(), "切换到对战回合状态");
                    _nextState = GameBattleState.BATTLE_TURN;
                    return;
                }
            }
        }

        /// <summary>
        /// 战旗对战-己方回合
        /// </summary>
        /// <returns></returns>
        private async UniTask OwnTurn()
        {
            _nextState = GameBattleState.OWN_TURN;

            // 状态切换检查
            if (TrySwitchToBattlePhase()) return;

            // 选择模式处理
            if (TryHandleChoiceMode())
            {
                await UniTask.Delay(3000);
                return;
            }

            // 摆烂逻辑处理
            if (lazzyAfterTop4.Value && TryLazzyAfterTop4()) return;

            // 操作可行性检查
            if (!ShouldExecuteActions()) return;

            // 执行游戏操作
            ExecuteGameActions();
        }

        /// <summary>
        /// 状态切换检查
        /// </summary>
        /// <returns></returns>
        private bool TrySwitchToBattlePhase()
        {
            if (!GameInfoUtils.IsBattlegroundBattlePhase) return false;

            LogPrintUtils.Info(Utils.GetCallingMethod(), "切换到对战回合状态");
            _nextState = GameBattleState.BATTLE_TURN;
            return true;
        }

        /// <summary>
        /// 选择模式处理
        /// </summary>
        /// <returns></returns>
        private bool TryHandleChoiceMode()
        {
            if (GameState.Get()?.IsInChoiceMode() != true) return false;

            if (MulliganManager.Get() != null)
            {
                LogPrintUtils.Info(Utils.GetCallingMethod(), "选择英雄");
                _nextState = GameBattleState.CHOOSE_HERO;
            }
            else
            {
                LogPrintUtils.Info(Utils.GetCallingMethod(), "选择卡牌");
                BotUtils.ChooseBattlegroundCard();
            }
            return true;
        }

        /// <summary>
        /// 摆烂逻辑处理
        /// </summary>
        /// <returns></returns>
        private bool TryLazzyAfterTop4()
        {
            // 摆烂一轮后投降
            if (HsBgAI.lazzyGameCount >= 1)
            {
                LogPrintUtils.Info(Utils.GetCallingMethod(), "卖完卡牌之后，一轮没死，投降！");
                GameState.Get()?.Concede();
                return true;
            }

            // 检查淘汰玩家
            var leaderboard = PlayerLeaderboardManager.Get();
            if (leaderboard == null) return false;

            int deadCount = leaderboard.GetAllTiles().Count(p => p.Entity?.GetCurrentHealth() <= 0);
            if (deadCount < 4) return false;

            // 执行摆烂操作
            if (GameInfoUtils.OwnMinions.Count > 0 && BotUtils.SellAllCard())
            {
                if (!HsBgAI.lazzyGameFirstPrint)
                {
                    HsBgAI.lazzyGameFirstPrint = true;
                    HsBgAI.lazzyGameCount++;
                    LogPrintUtils.Info(Utils.GetCallingMethod(),
                        $"当前已有{deadCount}人淘汰，开始摆烂...{HsBgAI.lazzyGameCount}");
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 操作可行性检查
        /// </summary>
        /// <returns></returns>
        private bool ShouldExecuteActions()
        {
            if (_hasOpitonToDo) return true;

            // 每10次检查一次可用操作
            if (_curStateTimes % 10 != 0) return false;

            bool hasAction = GameInfoUtils.OwnHands.Any(item =>
                item.GetEntity()?.GetCard() is { } card &&
                GameInfoUtils.CardIsCanBeUsed(card)
            );

            if (hasAction)
            {
                LogPrintUtils.Info(Utils.GetCallingMethod(), $"检测到可操作手牌，数量: {GameInfoUtils.OwnHands.Count}");
                _hasOpitonToDo = true;
                return true;
            }

            LogPrintUtils.Info(Utils.GetCallingMethod(), $"已无操作可执行{_curStateTimes}");
            return false;
        }

        /// <summary>
        /// 执行游戏操作
        /// </summary>
        /// <returns></returns>
        private void ExecuteGameActions()
        {
            try
            {
                var actions = new Func<bool>[]
                {
                    () =>
                    BotUtils.Upgrade(),
                    BotUtils.BuyCard,
                    BotUtils.SellCardWhenFull,
                    BotUtils.PlayCard,
                    BotUtils.Refresh,
                    BotUtils.UseHeroPower
                };

                foreach (var action in actions)
                {
                    if (!action.Invoke()) continue;

                    LogPrintUtils.Info(Utils.GetCallingMethod(), $"{action.Method.Name} <== 成功");
                    return;
                }

                // 无操作可执行
                if (!BotUtils.HandCardCanPlay())
                {
                    LogPrintUtils.Info(Utils.GetCallingMethod(), "等待回合结束...");
                    _hasOpitonToDo = false;
                }
            }
            catch (NullReferenceException ex)
            {
                LogPrintUtils.Exception(Utils.GetCallingMethod(), ex);
                LogPrintUtils.Info(Utils.GetCallingMethod(), "状态异常，切换到对战回合");
                _nextState = GameBattleState.BATTLE_TURN;
            }
        }

        /// <summary>
        /// 战旗对战-战斗回合
        /// </summary>
        /// <returns></returns>
        private async UniTask BattleTurn()
        {
            _nextState = GameBattleState.BATTLE_TURN;
            LogPrintUtils.Info(Utils.GetCallingMethod(), "进入战斗回合");
            gameInfo.AllRunTime += (long)HsBgAI.stopwatch.Elapsed.TotalSeconds;
            gameInfo = GetGameInfo();
            // 标记需要保存
            MarkDirty();
            HsBgAI.stopwatch.Restart();
            var (isCanceled, _) = await AsyncUtil.WaitForAsync(async UniTask<bool> () =>
            {
                if (GameInfoUtils.IsBattlegroundShoppingPhase)
                {
                    LogPrintUtils.Info("HsBgAI.Ai.ProcessTree.BattleTurn".PadRight(40), "切换到我方回合状态");
                    _nextState = GameBattleState.OWN_TURN;
                    _hasOpitonToDo = true;
                    await UniTask.Delay(3000);
                    return true;
                }
                if (GameState.Get().IsGameOverNowOrPending())
                {
                    LogPrintUtils.Info("HsBgAI.Ai.ProcessTree.BattleTurn".PadRight(40), "切换到游戏结束状态");
                    _nextState = GameBattleState.GAME_OVER;
                    return true;
                }
                return false;
            }, TimeSpan.FromMinutes(2)).SuppressCancellationThrow();
            if (isCanceled)
            {
                LogPrintUtils.Info(Utils.GetCallingMethod(), "战斗回合方法超时");
            }
        }

        /// <summary>
        /// 游戏结束
        /// </summary>
        /// <returns></returns>
        private async UniTask GameOver()
        {
            _nextState = GameBattleState.GAME_OVER;
            LogPrintUtils.Info(Utils.GetCallingMethod(), "游戏结束");
            await UniTask.Delay(3000);
            LogPrintUtils.Info(Utils.GetCallingMethod(), $"matchFirstRecord: {HsBgAI.matchFirstRecord}");
            var (isCanceled, hitbox) = await AsyncUtil.WaitForSync(
                () => EndGameScreen.Get()?.m_hitbox,
                TimeSpan.FromSeconds(30)).SuppressCancellationThrow();
            if (hitbox != null)
            {
                // 记录一些信息
                if (HsBgAI.matchFirstRecord)
                {
                    HsBgAI.matchFirstRecord = false;
                    gameInfo.AllRunTime += (long)HsBgAI.stopwatch.Elapsed.TotalSeconds;
                    gameInfo = GetGameInfo();
                    // 标记需要保存
                    MarkDirty();
                    HsBgAI.stopwatch.Restart();
                }
                await UniTask.Delay(3000);

                // 这 3 秒里结算界面可能已经关闭（组件被 Unity 销毁），
                // 直接点旧的 m_hitbox 会在 PegUIElement.IsScrolling() 里抛
                // NullReferenceException（Component.get_gameObject）。
                // 因此延迟后重新取一次，并确认对象仍然有效再点击。
                try
                {
                    var clickTarget = EndGameScreen.Get()?.m_hitbox;
                    if (clickTarget != null && clickTarget.isActiveAndEnabled)
                    {
                        clickTarget.TriggerPress();
                        clickTarget.TriggerRelease();
                    }
                    else
                    {
                        LogPrintUtils.Info(Utils.GetCallingMethod(), "结算按钮已失效，跳过点击");
                    }
                }
                catch (Exception ex)
                {
                    LogPrintUtils.Exception(Utils.GetCallingMethod(), ex);
                }
            }
            if (isCanceled)
            {
                LogPrintUtils.Info(Utils.GetCallingMethod(), "游戏结束方法超时");
            }
            else
            {
                // 等待游戏离开EndGameScreen
                await UniTask.Delay(3000);
            }
            // 刷新未完成的任务
            await BotUtils.RefreshUnfinishedQuest();
            if (enableClaimReward.Value)
            {
                // 领取成就奖励
                await BotUtils.ClaimAchievementReward();
                // 领取战令奖励
                await BotUtils.ClaimBattlePassReward();
            }
            LogPrintUtils.Info(Utils.GetCallingMethod(), "切换到无效状态");
            _nextState = GameBattleState.INVALID;
        }

        /// <summary>
        /// 游戏报错
        /// </summary>
        /// <returns></returns>
        private void FatalError()
        {
            LogPrintUtils.Info(Utils.GetCallingMethod(), "游戏出现错误");
            HsBgAI.updateRequired = ReconnectMgr.Get().UpdateRequired;
            if (HsBgAI.updateRequired)
                Utils.SaveErrorJsonData();
            Thread.Sleep(11451);
            Utils.Quit(114514);
        }

        /// <summary>
        /// 获取游戏信息
        /// </summary>
        /// <returns></returns>
        public GameInfo GetGameInfo()
        {
            GameInfo newObj = new GameInfo();
            NetCache netCacheData = NetCache.Get();
            var gold = (int)(ServiceManager.Get<CurrencyManager>()?.GetBalance(CurrencyType.GOLD) ?? 0);
            if (netCacheData != null)
            {
                newObj.Collection = $"金币({gold}) 粉尘({netCacheData.GetArcaneDustBalance()}) 门票({netCacheData.GetArenaTicketBalance()})";
            }
            RewardTrackManager rewardTrackManager = RewardTrackManager.Get();
            if (rewardTrackManager != null)
            {
                RewardTrack rewardTrack = rewardTrackManager.GetRewardTrack(Assets.Global.RewardTrackType.GLOBAL);
                if (rewardTrack != null)
                {
                    newObj.Level = rewardTrack.TrackDataModel.Level;
                    newObj.Xp = rewardTrack.TrackDataModel.Xp;
                    newObj.XpNeeded = rewardTrack.TrackDataModel.XpNeeded;
                }
            }
            newObj.StandardInfo = BotUtils.GetRankInfo(VisualsFormatType.VFT_STANDARD);
            newObj.WildInfo = BotUtils.GetRankInfo(VisualsFormatType.VFT_WILD);
            newObj.TwistInfo = BotUtils.GetRankInfo(VisualsFormatType.VFT_TWIST);
            if (newObj.Level > 0)
            {
                int total;
                if (newObj.Level > 130)
                    total = GameInfoUtils.AllNeedXpArr[129] + (newObj.Level - 130) * 1500 + newObj.Xp;
                else
                    total = GameInfoUtils.AllNeedXpArr[newObj.Level - 1] + newObj.Xp;
                newObj.AllRunTime = gameInfo.AllRunTime;
                newObj.AllGetXp = gameInfo.AllGetXp;
                newObj.PerHourXp = gameInfo.PerHourXp <= 10 ? 500 : gameInfo.PerHourXp;
                newObj.AllXp = total;
                newObj.FullXpNeeded = newObj.AllXpNeeded - newObj.AllXp;
                int hour = newObj.FullXpNeeded / (gameInfo.PerHourXp <= 10 ? 500 : gameInfo.PerHourXp);
                newObj.FullTimeNeeded = hour == 0 ? "恭喜满级" : $"{hour / 24}天{hour % 24}小时";
                TimeSpan ts1 = DateTime.Now - Convert.ToDateTime(PluginInfo.GAME_VERSION_START_TIME);
                TimeSpan ts2 = Convert.ToDateTime(PluginInfo.GAME_VERSION_END_TIME) - DateTime.Now;
                newObj.PassportEnd = $"{PluginInfo.CURRENT_VERSION_DURATION_STR} 已过{ts1.Days}天{ts1.Hours}小时{ts1.Minutes}分 还剩{ts2.Days}天";
                GameSaveDataManager.Get().GetSubkeyValue(GameSaveKeyId.BACON, GameSaveKeySubkeyId.BACON_HEROES_PICKED_COUNT, out List<long> HeroesPickedCount);
                GameSaveDataManager.Get().GetSubkeyValue(GameSaveKeyId.BACON, GameSaveKeySubkeyId.BACON_TOP_4_FINISHES, out long Top4Finishes);
                int top4Finishes = (int)Top4Finishes - 1;
                int rating = BaconLobbyMgr.Get()?.GetBattlegroundsActiveGameModeRating() ?? 0;
                newObj.BattlegroundsInfo = $"{rating} {HeroesPickedCount.Sum()}场 前四: {top4Finishes}";
            }
            return newObj;
        }
    }
}