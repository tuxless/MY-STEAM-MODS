using System.Text.Json;

namespace SpireDraft.Core;

public sealed class Configuration
{
    public int SchemaVersion { get; set; } = 1;
    public bool Enabled { get; set; } = true;
    public bool Automatic { get; set; } = true;
    public int DelaySeconds { get; set; } = 4;
    public int TwoStarDeckLimit { get; set; } = 20;
    public int ThreeStarDeckLimit { get; set; } = 30;
    public Dictionary<string, Dictionary<string, Rating>> Profiles { get; set; } = new();
    public void Validate()
    {
        if (SchemaVersion != 1) throw new InvalidDataException("配置版本不兼容");
        if (DelaySeconds is < 1 or > 30 || TwoStarDeckLimit is < 1 or > 200 ||
            ThreeStarDeckLimit < TwoStarDeckLimit || ThreeStarDeckLimit > 200)
            throw new InvalidDataException("倒计时或牌组阈值无效");
        if (Profiles is null || Profiles.Any(p => string.IsNullOrWhiteSpace(p.Key) || p.Value is null ||
            p.Value.Any(v => string.IsNullOrWhiteSpace(v.Key) || v.Value is null ||
                v.Value.Score is < 1 or > 5 || v.Value.MaxCopies is < 0)))
            throw new InvalidDataException("卡牌评分无效");
    }
}

public sealed class ConfigurationStore(string path)
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    public string Path { get; } = path;
    public Configuration Value { get; private set; } = new();
    public string? Error { get; private set; }
    public long Revision { get; private set; }
    public bool Healthy => Error is null;

    public bool Load()
    {
        try
        {
            var next = File.Exists(Path)
                ? JsonSerializer.Deserialize<Configuration>(File.ReadAllText(Path), Json)
                    ?? throw new InvalidDataException("配置为空")
                : new Configuration();
            next.Validate();
            Value = next;
            Error = null;
            Revision++;
            return true;
        }
        catch (Exception e) { Error = "配置读取失败：" + e.Message; Revision++; return false; }
    }

    public bool Change(Action<Configuration> change)
    {
        // Do not overwrite a malformed user file with defaults.
        if (!Healthy) return false;
        var before = JsonSerializer.Serialize(Value, Json);
        try
        {
            change(Value);
            Value.Validate();
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
            var temp = Path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(Value, Json));
            if (File.Exists(Path)) File.Copy(Path, Path + ".bak", true);
            File.Move(temp, Path, true);
            Revision++;
            return true;
        }
        catch (Exception e)
        {
            Value = JsonSerializer.Deserialize<Configuration>(before, Json)!;
            Error = "配置保存失败：" + e.Message;
            Revision++;
            return false;
        }
    }

    public Rating? Override(string profile, string cardId) =>
        Value.Profiles.TryGetValue(profile, out var ratings) && ratings.TryGetValue(cardId, out var r) ? r : null;

    public bool SetRating(string profile, string cardId, Rating? rating) => Change(c =>
    {
        if (!c.Profiles.TryGetValue(profile, out var ratings)) c.Profiles[profile] = ratings = new();
        if (rating is null) ratings.Remove(cardId); else ratings[cardId] = rating;
    });
}
