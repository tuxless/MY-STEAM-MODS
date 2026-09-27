using System.Reflection;
using System.Text.Json;
using SpireDraft.Core;

namespace SpireDraft;

internal static class Ratings
{
    private sealed class Seed { public Dictionary<string, Rating> Cards { get; set; } = new(); }
    private static readonly Dictionary<string, Dictionary<string, Rating>> Defaults = new();
    internal static void Load()
    {
        var assembly = Assembly.GetExecutingAssembly();
        foreach (var profile in new[] { "stable", "beta" })
        {
            var name = assembly.GetManifestResourceNames().Single(n => n.EndsWith($"ratings.{profile}.json"));
            using var stream = assembly.GetManifestResourceStream(name)!;
            var seed = JsonSerializer.Deserialize<Seed>(stream) ?? throw new InvalidDataException("Empty seed ratings");
            if (seed.Cards.Any(r => r.Value.Score is < 1 or > 5)) throw new InvalidDataException("Invalid seed score");
            Defaults[profile] = seed.Cards;
        }
    }

    internal static Rating? For(object card)
    {
        return ForProfile(card, GameApi.Profile);
    }

    internal static Rating? ForProfile(object card, string profile)
    {
        var user = Entry.Store.Override(profile, GameApi.Id(card));
        if (user is not null) return user;
        if (!GameApi.BuiltIn(card)) return null;
        return Defaults.TryGetValue(profile, out var map) && map.TryGetValue(card.GetType().Name, out var r) ? r : null;
    }
}
