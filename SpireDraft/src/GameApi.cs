using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using Godot;
using MegaCrit.Sts2.Core.Modding;

namespace SpireDraft;

/// <summary>
/// Narrow compatibility boundary. Missing/changed members disable automation instead of guessing.
/// Game methods are resolved by name + arity and validated before any patch is installed.
/// </summary>
internal static class GameApi
{
    internal static readonly Assembly Game = typeof(ModInitializerAttribute).Assembly;
    internal static Type RewardScreen = null!;
    internal static MethodInfo Refresh = null!, Select = null!, Alternate = null!, Exit = null!;
    internal static string Profile { get; private set; } = "unknown";
    internal static string Version { get; private set; } = "unknown";
    private static Type _run = null!, _overlays = null!, _modal = null!, _context = null!;
    private static readonly BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic |
                                                  BindingFlags.Instance | BindingFlags.Static;

    internal static void Resolve()
    {
        RewardScreen = Type("Nodes.Screens.CardSelection.NCardRewardSelectionScreen");
        Refresh = Method(RewardScreen, "RefreshOptions", 2);
        Select = Method(RewardScreen, "SelectCard", 1);
        Alternate = Method(RewardScreen, "OnAlternateRewardSelected", 1);
        Exit = Method(RewardScreen, "_ExitTree", 0);
        foreach (var f in new[] { "_options", "_extraOptions", "_cardRow", "_completionSource" })
            if (RewardScreen.GetField(f, Members) is null) throw new MissingFieldException(RewardScreen.FullName, f);
        _run = Type("Runs.RunManager");
        _overlays = Type("Nodes.Screens.Overlays.NOverlayStack");
        _modal = Type("Nodes.CommonUi.NModalContainer");
        _context = Type("Nodes.Screens.ScreenContext.ActiveScreenContext");
        _ = Method(_overlays, "Peek", 0);
        _ = Method(_context, "IsCurrent", 1);
        _ = RequiredProperty(_modal, "OpenModal");
        _ = RequiredProperty(_run, "NetService");
        _ = RequiredProperty(_run, "IsInProgress");
        _ = RequiredProperty(_run, "State");
        // Require the original callback and holder shape before enabling actions.
        var holder = Type("Nodes.Cards.Holders.NCardHolder");
        if (!Select.GetParameters()[0].ParameterType.IsAssignableFrom(holder))
            throw new InvalidOperationException("SelectCard signature changed");
        _ = RequiredProperty(holder, "CardModel");
        var alternative = Type("Entities.CardRewardAlternatives.CardRewardAlternative");
        _ = RequiredProperty(alternative, "OptionId");
        _ = RequiredProperty(alternative, "AfterSelected");
        if (RequiredProperty(alternative, "OnSelect").PropertyType != typeof(Func<Task>))
            throw new InvalidOperationException("Alternate reward callback signature changed");
    }

    internal static bool ResolveVersion()
    {
        try
        {
            var manager = Get(Type("Debug.ReleaseInfoManager"), "Instance");
            var info = Get(manager, "ReleaseInfo");
            Version = Convert.ToString(Get(info, "Version")) ?? "unknown";
            var number = Regex.Match(Version, @"\d+\.\d+\.\d+").Value;
            Profile = number switch
            {
                "0.107.1" => "stable",
                "0.109.0" or "0.110.0" or "0.111.0" => "beta",
                _ => "unknown"
            };
            return Profile != "unknown";
        }
        catch { Profile = "unknown"; return false; }
    }

    internal static bool SinglePlayer()
    {
        try
        {
            var run = Get(_run, "Instance");
            // Player count alone is NOT a valid way to identify a single-player game.
            return Get(run, "IsInProgress") is true &&
                   Convert.ToString(Get(Get(run, "NetService"), "Type")) == "Singleplayer" &&
                   Items(Get(Get(run, "State"), "Players")).Count == 1;
        }
        catch { return false; }
    }

    internal static bool Active(Control screen)
    {
        if (!GodotObject.IsInstanceValid(screen) || !screen.IsInsideTree() ||
            screen.IsQueuedForDeletion() || !screen.IsVisibleInTree() || screen.GetTree().Paused || !SinglePlayer())
            return false;
        var overlay = Get(_overlays, "Instance");
        var context = Get(_context, "Instance");
        var modal = Get(_modal, "Instance");
        if (overlay is null || context is null || modal is null) return false;
        return ReferenceEquals(Method(_overlays, "Peek", 0).Invoke(overlay, null), screen) &&
               Method(_context, "IsCurrent", 1).Invoke(context, [screen]) is true &&
               Get(modal, "OpenModal") is null;
    }

