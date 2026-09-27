using System.Reflection;
using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using SpireDraft.Core;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Patching.Models;

namespace SpireDraft;

[ModInitializer(nameof(Initialize))]
public static class Entry
{
    public const string ModId = "SpireDraft";
    internal static ConfigurationStore Store { get; private set; } = null!;
    internal static string? Failure { get; private set; }
    private static readonly ConditionalWeakTable<Control, RewardSession> Sessions = new();
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        try
        {
            Store = new ConfigurationStore(Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "SpireDraft", "settings.json"));
            Store.Load();
            Ratings.Load();
            ModTypeDiscoveryHub.RegisterModAssembly(ModId, Assembly.GetExecutingAssembly());
            SettingsPage.Register();
            GameApi.Resolve();
            var patcher = RitsuLibFramework.CreatePatcher(ModId, "reward-screen");
            // PatchAll / ApplyRequiredPatcher only applies static ModPatchInfo entries in RitsuLib 0.6.2.
            // Runtime-resolved DynamicPatchInfo entries must be passed to ApplyDynamicPatches.
            var patches = new[]
            {
                new DynamicPatchInfo("refresh", GameApi.Refresh,
                    postfix: new HarmonyMethod(typeof(Entry), nameof(AfterRefresh))),
                new DynamicPatchInfo("pick", GameApi.Select,
                    prefix: new HarmonyMethod(typeof(Entry), nameof(BeforeManualAction))),
                new DynamicPatchInfo("alternate", GameApi.Alternate,
                    prefix: new HarmonyMethod(typeof(Entry), nameof(BeforeManualAction))),
                new DynamicPatchInfo("exit", GameApi.Exit,
                    prefix: new HarmonyMethod(typeof(Entry), nameof(BeforeExit)))
            };
            if (!patcher.ApplyDynamicPatches(patches, rollbackOnCriticalFailure: true))
            {
                Disable("奖励界面补丁未能加载");
                return;
            }
            Log($"Installed {patcher.AppliedPatchCount} reward patches.");
            Log("Initialized; single-player reward drafting only.");
        }
        catch (Exception e) { Disable("初始化失败：" + e.Message); }
    }

    internal static void Disable(string message) { Failure = message; Log(message); }
    internal static void Log(string message) => GD.Print("[SpireDraft] " + message);

    // These patches always let the original game operation continue.
    private static void AfterRefresh(object __instance)
    {
        if (Failure is not null || __instance is not Control screen) return;
        if (!GameApi.SinglePlayer()) { Log("Reward refresh ignored outside a single-player run."); return; }
        try
        {
            Log("Reward refresh detected; scheduling rating panel.");
            if (Sessions.TryGetValue(screen, out var existing)) existing.Dispose();
            Sessions.Remove(screen);
            // Deferred: old card holders queued for deletion must disappear before matching offers.
            Callable.From(() => Attach(screen)).CallDeferred();
        }
        catch (Exception e) { Log("Attach scheduling failed: " + e.Message); }
    }

    private static void Attach(Control screen)
    {
        if (Failure is not null || !GodotObject.IsInstanceValid(screen) || screen.IsQueuedForDeletion() ||
            !screen.IsInsideTree() || !GameApi.SinglePlayer()) return;
        try
        {
            if (Sessions.TryGetValue(screen, out var previous)) previous.Dispose();
            Sessions.Remove(screen);
            var session = new RewardSession(screen);
            Sessions.Add(screen, session);
            Log("Rating panel attached.");
        }
        catch (Exception e) { Log("Reward automation unavailable: " + e.Message); }
    }

    private static void BeforeManualAction(object __instance)
    {
        if (__instance is Control screen && Sessions.TryGetValue(screen, out var session)) session.Cancel();
    }
    private static void BeforeExit(object __instance)
    {
        if (__instance is Control screen && Sessions.TryGetValue(screen, out var session))
        { session.Dispose(); Sessions.Remove(screen); }
    }
}
