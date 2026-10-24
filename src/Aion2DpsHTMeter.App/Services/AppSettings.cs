using System.IO;
using System.Text.Json;

namespace Aion2DpsHTMeter.App.Services;

public enum SortKey
{
    Dps,
    Healing,
    DamageTaken,
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

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    /// <summary>Background opacity of the overlay panel, 0.4–1.</summary>
    public double Opacity { get; set; } = 0.85;

    public SortKey SortKey { get; set; } = SortKey.Dps;

    /// <summary>Npcap device to force (VPN / ping accelerator users). Null = pick from the game's connections.</summary>
    public string? CaptureDevice { get; set; }

    public double? Left { get; set; }
    public double? Top { get; set; }

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
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