    internal static bool Pending(Control screen)
    {
        var source = Get(screen, "_completionSource");
        return source is not null && Get(source, "Task") is Task task && !task.IsCompleted;
    }

    internal static List<object> Deck()
    {
        var state = Get(Get(_run, "Instance"), "State");
        var players = Items(Get(state, "Players"));
        if (players.Count != 1) throw new InvalidOperationException("Not a single-player run");
        return Items(Get(Get(players[0], "Deck"), "Cards"));
    }

    internal static List<object> Options(Control screen) => Items(Get(screen, "_options"))
        .Select(o => Get(o, "Card") ?? throw new InvalidOperationException("Missing offered card")).ToList();

    internal static List<Control> Holders(Control screen)
    {
        var row = Get(screen, "_cardRow") as Node ?? throw new InvalidOperationException("Missing card row");
        return row.GetChildren().OfType<Control>().Where(n => !n.IsQueuedForDeletion() &&
            n.GetType().FullName == "MegaCrit.Sts2.Core.Nodes.Cards.Holders.NGridCardHolder").ToList();
    }

    internal static string Id(object card) => Get(card, "Id")?.ToString()
        ?? throw new InvalidOperationException("Missing card id");
    internal static string Title(object card) => Convert.ToString(Get(card, "Title")) ?? Id(card);
    internal static bool Upgraded(object card) => Get(card, "IsUpgraded") is true;
    internal static int UpgradeLevel(object card) => Convert.ToInt32(Get(card, "CurrentUpgradeLevel"));
    internal static bool BuiltIn(object card) => card.GetType().Assembly == Game &&
        card.GetType().Namespace == "MegaCrit.Sts2.Core.Models.Cards";
    internal static List<object> AllCards() => Items(Get(Type("Models.ModelDb"), "AllCards"));
    internal static object? SkipOption(Control screen) => Items(Get(screen, "_extraOptions"))
        .SingleOrDefault(o => string.Equals(Convert.ToString(Get(o, "OptionId")), "Skip", StringComparison.OrdinalIgnoreCase));

    internal static void Pick(Control screen, int index, object expectedCard)
    {
        var holders = Holders(screen);
        if (index < 0 || index >= holders.Count || !ReferenceEquals(Get(holders[index], "CardModel"), expectedCard))
            throw new InvalidOperationException("Reward changed before selection");
        Select.Invoke(screen, [holders[index]]);
    }

    internal static void Skip(Control screen)
    {
        var skip = SkipOption(screen) ?? throw new InvalidOperationException("Skip is not offered");
        var after = Get(skip, "AfterSelected") ?? throw new InvalidOperationException("Missing skip action");
        var callback = Get(skip, "OnSelect") as Func<Task> ?? throw new InvalidOperationException("Missing skip callback");
        // Match the game's own alternate-reward button: settle selection, then run its callback.
        Alternate.Invoke(screen, [after]);
        _ = Observe(callback());
    }

    private static async Task Observe(Task task)
    {
        try { await task; }
        catch (Exception e) { Entry.Log("Skip callback failed: " + e.Message); }
    }

    internal static object? Get(object? owner, string name)
    {
        if (owner is null) return null;
        var type = owner as Type ?? owner.GetType();
        var instance = owner is Type ? null : owner;
        var p = type.GetProperty(name, Members);
        if (p is not null) return p.GetValue(instance);
        var f = type.GetField(name, Members);
        if (f is not null) return f.GetValue(instance);
        throw new MissingMemberException(type.FullName, name);
    }
    private static List<object> Items(object? items) => items is IEnumerable enumerable
        ? enumerable.Cast<object>().ToList() : throw new InvalidOperationException("Missing collection");
    private static Type Type(string suffix) => Game.GetType("MegaCrit.Sts2.Core." + suffix, true)!;
    private static PropertyInfo RequiredProperty(Type type, string name) => type.GetProperty(name, Members)
        ?? throw new MissingMemberException(type.FullName, name);
    private static MethodInfo Method(Type type, string name, int count)
    {
        var methods = type.GetMethods(Members).Where(m => m.Name == name && m.GetParameters().Length == count).ToArray();
        return methods.Length == 1 ? methods[0] : throw new MissingMethodException(type.FullName, name);
    }
}
