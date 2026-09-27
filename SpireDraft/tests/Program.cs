using SpireDraft.Core;

var passed = 0;
void Test(string name, Action action)
{
    try { action(); Console.WriteLine("PASS " + name); passed++; }
    catch (Exception e) { Console.Error.WriteLine("FAIL " + name + ": " + e.Message); Environment.Exit(1); }
}
void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}; got {actual}");
}
Offer Card(string id, int score, int? cap = null, int upgraded = 0, int priority = 0) =>
    new(id, upgraded, new Rating(score, cap, priority));
Decision Decide(Offer[] offers, int deck = 10, bool single = true, bool skip = true,
    Dictionary<string, int>? copies = null) => RatingEngine.Decide(offers, copies ?? new(), deck, new Rules(), single, skip);

Test("one-star skipped", () => Equal(DecisionKind.Skip, Decide([Card("a", 1)]).Kind));
Test("two-star limit is exclusive", () =>
{
    Equal(DecisionKind.Pick, Decide([Card("a", 2)], 19).Kind);
    Equal(DecisionKind.Skip, Decide([Card("a", 2)], 20).Kind);
});
Test("three-star limit is exclusive", () =>
{
    Equal(DecisionKind.Pick, Decide([Card("a", 3)], 29).Kind);
    Equal(DecisionKind.Skip, Decide([Card("a", 3)], 30).Kind);
});
Test("four-star ignores deck limit", () => Equal(DecisionKind.Pick, Decide([Card("a", 4)], 80).Kind));
Test("copy cap counts upgraded and unupgraded together", () =>
    Equal(DecisionKind.Skip, Decide([Card("a", 4, 2, 1)], copies: new() { ["a"] = 2 }).Kind));
Test("five-star bypasses all size and copy limits", () =>
    Equal(DecisionKind.Pick, Decide([Card("a", 5, 0)], 100, copies: new() { ["a"] = 50 }).Kind));
Test("multiplayer disables even five-star", () => Equal(DecisionKind.Hold, Decide([Card("a", 5)], single: false).Kind));
Test("unknown candidate pauses whole reward", () =>
    Equal(DecisionKind.Hold, Decide([Card("a", 5), new Offer("modded", 0, null)]).Kind));
Test("mandatory selection cannot be skipped", () => Equal(DecisionKind.Hold, Decide([Card("a", 1)], skip: false).Kind));
Test("highest eligible wins", () => Equal(1, Decide([Card("a", 3), Card("b", 4), Card("c", 2)]).Index));
Test("upgraded before manual tiebreak priority", () =>
    Equal(1, Decide([Card("a", 4, priority: 99), Card("b", 4, upgraded: 1)]).Index));
Test("manual priority before left-to-right", () =>
    Equal(1, Decide([Card("a", 5), Card("b", 5, priority: 10)]).Index));
Test("leftmost final tie", () => Equal(0, Decide([Card("a", 5), Card("b", 5)]).Index));
Test("unknown rules and invalid scores hold", () =>
{
    Equal(DecisionKind.Hold, Decide([Card("a", 6)]).Kind);
    Equal(DecisionKind.Hold, RatingEngine.Decide([Card("a", 3)], new Dictionary<string,int>(), 20, new Rules(30,20), true,true).Kind);
});
Test("clock commits once", () =>
{
    var c = new DecisionClock(); var count = 0;
    for (var i = 0; i < 100; i++) if (c.Tick("a", true, .1, 1)) count++;
    Equal(1, count);
});
Test("manual cancel prevents all future actions", () =>
{
    var c = new DecisionClock(); c.Tick("a",true,.2,1); c.Cancel();
    for (var i = 0; i < 100; i++) Equal(false, c.Tick("a",true,.1,1));
});
Test("covered screen resets countdown", () =>
{
    var c = new DecisionClock(); for (var i=0;i<4;i++) c.Tick("a",true,.2,1);
    c.Tick("a",false,.2,1);
    Equal(false,c.Tick("a",true,.2,1));
    Equal(.8,c.Remaining(1));
});
Test("changed deck invalidates countdown", () =>
{
    var c = new DecisionClock(); for (var i=0;i<4;i++) c.Tick("a",true,.2,1);
    Equal(false,c.Tick("b",true,.2,1)); Equal(.8,c.Remaining(1));
});
Test("stall cannot instantly select", () => Equal(false,new DecisionClock().Tick("a",true,60,1)));
Test("configuration roundtrip, profile isolation and corruption preservation", () =>
{
    var root = Path.Combine(Path.GetTempPath(), "SpireDraftTests-" + Guid.NewGuid());
    Directory.CreateDirectory(root);
    try
    {
        var path = Path.Combine(root,"settings.json"); var store = new ConfigurationStore(path);
        Equal(true,store.Load()); Equal(true,store.SetRating("stable","a",new Rating(5)));
        Equal(true,store.SetRating("beta","a",new Rating(1)));
        Equal(true,store.Load()); Equal(5,store.Override("stable","a")!.Score); Equal(1,store.Override("beta","a")!.Score);
        Equal(true,store.SetRating("stable","a",null)); Equal<Rating?>(null,store.Override("stable","a"));
        File.WriteAllText(path,"broken JSON"); Equal(false,store.Load());
        Equal(false,store.Change(c=>c.Enabled=false)); Equal("broken JSON",File.ReadAllText(path));
    }
    finally { Directory.Delete(root,true); }
});
Console.WriteLine($"{passed} tests passed.");
