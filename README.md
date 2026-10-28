# HsBgAI

炉石传说**酒馆战棋自动对战**插件，基于 BepInEx 5 开发（net48 / x64）。

插件接管从「寻找对局 → 选择英雄 → 挂机对战 → 结算收尾」的整条流程：按 Firestone 的胜率数据挑英雄、
托管每个回合的操作，对局结束后自动刷新任务和领取奖励。

配合 [HsBot](https://github.com/kanshufan-1107/HsBot) 中控使用可以多账号批量挂机；也可以用 HsBot 的「安装插件」按钮一键部署。

---

## 功能

| 功能 | 说明 |
|---|---|
| 自动开局 | 自动寻找并进入酒馆战棋对局，可设定挂机时间段（由中控写入） |
| 自动选英雄 | 用 Firestone 的英雄/种族胜率数据打分选最优英雄 |
| 挂机对战 | 托管回合操作，稳定刷战棋通行证经验 |
| 前四摆烂 | 进入前四后放弃继续操作，减少耗时与掉分风险 |
| 维持分段 | 低于设定分段时正常打，达到分段后主动投降保分 |
| 结算收尾 | 自动刷新未完成任务、领取成就奖励、领取战令奖励 |
| 版本周期 | 每天 05:00 抓取官网资料片页面，刷新当前资料片起止日期 |
| 帮助覆盖层 | 游戏内按 `F10` 显示快捷键与状态说明 |

## 环境要求

- Windows + 炉石传说（国服 / 亚服 / 欧服 / 美服，游戏语言 zhCN）
- **BepInEx 5.4.x（64 位）**
- `HsFirestone.Models.dll`：本插件的依赖，**必须与 `HsBgAI.dll` 放在同一目录**
- 可选：`HsMod.dll`（游戏内增强，和本插件互相独立）

## 安装

1. 在炉石安装目录装好 BepInEx 5（存在 `winhttp.dll`、`doorstop_config.ini`、`BepInEx\`）；
2. 把 `HsBgAI.dll` 和 `HsFirestone.Models.dll` 复制到：

   ```
   {炉石目录}\BepInEx\plugins\
   ```

3. 启动游戏，在 `BepInEx\LogOutput.log` 里看到下面几行即加载成功：

   ```
   [Info   :   BepInEx] Loading [HsBgAI 1.0.0.0]
   [Info   :    HsBgAI] 插件 HsBgAI 加载成功......
   [Info   :    HsBgAI] 插件 HsBgAI 启动成功......
   ```

> 用 HsBot 中控的「安装插件」按钮会自动完成上述步骤。

## 配置

配置文件：`{炉石目录}\BepInEx\config\HsBgAI.cfg`（首次运行自动生成）

| 配置节 | 键 | 默认值 | 说明 |
|---|---|---|---|
| `HsBgAI配置` | `EnablePlugin` | `true` | 是否启用插件 |
| `HsBgAI配置` | `EnableAutoRun` | `false` | 是否自动开始挂机 |
| `HsBgAI配置` | `EnableClaimReward` | `false` | 结算后自动领取成就 / 战令奖励 |
| `战旗配置` | `LazzyAfterTop4` | `false` | 前四摆烂 |
| `战旗配置` | `BattlegroundsLevelCap` | `-1` | 维持分段，`-1` 表示不限制 |

这些值由 HsBot 中控在启动账号时自动写入；手动改也可以，但下次中控启动会被覆盖。

## 日志与排查

| 内容 | 位置 |
|---|---|
| 插件运行日志 | `{炉石目录}\BepInEx\LogOutput.log`（BepInEx 会轮转为 `LogOutput.log.1`…） |
| 对局记录 | `{炉石目录}\BepInEx\HsAI\GameLog\{hsunitid}\MatchInfo.log` |
| 错误归档 | HsBot 中控会把日志里的 `[Error]` 按实例和日期整理到 `ErrorLogs\{hsunitid}\{日期}.log` |

日志里的 `DBF Asset has 0 records!`、`NetCache... is null`、`SubSpellController...` 之类都来自炉石客户端自身，
与本插件无关，不影响挂机。

## 构建

```powershell
dotnet build HsBgAI.sln -c Release
```

产物：`HsBgAI\bin\Release\net48\HsBgAI.dll`

构建前需要把游戏程序集放进 `HsBgAI\LibHearthstone\`（`Assembly-CSharp.dll`、`UnityEngine.*.dll`、
`blizzard.*.dll` 等，仓库不包含这些文件），否则引用解析会失败。

## 目录结构

```
HsBgAI/
├── Ai/
│   └── MainProcess.cs        状态机主流程（找局 / 选英雄 / 回合 / 结算）
├── Enums/                    GameBattleState、ConcedeState
├── Firestone/
│   └── FirestoneClient.cs    拉取并缓存 Firestone 的胜率数据
├── Helpers/
│   └── HelpOverlay.cs        F10 帮助覆盖层
├── Mapping/
│   └── GameInfo.cs           对局信息模型
├── Patchers/                 运行时补丁（介绍弹窗、通行证经验记录）
├── Tools/                    工具类（日志、异步、通用操作、版本周期）
├── LibHearthstone/           游戏程序集引用（需自备，不纳入版本库）
├── PluginConfig.cs           配置项定义
└── PluginInfo.cs             GUID / 名称 / 版本
```

## 许可

见仓库根目录的 [LICENSE](LICENSE)。
