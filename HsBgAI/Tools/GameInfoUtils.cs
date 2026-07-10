namespace HsBgAI.Tools
{
    public class GameInfoUtils
    {
        public static int[] AllNeedXpArr => GameConstants.AllNeedXpArr;
        public static int[] LevelExperienceThresholds => GameConstants.LevelExperienceThresholds;
        /// <summary>
        /// 是否在游戏中
        /// </summary>
        public static bool IsPlaying => SceneMgr.Get()?.GetMode() == SceneMgr.Mode.GAMEPLAY;
        /// <summary>
        /// 是否为购物阶段
        /// </summary>
        public static bool IsBattlegroundShoppingPhase => ZoneMgr.Get().IsBattlegroundShoppingPhase() && FreezeButtonCard != null;
        /// <summary>
        /// 是否为对战状态
        /// </summary>
        public static bool IsBattlegroundBattlePhase => ZoneMgr.Get().IsBattlegroundBattlePhase() && FreezeButtonCard == null;
        /// <summary>
        /// 冰冻按钮卡牌
        /// </summary>
        public static Card FreezeButtonCard => (GameState.Get()?.GetGameEntity() as TB_BaconShop)?.GetFreezeButtonCard();
        /// <summary>
        /// 刷新按钮卡牌
        /// </summary>
        public static Card RefreshButtonCard => (GameState.Get()?.GetGameEntity() as TB_BaconShop)?.GetRefreshButtonCard();
        /// <summary>
        /// 升级按钮卡牌
        /// </summary>
        public static Card UpgradeButtonCard => (GameState.Get()?.GetGameEntity() as TB_BaconShop)?.GetTavernUpgradeButtonCard();
        /// <summary>
        /// 己方英雄酒馆等级
        /// </summary>
        public static int OwnTavernTier => GameState.Get()?.GetFriendlySidePlayer()?.GetTag(GAME_TAG.PLAYER_TECH_LEVEL) ?? 0;
        /// <summary>
        /// 当前回合数
        /// </summary>
        public static int TurnCounter => GameState.Get()?.GetGameEntity()?.GetTag(GAME_TAG.TURN) ?? 0;
        /// <summary>
        /// 战旗回合数
        /// </summary>
        public static int BattleTurnCounter => (TurnCounter + 1) / 2;
        /// <summary>
        /// 己方场面随从
        /// </summary>
        public static List<Card> OwnMinions => GameState.Get()?.GetFriendlySidePlayer()?.GetBattlefieldZone()?.GetCards();
        /// <summary>
        /// 敌方场面随从
        /// </summary>
        public static List<Card> EnemyMinions => GameState.Get()?.GetOpposingSidePlayer()?.GetBattlefieldZone()?.GetCards();
        /// <summary>
        /// 己方手牌
        /// </summary>
        public static List<Card> OwnHands => GameState.Get()?.GetFriendlySidePlayer()?.GetHandZone()?.GetCards();
        /// <summary>
        /// 敌方手牌
        /// </summary>
        public static List<Card> EnemyHands => GameState.Get()?.GetOpposingSidePlayer()?.GetHandZone()?.GetCards();
        /// <summary>
        /// 己方武器
        /// </summary>
        public static Entity OwnWeapon => GameState.Get()?.GetFriendlySidePlayer()?.GetWeaponCard()?.GetEntity();
        /// <summary>
        /// 敌方武器
        /// </summary>
        public static Entity EnemyWeapon => GameState.Get()?.GetOpposingSidePlayer()?.GetWeaponCard()?.GetEntity();
        /// <summary>
        /// 己方英雄技能
        /// </summary>
        public static Card OwnHeroAbility => GameState.Get()?.GetFriendlySidePlayer()?.GetHeroPowerCard();
        /// <summary>
        /// 敌方英雄技能
        /// </summary>
        public static Card EnemyHeroAbility => GameState.Get()?.GetOpposingSidePlayer()?.GetHeroPowerCard();
        /// <summary>
        /// 己方玩家
        /// </summary>
        public static Player OwnPlayer => GameState.Get()?.GetFriendlySidePlayer();
        /// <summary>
        /// 敌方玩家
        /// </summary>
        public static Player EnemyPlayer => GameState.Get()?.GetOpposingSidePlayer();
        /// <summary>
        /// 己方英雄
        /// </summary>
        public static Entity OwnHero => GameState.Get()?.GetFriendlySidePlayer()?.GetHero();
        /// <summary>
        /// 敌方英雄
        /// </summary>
        public static Entity EnemyHero => GameState.Get()?.GetOpposingSidePlayer()?.GetHero();
        /// <summary>
        /// 己方可用法力值
        /// </summary>
        public static int OwnMana => GameState.Get()?.GetFriendlySidePlayer()?.GetNumAvailableResources() ?? 0;
        /// <summary>
        /// 敌方可用法力值
        /// </summary>
        public static int EnemyMana => GameState.Get()?.GetOpposingSidePlayer()?.GetNumAvailableResources() ?? 0;
        /// <summary>
        /// 己方最大法力值
        /// </summary>
        public static int OwnMaxMana => GameState.Get()?.GetFriendlySidePlayer()?.GetTag(GAME_TAG.RESOURCES) ?? 0;
        /// <summary>
        /// 敌方最大法力值
        /// </summary>
        public static int EnemyMaxMana => GameState.Get()?.GetOpposingSidePlayer()?.GetTag(GAME_TAG.RESOURCES) ?? 0;
        /// <summary>
        /// 己方疲劳值
        /// </summary>
        public static int OwnFatigue => GameState.Get()?.GetFriendlySidePlayer()?.GetTag(GAME_TAG.FATIGUE) ?? 0;
        /// <summary>
        /// 敌方疲劳值
        /// </summary>
        public static int EnemyFatigue => GameState.Get()?.GetOpposingSidePlayer()?.GetTag(GAME_TAG.FATIGUE) ?? 0;
        /// <summary>
        /// 己方抽牌数
        /// </summary>
        public static int OwnDrawnCards => GameState.Get()?.GetFriendlySidePlayer()?.GetTag(GAME_TAG.NUM_CARDS_DRAWN_THIS_TURN) ?? 0;
        /// <summary>
        /// 敌方抽牌数
        /// </summary>
        public static int EnemyDrawnCards => GameState.Get()?.GetOpposingSidePlayer()?.GetTag(GAME_TAG.NUM_CARDS_DRAWN_THIS_TURN) ?? 0;
        /// <summary>
        /// 己方尸体数量
        /// </summary>
        public static int OwnCorpses => GameState.Get()?.GetFriendlySidePlayer()?.GetNumAvailableCorpses() ?? 0;
        /// <summary>
        /// 敌方尸体数量
        /// </summary>
        public static int EnemyCorpses => GameState.Get()?.GetOpposingSidePlayer()?.GetNumAvailableCorpses() ?? 0;
        /// <summary>
        /// 己方当回合已打出卡牌数
        /// </summary>
        public static int CardsPlayedThisTurn => GameState.Get()?.GetFriendlySidePlayer()?.GetTag(GAME_TAG.NUM_CARDS_PLAYED_THIS_TURN) ?? 0;
        /// <summary>
        /// 己方当回合已打出随从数
        /// </summary>
        public static int MinionsPlayedThisTurn => GameState.Get()?.GetFriendlySidePlayer()?.GetTag(GAME_TAG.NUM_MINIONS_PLAYED_THIS_TURN) ?? 0;
        /// <summary>
        /// 己方过载法力值
        /// </summary>
        public static int OverloadOwed => GameState.Get()?.GetFriendlySidePlayer()?.GetTag(GAME_TAG.OVERLOAD_OWED) ?? 0;
        /// <summary>
        /// 己方过载锁定法力值
        /// </summary>
        public static int OverloadLocked => GameState.Get()?.GetFriendlySidePlayer()?.GetTag(GAME_TAG.OVERLOAD_LOCKED) ?? 0;
        /// <summary>
        /// 己方卡组
        /// </summary>
        public static List<Entity> OwnDeck => GameState.Get()?.GetFriendlySidePlayer()?.GetDeckZone()?.GetCards()?.Select(card => card.GetEntity()).ToList() ?? new List<Entity>();
        /// <summary>
        /// 敌方卡组
        /// </summary>
        public static List<Entity> EnemyDeck => GameState.Get()?.GetOpposingSidePlayer()?.GetDeckZone()?.GetCards()?.Select(card => card.GetEntity()).ToList() ?? new List<Entity>();
        /// <summary>
        /// 己方奥秘
        /// </summary>
        public static List<Entity> OwnSecrets => GameState.Get()?.GetFriendlySidePlayer()?.GetSecretZone()?.GetCards()?.Select(card => card.GetEntity()).ToList() ?? new List<Entity>();
        /// <summary>
        /// 敌方奥秘
        /// </summary>
        public static List<Entity> EnemySecrets => GameState.Get()?.GetOpposingSidePlayer()?.GetSecretZone()?.GetCards()?.Select(card => card.GetEntity()).ToList() ?? new List<Entity>();
        /// <summary>
        /// 己方墓地
        /// </summary>
        public static List<Entity> OwnGraveyards => GameState.Get()?.GetFriendlySidePlayer()?.GetGraveyardZone()?.GetCards()?.Select(card => card.GetEntity()).ToList() ?? new List<Entity>();
        /// <summary>
        /// 敌方墓地
        /// </summary>
        public static List<Entity> EnemyGraveyards => GameState.Get()?.GetOpposingSidePlayer()?.GetGraveyardZone()?.GetCards()?.Select(card => card.GetEntity()).ToList() ?? new List<Entity>();
        /// <summary>
        /// 当前回合数
        /// </summary>
        public static int CurrTurn => GameState.Get()?.GetTurn() ?? 0;

        /// <summary>
        /// 转化战旗种族
        /// </summary>
        /// <param name="races"></param>
        /// <returns></returns>
        public static List<string> ConvertRaceCN(List<TAG_RACE> races)
        {
            List<string> racesCN = new List<string>();
            foreach (var race in races)
            {
                switch (race)
                {
                    case TAG_RACE.UNDEAD:
                        racesCN.Add("亡灵");
                        break;
                    case TAG_RACE.MURLOC:
                        racesCN.Add("鱼人");
                        break;
                    case TAG_RACE.DEMON:
                        racesCN.Add("恶魔");
                        break;
                    case TAG_RACE.MECHANICAL:
                        racesCN.Add("机械");
                        break;
                    case TAG_RACE.ELEMENTAL:
                        racesCN.Add("元素");
                        break;
                    case TAG_RACE.BEAST:
                        racesCN.Add("野兽");
                        break;
                    case TAG_RACE.PIRATE:
                        racesCN.Add("海盗");
                        break;
                    case TAG_RACE.DRAGON:
                        racesCN.Add("龙");
                        break;
                    case TAG_RACE.QUILBOAR:
                        racesCN.Add("野猪人");
                        break;
                    case TAG_RACE.NAGA:
                        racesCN.Add("娜迦");
                        break;
                    default:
                        racesCN.Add("未知战旗种族");
                        break;
                }
            }
            return racesCN;
        }

        /// <summary>
        /// 获取购买或出售的卡牌
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public static List<Card> GetBuyOrSellCards(int slot)
        {
            List<ZoneMoveMinionHoverTarget> list = ZoneMgr.Get().FindZonesOfType<ZoneMoveMinionHoverTarget>(Player.Side.FRIENDLY);
            if (list == null || list.Count == 0)
                return null;
            List<ZoneMoveMinionHoverTarget> slotList = [.. list.Where(zone => zone.m_Slot == slot)];
            List<Card> moveMinionHoverTargets = new List<Card>();
            slotList.ForEach(delegate (ZoneMoveMinionHoverTarget z)
            {
                moveMinionHoverTargets.AddRange(z.GetCards());
            });
            return moveMinionHoverTargets;
        }

        /// <summary>
        /// 根据卡牌获取选项索引
        /// </summary>
        /// <param name="options"></param>
        /// <param name="card"></param>
        /// <returns></returns>
        public static int GetOptionIndexFromCard(List<Network.Options.Option> options, Card card)
        {
            for (int i = 0; i < options.Count; i++)
            {
                var main = options[i].Main;
                if (main.ID == card.GetEntity().GetEntityId())
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 选择目标
        /// </summary>
        /// <param name="option"></param>
        /// <param name="targetEntityId"></param>
        /// <param name="subOptionId"></param>
        /// <param name="targetSelector"></param>
        /// <returns></returns>
        public static bool PlayOption(Network.Options.Option option, out int targetEntityId, out int subOptionId,
            Func<List<Card>, (bool, Card)> targetSelector)
        {
            targetEntityId = 0;
            subOptionId = -1;
            // 这个牌不能用
            if (option.Main.PlayErrorInfo.PlayError != PlayErrors.ErrorType.NONE)
                return false;
            // 如果有subOption
            if (option.Subs != null && option.Subs.Count != 0)
            {
                for (int i = 0; i < option.Subs.Count; i++)
                {
                    subOptionId = i;
                    // 如果不需要目标，直接释放
                    if (option.Subs[i].Targets.Count == 0)
                        return true;
                    foreach (var target in option.Subs[i].Targets)
                    {
                        if (option.Subs[i].IsValidTarget(target.ID))
                        {
                            targetEntityId = target.ID;
                            return true;
                        }
                    }
                }
                return false;
            }
            List<Card> targets = new List<Card>();
            foreach (Network.Options.Option.TargetOption target in option.Main.Targets)
            {
                if (!option.Main.IsValidTarget(target.ID))
                    continue;
                Card card = GameState.Get().GetEntity(target.ID).GetCard();
                if (card == null)
                    continue;

                targets.Add(card);
            }
            var (isValid, selectedCard) = targetSelector(targets);
            targetEntityId = selectedCard?.GetEntity()?.GetEntityId() ?? 0;
            return isValid;
        }

        /// <summary>
        /// 根据英雄技能选择目标
        /// </summary>
        /// <param name="targets"></param>
        /// <returns></returns>
        public static Card GetBestTargetByUseHeroPower(List<Card> targets)
        {
            if (targets == null || targets.Count == 0)
                return null;
            // 根据对应英雄技能选择对应的目标
            var heroPower = GameState.Get().GetFriendlySidePlayer()?.GetHeroPowerCard();
            if (heroPower == null)
                return targets.FirstOrDefault();
            switch (heroPower.GetEntity().GetCardId())
            {
                case "TB_BaconShop_HP_010":// 堕落的乔治
                    // 选择攻击+血量最高的己方随从
                    return targets.Where(x => x.GetControllerSide() == Player.Side.FRIENDLY)
                        .OrderByDescending(x => x.GetEntity().GetRealTimeAttack() + x.GetEntity().GetHealth()).FirstOrDefault();
                case "TB_BaconShop_HP_046":// 雷诺·杰克逊
                    // 选择星级最高的随从
                    return targets.OrderByDescending(x => x.GetEntity().GetTechLevel()).FirstOrDefault();
                case "BG21_HERO_020p":// 厨师曲奇
                    return targets.Where(x => x.GetControllerSide() == Player.Side.OPPOSING)
                        .OrderByDescending(x => x.GetEntity().GetTechLevel()).FirstOrDefault();
                case "BG20_HERO_101p":// 泽瑞拉
                case "TB_BaconShop_HP_075":// 钩牙船长
                case "TB_BaconShop_HP_052":// 玛里苟斯
                case "TB_BaconShop_HP_011":// 迦拉克隆
                case "TB_BaconShop_HP_049":// 巫妖巴兹亚尔
                case "TB_BaconShop_HP_702":// 典狱长
                case "TB_BaconShop_HP_014":// 辛达苟萨
                case "BG20_HERO_102p":// 萨鲁法尔大王
                case "TB_BaconShop_HP_068"://玛维·影歌
                case "TB_BaconShop_HP_036":// 加拉克苏斯大王
                    return targets.OrderByDescending(x => x.GetEntity().GetTechLevel()).FirstOrDefault();
                case "TB_BaconShop_HP_001":// 艾德温·范克里夫
                case "BG21_HERO_030p":// 斯尼德
                    return targets.Where(x => x.GetControllerSide() == Player.Side.FRIENDLY)
                        .OrderByDescending(x => x.GetEntity().GetTechLevel()).FirstOrDefault();
                case "TB_BaconShop_HP_024":// 巫妖王
                case "BG25_HERO_103p": // 塔隆·血魔
                    var deathrattle = targets.Where(x => x.GetControllerSide() == Player.Side.FRIENDLY && x.GetEntity().HasDeathrattle()).ToList();
                    if (deathrattle.Count > 0)
                        return deathrattle.OrderByDescending(x => x.GetEntity().GetTechLevel()).FirstOrDefault();
                    return targets.Where(x => x.GetControllerSide() == Player.Side.FRIENDLY)
                        .OrderByDescending(x => x.GetEntity().GetTechLevel()).FirstOrDefault();
                case "BG20_HERO_301p"://吞噬者穆坦努斯
                    // 先按星级降序，再按攻击+血量降序（原来第二个 OrderByDescending 会完全覆盖第一个，改用 ThenByDescending）
                    return targets.OrderByDescending(x => x.GetEntity().GetTechLevel()).ThenByDescending(x => x.GetEntity().GetRealTimeAttack() + x.GetEntity().GetHealth()).FirstOrDefault();
                case "BG20_HERO_201p":// 沃金
                    return targets.OrderByDescending(x => x.GetEntity().GetRealTimeAttack()).FirstOrDefault();
                case "TB_BaconShop_HP_040":// 疯狂金字塔
                    return targets.Where(x => x.GetControllerSide() == Player.Side.FRIENDLY).OrderByDescending(x => x.GetEntity().GetRealTimeAttack()).FirstOrDefault();
                case "BG26_HERO_102p":// 因葛，钢铁颂歌
                    return targets.Where(x => x.GetControllerSide() == Player.Side.FRIENDLY).OrderByDescending(x => x.GetEntity().GetHealth()).FirstOrDefault();
                case "BG26_HERO_102p2":// 因葛，钢铁颂歌
                    return targets.Where(x => x.GetControllerSide() == Player.Side.FRIENDLY).OrderByDescending(x => x.GetEntity().GetRealTimeAttack()).FirstOrDefault();
                case "TB_BaconShop_HP_084":// 詹迪斯·巴罗夫
                    // 先按星级升序，再按攻击+血量升序（原来第二个 OrderBy 会完全覆盖第一个，改用 ThenBy）
                    return targets.OrderBy(x => x.GetEntity().GetTechLevel()).ThenBy(x => x.GetEntity().GetRealTimeAttack() + x.GetEntity().GetHealth()).FirstOrDefault();
                default:
                    return targets.FirstOrDefault();
            }
        }

        /// <summary>
        /// 判断卡牌是否可以使用
        /// </summary>
        /// <param name="card"></param>
        /// <returns></returns>
        public static bool CardIsCanBeUsed(Card card)
        {
            Actor actor = card.GetActor();
            if (actor != null)
            {
                if (actor.GetActorStateType() != ActorStateType.CARD_PLAYABLE && actor.GetActorStateType() != ActorStateType.CARD_COMBO && actor.GetActorStateType() != ActorStateType.CARD_PLAYABLE_MOUSE_OVER)
                {
                    return actor.GetActorStateType() == ActorStateType.CARD_COMBO_MOUSE_OVER;
                }
                return true;
            }
            return false;
        }
    }
}