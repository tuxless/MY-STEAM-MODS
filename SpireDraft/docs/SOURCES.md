# 开发参考（核对日期 2026-09-27）

源码为本项目编写；未把参考项目或游戏反编译源码复制进发布包。

- RitsuLib 作者仓库：<https://github.com/BAKAOLC/STS2-RitsuLib>
- RitsuLib v0.6.2：<https://github.com/BAKAOLC/STS2-RitsuLib/releases/tag/v0.6.2>
- 框架当前兼容目标：`build/RitsuLib.Compatibility.props`，0.107.1、0.109.0、0.110.0、0.111.0。
- RitsuLib 入门：<https://sts2-ritsulib.ritsukage.com/guide/getting-started>
- RitsuLib 设置：<https://sts2-ritsulib.ritsukage.com/guide/mod-settings>
- RitsuLib 模板：<https://github.com/NothingFumo/RitsuLibModTemplate>
- 社区奖励界面扩展参考：<https://github.com/reddots1453/STS2mod-Stats_the_Spire/blob/main/mods/sts2_community_stats/src/Patches/CardRewardScreenPatch.cs>
- 奖励界面、原版跳过语义及卡牌模型名称的接口核对：<https://github.com/frankqwang/sts2-ai/tree/main/src/Core>
- 官方工坊上传器与工作区字段：<https://github.com/megacrit/sts2-mod-uploader>
- 上传器源码 `src/UploadCommand.cs`：要求工作区根目录存在 `image.png`、`workshop.json`、`content/`，只把 `content/` 设置为 Steam 工坊内容；成功后在工作区根目录写 `mod_id.txt`。`src/ModConfig.cs` 定义 JSON 字段；`template/README.md` 说明封面限制和分支字段。
- RitsuLib 工坊依赖 ID：<https://steamcommunity.com/sharedfiles/filedetails/?id=3747602295>

评分参考阅读：

- <https://spire-codex.com/tier-list>：统计评分及偏差说明。
- <https://slaythetierlist.com/>：社区强度榜。
- <https://sts2.gg/zh/cards/tier-list>：中文卡牌说明与评级。

没有复制这些网站的整套评分。576 项初始评分以已知原版模型名称匹配：491 项为统一 3 分试用基线，85 项采用本项目设定的初始值。用户覆盖始终优先。正式版和 Beta 初始文件暂时使用相同基线，运行时和个人覆盖分开；不代表已经完成两套独立平衡评估。
