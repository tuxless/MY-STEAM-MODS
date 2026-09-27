# 当前验证状态

此源码包没有已编译的 `SpireDraft.dll`，也没有已上传的工坊项目。

- 已核对：RitsuLib 0.6.2 公共源码、设置和动态补丁接口、当前兼容目标。
- 已核对：公开社区模组对奖励界面 RefreshOptions / SelectCard 的使用，以及原版 Skip 选项的标识和回调顺序。
- 已编写：20 组 C# 自动测试，覆盖阈值边界、跳过、多人与未知牌保护、同分排序、重复上限、一次性倒计时、遮挡重置、配置保存与损坏保护。
- 用户在 Windows 的 Beta 构建中运行了 20 组核心测试，均通过。第一次模组编译报 `Entry.cs` 的 `Environment` 类型歧义（CS0104）；显式指定 `System.Environment` 后，第二次构建已成功生成 DLL。
- 第二次 Beta 构建已成功生成 DLL，随后工作区校验因 Windows PowerShell 默认编码读取中文 JSON 失败；显式指定 UTF-8 后第三次构建和校验均通过。
- 用户第三次 Beta 构建的 20 项测试、DLL 编译和官方上传工作区校验均通过。游戏日志显示已加载 SpireDraft 和 RitsuLib 0.6.2，游戏版本 v0.111.0，但 `reward-screen` 补丁为 `0 applied, 0 total`；普通战斗奖励未出现面板。对照 RitsuLib v0.6.2 源码确认 `PatchAll` 只执行静态补丁；0.1.1 改为 `ApplyDynamicPatches`，尚待用户重新编译并在游戏内验证。
- 当前执行环境没有游戏引用 DLL，无法在这里独立编译；另行准备的 .NET SDK 在此受限环境无法启动 CoreCLR。
- 尚未完成：0.1.1 的 Godot 奖励界面验证、正式版游戏内测试、Steam 上传。
- 已核对 Mega Crit 官方上传器的工作区根目录、`content/`、元数据字段和 `mod_id.txt` 处理。`scripts/ValidateWorkshop.ps1` 已在用户的 Windows Beta 构建中通过。

`Build.cmd` 会在你的 Windows 上先执行测试，成功后才编译并生成工作区。失败不会声称生成可上传成品。编译成功后，`build-report.json` 仍保留 `gameplayTest: not yet performed`，须按 `TESTING.md` 完成实机验证。

结构和语法静态检查结果见 `static-validation.json`。它们不代替 C# 类型检查或实际运行。
