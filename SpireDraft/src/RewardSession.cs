using System.Text.Json;
using Godot;
using SpireDraft.Core;

namespace SpireDraft;

internal sealed class RewardSession : IDisposable
{
    private readonly Control _screen;
    private readonly PanelContainer _panel;
    private readonly Godot.Timer _timer;
    private readonly Label _status;
    private readonly DecisionClock _clock = new();
    private readonly List<object> _cards;
    private bool _disposed;
    private bool _executing;
    private ulong _lastTick;

    internal RewardSession(Control screen)
    {
        _screen = screen;
        _cards = GameApi.Options(screen);
        GameApi.ResolveVersion();
        _panel = new PanelContainer { Name = "SpireDraftPanel", Position = new Vector2(24, 90),
            CustomMinimumSize = new Vector2(410, 0), MouseFilter = Control.MouseFilterEnum.Stop };
        var style = new StyleBoxFlat { BgColor = new Color(0.06f, 0.08f, 0.12f, 0.95f),
            CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12,
            ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 12, ContentMarginBottom = 12 };
        _panel.AddThemeStyleboxOverride("panel", style);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 9);
        _panel.AddChild(box);
        box.AddChild(new Label { Text = "评分抓牌  /  Spire Draft", MouseFilter = Control.MouseFilterEnum.Ignore });
        box.AddChild(new Label { Text = "1 跳过 · 2 牌少抓 · 3 常规 · 4 优先 · 5 必抓",
            MouseFilter = Control.MouseFilterEnum.Ignore });
        _status = new Label { Text = "正在检查奖励…", AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(375, 62), MouseFilter = Control.MouseFilterEnum.Ignore };
        box.AddChild(_status);

