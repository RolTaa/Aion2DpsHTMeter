using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aion2DpsHTMeter.App.Services;

public enum SortKey
{
    Dps,
    Damage,
    Healing,
    DamageTaken,
}

/// <summary>Whose rows the overlay shows.</summary>
public enum MemberScope
{
    /// <summary>周囲: everyone fighting the target.</summary>
    Everyone,
    /// <summary>個人: the local player only.</summary>
    Self,
}

/// <summary>
/// User settings, saved as JSON under %LocalAppData%\Aion2DpsHTMeter. Only what the skeleton uses so far;
/// the settings window will add the rest.
/// </summary>
public sealed class AppSettings
{
    public static string AppDataDir { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Aion2DpsHTMeter");

    private static string FilePath => Path.Combine(AppDataDir, "settings.json");

    /// <summary>Choices are saved by name, so adding a choice later does not change what an old file means.</summary>
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };

    /// <summary>Background opacity of the overlay panel, 0.4–1.</summary>
    public double Opacity { get; set; } = 0.85;

    public SortKey SortKey { get; set; } = SortKey.Dps;

    /// <summary>Whose rows are shown: everyone around, or the local player only.</summary>
    public MemberScope MemberScope { get; set; } = MemberScope.Everyone;

    /// <summary>Which fights are measured and shown.</summary>
    public TargetKinds Targets { get; set; } = TargetKinds.AbyssArtifact | TargetKinds.Boss;

    /// <summary>Npcap device to force (VPN / ping accelerator users). Null = pick from the game's connections.</summary>
    public string? CaptureDevice { get; set; }

    public double? Left { get; set; }
    public double? Top { get; set; }

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), Json) ?? new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // A broken file falls back to the defaults; it is overwritten on the next save.
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(AppDataDir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Json));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
