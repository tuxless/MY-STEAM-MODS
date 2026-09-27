namespace SpireDraft.Core;

public sealed record Rating(int Score, int? MaxCopies = null, int Priority = 0);
public sealed record Offer(string Id, int UpgradeLevel, Rating? Rating);
public sealed record Rules(int TwoStarDeckLimit = 20, int ThreeStarDeckLimit = 30);
public enum DecisionKind { Pick, Skip, Hold }
public sealed record Decision(DecisionKind Kind, int Index, string Reason);

/// <summary>Pure deterministic decision logic. Never reads or modifies game objects.</summary>
public static class RatingEngine
{
    public static Decision Decide(IReadOnlyList<Offer> offers, IReadOnlyDictionary<string, int> copies,
        int deckSize, Rules rules, bool isSinglePlayer, bool canSkip)
    {
        if (!isSinglePlayer) return Hold("多人或未知游戏模式：已关闭自动抓牌");
        if (deckSize < 0 || rules.TwoStarDeckLimit < 1 || rules.ThreeStarDeckLimit < rules.TwoStarDeckLimit)
            return Hold("规则或牌组数据无效");
        if (offers.Count == 0) return Hold("奖励尚未准备好");
        // Any unknown candidate pauses the entire reward, even beside a five-star card.
        if (offers.Any(o => string.IsNullOrWhiteSpace(o.Id) || o.Rating is null ||
            o.Rating.Score is < 1 or > 5 || o.Rating.MaxCopies is < 0))
            return Hold("包含未评分或无效评分的卡牌，请先评分或手动选择");

        var eligible = new List<(Offer Card, int Index)>();
        for (var i = 0; i < offers.Count; i++)
        {
            var card = offers[i];
            var r = card.Rating!;
            copies.TryGetValue(card.Id, out var owned);
            if (owned < 0) return Hold("重复张数无效");
            if (r.Score == 1) continue;
            if (r.Score != 5)
            {
                if (r.MaxCopies is int max && owned >= max) continue;
                if (r.Score == 2 && deckSize >= rules.TwoStarDeckLimit) continue;
                if (r.Score == 3 && deckSize >= rules.ThreeStarDeckLimit) continue;
            }
            eligible.Add((card, i));
        }
        if (eligible.Count == 0)
            return canSkip ? new(DecisionKind.Skip, -1, "所有候选牌均不满足抓牌条件") : Hold("当前奖励不能跳过，请手动选择");

        var best = eligible.OrderByDescending(x => x.Card.Rating!.Score)
            .ThenByDescending(x => x.Card.UpgradeLevel > 0)
            .ThenByDescending(x => x.Card.Rating!.Priority)
            .ThenBy(x => x.Index).First();
        return new(DecisionKind.Pick, best.Index, $"{best.Card.Rating!.Score} 分，满足抓牌条件");
    }

    private static Decision Hold(string reason) => new(DecisionKind.Hold, -1, reason);
}