        for (var i = 0; i < _cards.Count; i++)
        {
            var card = _cards[i];
            var row = new HBoxContainer();
            var rating = Ratings.For(card);
            var label = new Label { Text = $"{i + 1}. {GameApi.Title(card)}", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(230, 0), ClipText = true, TooltipText = GameApi.Id(card),
                MouseFilter = Control.MouseFilterEnum.Ignore };
            var choice = new OptionButton();
            choice.AddItem("内置 / 未评分", 0);
            for (var score = 1; score <= 5; score++) choice.AddItem($"{score} 分", score);
            choice.Select(rating?.Score ?? 0);
            choice.Disabled = GameApi.Profile == "unknown";
            choice.ItemSelected += index =>
            {
                Cancel();
                var old = Ratings.For(card);
                Entry.Store.SetRating(GameApi.Profile, GameApi.Id(card), index == 0 ? null :
                    new Rating((int)index, old?.MaxCopies, old?.Priority ?? 0));
            };
            row.AddChild(label);
            row.AddChild(choice);
            box.AddChild(row);
        }
        var actions = new HBoxContainer();
        var cancel = new Button { Text = "暂停本次", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        cancel.Pressed += Cancel;
        var resume = new Button { Text = "重新评估", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        resume.Pressed += () => { if (!_executing) _clock.Restart(); };
        actions.AddChild(cancel); actions.AddChild(resume); box.AddChild(actions);
        box.AddChild(new Label { Text = "在 RitsuLib 设置中调整阈值和全部卡牌评分",
            MouseFilter = Control.MouseFilterEnum.Ignore });
        screen.AddChild(_panel);
        _timer = new Godot.Timer { WaitTime = 0.1, OneShot = false, IgnoreTimeScale = true,
            ProcessMode = Node.ProcessModeEnum.Always };
        _timer.Timeout += Tick;
        screen.AddChild(_timer);
        _lastTick = Time.GetTicksMsec();
        _timer.Start();
    }

    internal void Cancel() => _clock.Cancel();

    private void Tick()
    {
        if (_disposed) return;
        try
        {
            var now = Time.GetTicksMsec();
            var dt = (now - _lastTick) / 1000.0;
            _lastTick = now;
            if (!GameApi.SinglePlayer()) { Dispose(); return; }
            if (!GameApi.Pending(_screen)) { _clock.Tick("waiting", false, 0, 4); return; }
            var config = Entry.Store.Value;
            _panel.Visible = config.Enabled;
            if (Entry.Failure is not null || !Entry.Store.Healthy || !config.Enabled || !GameApi.ResolveVersion())
            {
                _clock.Tick("disabled", false, 0, config.DelaySeconds);
                _status.Text = Entry.Failure ?? Entry.Store.Error ??
                    (GameApi.Profile == "unknown" ? $"尚未适配游戏版本：{GameApi.Version}，请手动抓牌" : "自动抓牌已关闭");
                return;
            }

            var offersNow = GameApi.Options(_screen);
            if (offersNow.Count != _cards.Count || offersNow.Where((c, i) => !ReferenceEquals(c, _cards[i])).Any())
            { Cancel(); _status.Text = "候选牌已变化，请手动选择"; return; }
            var deck = GameApi.Deck();
            var copies = deck.GroupBy(GameApi.Id).ToDictionary(g => g.Key, g => g.Count());
            var offers = _cards.Select(c => new Offer(GameApi.Id(c), GameApi.UpgradeLevel(c), Ratings.For(c))).ToArray();
            var decision = RatingEngine.Decide(offers, copies, deck.Count,
                new Rules(config.TwoStarDeckLimit, config.ThreeStarDeckLimit), true, GameApi.SkipOption(_screen) is not null);
            // Include all offered cards, upgrade levels, ratings, deck contents and settings revision.
            // Re-evaluate every tick: an old recommendation can never outlive a changed state.
            var fingerprint = JsonSerializer.Serialize(new { offers, copies, deckCount = deck.Count,
                Entry.Store.Revision, GameApi.Profile, decision.Kind, decision.Index });
            var action = decision.Kind switch
            {
                DecisionKind.Pick => "抓取「" + GameApi.Title(_cards[decision.Index]) + "」",
                DecisionKind.Skip => "跳过本次奖励",
                _ => decision.Reason
            };
            var active = GameApi.Active(_screen) && config.Automatic && decision.Kind != DecisionKind.Hold;
            var commit = _clock.Tick(fingerprint, active, dt, config.DelaySeconds);
            _status.Text = _clock.Cancelled ? "本次已暂停，可手动抓牌或点击重新评估" :
                !config.Automatic ? "仅建议：" + action :
                decision.Kind == DecisionKind.Hold ? decision.Reason :
                !active ? "等待返回选牌界面：" + action :
                $"{Math.Ceiling(_clock.Remaining(config.DelaySeconds))} 秒后{action}\n牌组 {deck.Count} 张 · {decision.Reason}";
            if (!commit) return;
            // All callbacks and the original game action run on the Godot main thread.
            if (!GameApi.Active(_screen) || !GameApi.Pending(_screen) || !Entry.Store.Healthy || !config.Enabled || !config.Automatic)
            { Cancel(); return; }
            _executing = true;
            _timer.Stop();
            Entry.Log($"{GameApi.Version}/{GameApi.Profile}: {action}; {decision.Reason}");
            if (decision.Kind == DecisionKind.Pick) GameApi.Pick(_screen, decision.Index, _cards[decision.Index]);
            else if (decision.Kind == DecisionKind.Skip) GameApi.Skip(_screen);
        }
        catch (Exception e)
        {
            Cancel();
            _timer.Stop();
            _status.Text = "自动操作已暂停：接口或数据发生变化，请手动抓牌";
            Entry.Log("Reward session stopped: " + e.GetBaseException().Message);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _clock.Cancel();
        if (GodotObject.IsInstanceValid(_timer)) { _timer.Stop(); _timer.QueueFree(); }
        if (GodotObject.IsInstanceValid(_panel)) _panel.QueueFree();
    }
}
