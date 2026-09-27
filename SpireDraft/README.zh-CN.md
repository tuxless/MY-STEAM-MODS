# 评分抓牌 / Spire Draft — 0.1.1 开发源码包

**当前交付不是已编译、已实测的工坊成品。** 包内没有 `SpireDraft.dll`。已完成源码、初始评分表、自动测试代码、Windows 构建脚本，以及按 Mega Crit 官方上传器组织的工作区模板；仍需游戏引用 DLL 编译及游戏内验证。不要把本源码 ZIP 或缺少 DLL 的 `workshop/` 模板直接上传。

本工具不接 AI，不联网读取评分。运行依赖 RitsuLib 0.6.2 或更高兼容版本。目标系统 Windows 11；仅单人卡牌奖励界面生效。不会自动购买商店卡牌、处理删牌/升级界面或战斗中的选牌。

## 最省事：把编译依赖发回给开发者

1. 解压本包。双击 `CollectGameReferences.cmd`。
2. 输入 `stable`（正式版）或 `beta`（测试版），回车。脚本尝试找到 Steam 游戏文件；出现多个目录时选择当前分支；找不到时粘贴包含 `sts2.dll` 的目录。
3. 把生成的 `GameReferences-分支-日期时间.zip` 附加到本次对话。它只收集 `sts2.dll`、`0Harmony.dll`、`GodotSharp.dll`、`SmartFormat.dll` 和可用的版本信息，不收集存档或账号资料。
4. 若要验证两个分支，请切换游戏分支并等待 Steam 更新完成后，再收集另一份；也可从你已有的两个版本目录分别收集。不要把依赖包上传到创意工坊。

这一步不需要安装开发工具。仅凭 RitsuLib 工坊截图无法编译游戏接口，也无法确认实机兼容性。

## 在本机编译

1. 安装 [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)（SDK，不只是 Runtime）。准备已安装的游戏。
2. 双击 `Build.cmd`，选择当前分支。首次构建会从 NuGet 下载公开编译依赖。
3. 脚本先运行 20 组核心规则/倒计时/配置测试；全部通过后编译模组。
4. 成功后得到 `dist/SpireDraft-stable/` 或 `dist/SpireDraft-beta/`。`content/` 内仅包含 `SpireDraft.dll` 和 `SpireDraft.json`；评分表嵌在 DLL 中，不需要 PCK、Godot 编辑器或额外素材包。

`workshop/` 是官方格式的**模板**，根目录有 `workshop.json`、`image.png`、`README.md`，其 `content/` 里先只有 `SpireDraft.json`。构建脚本把编译出的 DLL 和模板文件组合成可供上传器读取的 `dist/` 工作区，并执行目录、元数据、封面大小及 DLL 文件头检查。这样检查通过仍不等于游戏内兼容。

