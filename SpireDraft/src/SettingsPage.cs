using Godot;
using SpireDraft.Core;
using STS2RitsuLib;
using STS2RitsuLib.Settings;

namespace SpireDraft;

internal static class SettingsPage
{
    private static ModSettingsText T(string text) => ModSettingsText.Literal(text);
    private static IModSettingsValueBinding<TValue> Bind<TValue>(string id,
        Func<Configuration, TValue> read, Action<Configuration, TValue> write) =>
        ModSettingsBindings.Callback(Entry.ModId, id, () => read(Entry.Store.Value),
            value => Entry.Store.Change(c => write(c, value)), () => { });

    internal static void Register()
    {
        RitsuLibFramework.RegisterModSettings(Entry.ModId, page => page
            .WithTitle(T("评分抓牌"))
            .WithModDisplayName(T("评分抓牌 / Spire Draft"))
            .AddSection("general", section => section.WithTitle(T("自动抓牌"))
                .AddToggle("enabled", T("启用评分抓牌"), Bind("enabled", c => c.Enabled, (c, v) => c.Enabled = v))
                .AddToggle("automatic", T("自动选牌和跳过（关闭后只显示建议）"),
                    Bind("automatic", c => c.Automatic, (c, v) => c.Automatic = v))
                .AddIntSlider("delay", T("操作前倒计时（秒）"), Bind("delay", c => c.DelaySeconds, (c, v) => c.DelaySeconds = v), 1, 30)
                .AddIntSlider("limit2", T("2 分牌：牌组达到此张数后不抓"),
                    Bind("limit2", c => c.TwoStarDeckLimit, (c, v) => { c.TwoStarDeckLimit = v; c.ThreeStarDeckLimit = Math.Max(v, c.ThreeStarDeckLimit); }), 1, 200)
                .AddIntSlider("limit3", T("3 分牌：牌组达到此张数后不抓"),
                    Bind("limit3", c => c.ThreeStarDeckLimit, (c, v) => { c.ThreeStarDeckLimit = v; c.TwoStarDeckLimit = Math.Min(v, c.TwoStarDeckLimit); }), 1, 200)
                .AddButton("folder", T("本机评分配置"), T("打开文件夹"), () =>
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(Entry.Store.Path)!);
                    OS.ShellOpen(Path.GetDirectoryName(Entry.Store.Path)!);
                })
                .AddButton("reload", T("从文件重新加载配置"), T("重新加载"), () => { Entry.Store.Load(); })
                .AddCustom("status", T("状态"), _ => new Label
                {
                    Text = Entry.Failure ?? Entry.Store.Error ?? "单人模式启用；多人模式自动关闭。初始评分为试用值，可自行修改。",
                    AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(500, 60)
                }))
            .AddSection("cards", section => section.WithTitle(T("卡牌评分"))
                .AddCustom("editor", T("搜索并编辑卡牌"), _ => BuildEditor())));
    }

    private static Control BuildEditor()
    {
        var root = new VBoxContainer { CustomMinimumSize = new Vector2(650, 420) };
        var bar = new HBoxContainer();
        var profileChoice = new OptionButton();
        profileChoice.AddItem("正式版评分"); profileChoice.AddItem("Beta 评分");
        GameApi.ResolveVersion();
        profileChoice.Select(GameApi.Profile == "beta" ? 1 : 0);
        var search = new LineEdit { PlaceholderText = "搜索中文名、英文类名或卡牌 ID", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        bar.AddChild(profileChoice); bar.AddChild(search); root.AddChild(bar);
        root.AddChild(new Label { Text = "分数 0 = 使用内置评分；上限 -1 = 不限制；优先级越大越先抓（同分且同升级状态时）。",
            AutowrapMode = TextServer.AutowrapMode.WordSmart, CustomMinimumSize = new Vector2(630, 46) });
        var status = new Label(); root.AddChild(status);
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(630, 320), SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        var list = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(list); root.AddChild(scroll);
        List<object> cards;
        try { cards = GameApi.AllCards().OrderBy(GameApi.Title).ToList(); }
        catch (Exception e) { status.Text = "卡牌库尚未就绪：" + e.Message; return root; }

        void Refresh()
        {
            foreach (var child in list.GetChildren()) { list.RemoveChild(child); child.QueueFree(); }
            var query = search.Text.Trim();
            var matches = cards.Where(c => query.Length == 0 ||
                GameApi.Title(c).Contains(query, StringComparison.OrdinalIgnoreCase) ||
                GameApi.Id(c).Contains(query, StringComparison.OrdinalIgnoreCase) ||
                c.GetType().Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
            status.Text = $"找到 {matches.Count} 张；显示前 60 张，请输入关键词缩小范围。";
            var profile = profileChoice.Selected == 1 ? "beta" : "stable";
            foreach (var card in matches.Take(60))
            {
                var id = GameApi.Id(card);
                var existing = Entry.Store.Override(profile, id);
                var baseline = Ratings.ForProfile(card, profile);
                var row = new HBoxContainer();
                row.AddChild(new Label { Text = GameApi.Title(card), TooltipText = id + " / " + card.GetType().Name,
                    CustomMinimumSize = new Vector2(210, 0), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, ClipText = true });
                row.AddChild(new Label { Text = "分" });
                var score = Spin(0, 5, existing?.Score ?? 0); row.AddChild(score);
                row.AddChild(new Label { Text = "上限" });
                var cap = Spin(-1, 99, (existing ?? baseline)?.MaxCopies ?? -1); row.AddChild(cap);
                row.AddChild(new Label { Text = "优先" });
                var priority = Spin(-999, 999, (existing ?? baseline)?.Priority ?? 0); row.AddChild(priority);
                var save = new Button { Text = "保存" };
                save.Pressed += () =>
                {
                    var r = (int)score.Value == 0 ? null : new Rating((int)score.Value,
                        (int)cap.Value < 0 ? null : (int)cap.Value, (int)priority.Value);
                    var success = Entry.Store.SetRating(profile, id, r);
                    status.Text = success ? "已保存：" + GameApi.Title(card) + (r is null ? "（恢复内置）" : $"（{r.Score} 分）") : Entry.Store.Error;
                };
                row.AddChild(save); list.AddChild(row);
                list.AddChild(new Label { Text = baseline is null ? "内置：未评分；遇到时暂停自动抓牌" : $"内置/当前：{baseline.Score} 分；{card.GetType().Name}",
                    Modulate = new Color(0.65f, 0.72f, 0.8f), MouseFilter = Control.MouseFilterEnum.Ignore });
            }
        }
        search.TextChanged += _ => Refresh();
        profileChoice.ItemSelected += _ => Refresh();
        Refresh();
        return root;
    }

    private static SpinBox Spin(int min, int max, int value) => new()
    { MinValue = min, MaxValue = max, Step = 1, Value = value, CustomMinimumSize = new Vector2(75, 0) };
}
