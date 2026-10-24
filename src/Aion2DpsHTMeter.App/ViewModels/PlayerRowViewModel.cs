using System.Globalization;
using System.Windows.Media;
using AionMeter.Core.Combat;

namespace Aion2DpsHTMeter.App.ViewModels;

/// <summary>Party maximum of each metric: the longest bar of each kind is full width.</summary>
public readonly record struct PartyMax(double Dps, long Healing, long DamageTaken);

/// <summary>
/// One player row: class bar with the rank on it, then the DPS (name on top), healing and damage-taken bars.
/// A row lives as long as its player is in the fight and is updated in place on every refresh, so WPF keeps
/// the row's visuals instead of rebuilding them five times a second.
/// </summary>
public sealed class PlayerRowViewModel : ObservableObject
{
    private int _rank;
    private string _name = "";
    private Brush _classBrush = ClassPalette.Of(default);
    private bool _isSelf;
    private string _dpsText = "", _healText = "", _takenText = "";
    private double _dpsBar, _healBar, _takenBar;

    public int Rank { get => _rank; private set => Set(ref _rank, value); }
    public string Name { get => _name; private set => Set(ref _name, value); }
    public Brush ClassBrush { get => _classBrush; private set => Set(ref _classBrush, value); }
    public bool IsSelf { get => _isSelf; private set => Set(ref _isSelf, value); }

    public string DpsText { get => _dpsText; private set => Set(ref _dpsText, value); }
    public string HealText { get => _healText; private set => Set(ref _healText, value); }
    public string TakenText { get => _takenText; private set => Set(ref _takenText, value); }

    /// <summary>Bar lengths, 0–1.</summary>
    public double DpsBar { get => _dpsBar; private set => Set(ref _dpsBar, value); }
    public double HealBar { get => _healBar; private set => Set(ref _healBar, value); }
    public double TakenBar { get => _takenBar; private set => Set(ref _takenBar, value); }

    public void Update(int rank, CombatantSnapshot c, PartyMax max)
    {
        Rank = rank;
        Name = c.IsUnknownSummons ? "召喚獣(所有者不明)" : c.Name;
        ClassBrush = ClassPalette.Of(c.Class);
        IsSelf = c.IsSelf;

        DpsText = Kilo(c.Dps);
        HealText = Kilo(c.Healing);
        TakenText = Kilo(c.DamageTaken);

        DpsBar = Ratio(c.Dps, max.Dps);
        HealBar = Ratio(c.Healing, max.Healing);
        TakenBar = Ratio(c.DamageTaken, max.DamageTaken);
    }

    /// <summary>Every number in thousands with one decimal: 52840 → 52.8K, 1120000 → 1,120.0K.</summary>
    public static string Kilo(double value) => (value / 1000).ToString("#,0.0", CultureInfo.InvariantCulture) + "K";

    private static double Ratio(double value, double max) => max > 0 ? Math.Clamp(value / max, 0, 1) : 0;
}