手动指定目录的命令：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build.ps1 -GameDataDir "D:\SteamLibrary\steamapps\common\Slay the Spire 2\data_sts2_windows_x86_64" -Branch stable
```

`-Branch` 选择输出目录，不替你切换游戏分支。须自行确认所选目录对应正确的游戏版本。

## 本机验证后上传

1. 在创意工坊订阅 [RitsuLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3747602295)。本包不重复分发框架。
2. 将所生成工作区 `content/` 内的两个文件复制到当前游戏加载位置的 `mods/SpireDraft/`，通过 Steam 的模组启动选项启动。
3. 按 `docs/TESTING.md` 检查自动抓牌、跳过、取消、设置保存和多人禁用。编译通过不能代替实机验证。
4. 准备 [Mega Crit 官方上传器](https://github.com/megacrit/sts2-mod-uploader)，登录 Steam。
5. 修改**`dist/SpireDraft-所选分支/`** 工作区的 `workshop.json`，填写你自己的标题/说明/可见性；修改 `content/SpireDraft.json` 的作者名。当前默认可见性为 `private`，说明中明确标记开发预览。若想让作者名在下一次重新构建后仍保留，也修改 `workshop/content/SpireDraft.json` 模板。
6. 双击 `Upload.cmd`，选择工作区分支，并输入 `ModUploader.exe` 路径。脚本调用官方 `upload -w` 命令。
7. 保留官方工具生成的 `mod_id.txt`，后续更新同一条工坊项目时继续使用原工作区。

官方上传器调用形式是 `ModUploader.exe upload -w <工作区目录>`，传入的是 `dist/SpireDraft-stable/` 或 `dist/SpireDraft-beta/` **目录**，不是 ZIP，也不是 `content/` 目录。`workshop.json` 中的数字 `3747602295` 是 RitsuLib 的 Steam 工坊项目 ID；`content/SpireDraft.json` 中的 `STS2-RitsuLib` 是游戏内模组 ID，两者职责不同。封面目前为 512×512 PNG、约 60 KB，低于官方上传器说明的 1 MB 上限。`minBranch` 和 `maxBranch` 暂不填写，按官方模板表示不限制；正式版和 Beta 的实际兼容性仍需分别验证。

| 构建后的工作区路径 | 用途 |
|---|---|
| `workshop.json` | 标题、描述、可见性、RitsuLib 工坊依赖 |
| `image.png` | 创意工坊封面 |
| `content/SpireDraft.json` | 游戏内模组信息和 RitsuLib 模组依赖 |
| `content/SpireDraft.dll` | 编译后的可运行代码 |
| `mod_id.txt` | 首次上传成功后由官方上传器创建，后续更新沿用 |
| `build-report.json` | 本地构建记录；位于 `content/` 之外，不随模组内容上传 |

本包没有上传任何内容。正式版/Beta 工作区分别维护；只有在同一 DLL 已在两者验证通过后，才可对外宣称一个工坊项目兼容两个分支。**不要同时安装两个同 ID 的本地/工坊副本。**

## 评分规则

| 分数 | 行为 |
|---|---|
| 1 | 不抓 |
| 2 | 永久牌组少于 20 张，并且未达到该卡重复上限时可抓 |
| 3 | 永久牌组少于 30 张，并且未达到该卡重复上限时可抓 |
| 4 | 忽略牌组张数，但遵守该卡重复上限 |
| 5 | 忽略牌组张数和重复上限，优先抓 |

从合格候选中选最高分；同分优先已升级，再比自定义优先级，最后按界面从左到右。多张 5 分时仍只选一张。没有合格候选且游戏提供跳过选项时调用原版跳过流程；强制选牌、未知卡牌和无效数据转为手动选择。

“牌组张数”是永久牌组总张数，包括初始牌和诅咒，不是战斗中手牌数。重复张数按卡牌 ID 合并统计，升级版和未升级版算同一张牌。

初始评分包含 576 个已知原版卡牌模型名，其中 85 个设定了专门的初始值，其余默认 3 分、最多 2 张。**这是可修改的试用基线，没有对所有卡牌逐张评估；没有使用网站胜率榜批量映射，也不是最优抓牌策略。** 列表可能包含不进入奖励池的衍生牌；它们不会因此被添加到奖励中。新增卡、未知模组牌默认暂停自动抓牌。

## 修改评分

主菜单或暂停菜单进入 RitsuLib 模组设置 → 评分抓牌。

- 总开关、自动/仅建议、倒计时、2 分与 3 分的张数阈值可调整。
- 搜索中文名称、英文模型名或卡牌 ID；正式版和 Beta 分开编辑。
- 分数填 `0` 后保存：删除个人覆盖，恢复内置评分。它不是“零分”。
- 重复上限 `-1` 表示不限制，`0` 表示该卡不再抓（5 分仍忽略上限）。
- 同分优先级越大越优先，但已升级优先于未升级。
- 奖励界面的小面板也可直接改本次出现卡牌的分数；修改后暂停本次倒计时，点击“重新评估”才恢复。

个人配置保存在 `%APPDATA%\SpireDraft\settings.json`，与工坊内容分开；更新模组不会覆盖个人评分。保存使用临时文件替换并保留 `.bak`。如果文件损坏，自动操作停止，修复后使用“重新加载”按钮；不会静默覆盖用户文件。

## 兼容范围与当前限制

- 版本分流表：`0.107.1` 为正式版；`0.109.0`、`0.110.0`、`0.111.0` 为 Beta。来源是 RitsuLib 当前兼容目标。这是**实现目标，不是已经通过的测试矩阵**。
- 使用原版 `RefreshOptions`、`SelectCard` 和 `OnAlternateRewardSelected` 接口。关键成员缺失时自动操作停止；不会尝试猜测新接口。
- 未识别的游戏版本暂停自动操作，需更新适配表并验证后发布新版。
- 多人保护同时检查原版网络模式和跑局玩家数量；一个人的多人大厅也不生效。
- 打开查看卡牌、地图、其他模态界面或暂停时不执行自动操作；返回后重新计算倒计时。
- 新游戏版本可能改变界面或数据接口，需要重新编译/验证。
- Windows Beta 上已通过核心测试、编译及工作区校验；0.1.1 的奖励界面修复尚未在游戏中验证，正式版也尚未测试。阅读 `docs/VALIDATION.md` 了解检查记录。
- 0.1.1 修复了 RitsuLib 0.6.2 的动态补丁调用：旧版日志显示补丁 `0 total`，虽已载入设置页，却不会在奖励界面挂载面板。新版本应记录 `Dynamic patch application complete: 4/4 succeeded`；若仍未显示，请提供新的 `godot.log` 中 `[SpireDraft]` 行。

## 文件结构

- `src/Core/`：独立评分、倒计时、配置逻辑。
- `src/GameApi.cs`：限制范围的原版接口适配。
- `src/RewardSession.cs`：奖励面板、倒计时和执行。
- `src/SettingsPage.cs`：RitsuLib 设置页和评分编辑器。
- `data/`：正式版与 Beta 初始评分表。
- `tests/`：可在 .NET 9 SDK 下运行的核心测试。
- `workshop/`：官方上传器工作区模板，包含根目录工坊配置、封面及 `content/` 内的模组清单。
- `scripts/`：收集引用、编译、上传脚本。

作者显示名目前为 `SpireDraft`，发布前可替换成你的工坊昵称。
