using System.Globalization;
using System.Windows;
using System.Windows.Media;
using AionMeter.Core.Combat;

namespace Aion2DpsHTMeter.App.ViewModels;

/// <summary>One player row: class bar with the rank on it, then the DPS (name on top), healing and damage-taken bars.</summary>
public sealed class PlayerRowViewModel : ObservableObject
{
    private int _rank;
    private string _name = "";
    private Brush _classBrush = Brushes.Gray;
    private bool _isSelf;
    private string _dpsText = "", _healText = "", _takenText = "";
    private GridLength _dpsFill, _dpsRest, _healFill, _healRest, _takenFill, _takenRest;

    public int Rank { get => _rank; private set => Set(ref _rank, value); }
    public string Name { get => _name; private set => Set(ref _name, value); }
    public Brush ClassBrush { get => _classBrush; private set => Set(ref _classBrush, value); }
    public bool IsSelf { get => _isSelf; private set => Set(ref _isSelf, value); }

    public string DpsText { get => _dpsText; private set => Set(ref _dpsText, value); }
    public string HealText { get => _healText; private set => Set(ref _healText, value); }
    public string TakenText { get => _takenText; private set => Set(ref _takenText, value); }

    // Bar lengths as two star columns (filled / rest) so the bars stretch with the window.
    public GridLength DpsFill { get => _dpsFill; private set => Set(ref _dpsFill, value); }
    public GridLength DpsRest { get => _dpsRest; private set => Set(ref _dpsRest, value); }
    public GridLength HealFill { get => _healFill; private set => Set(ref _healFill, value); }
    public GridLength HealRest { get => _healRest; private set => Set(ref _healRest, value); }
    public GridLength TakenFill { get => _takenFill; private set => Set(ref _takenFill, value); }
    public GridLength TakenRest { get => _takenRest; private set => Set(ref _takenRest, value); }

    /// <param name="max">Party maximum of each metric: the longest bar of each kind is full width.</param>
    public void Update(int rank, CombatantSnapshot c, (double Dps, long Heal, long Taken) max)
    {
        Rank = rank;
        Name = c.IsUnknownSummons ? "召喚獣(所有者不明)" : c.Name;
        ClassBrush = ClassPalette.Of(c.Class);
        IsSelf = c.IsSelf;

        DpsText = Kilo(c.Dps);
        HealText = Kilo(c.Healing);
        TakenText = Kilo(c.DamageTaken);

        (DpsFill, DpsRest) = Bar(c.Dps, max.Dps);
        (HealFill, HealRest) = Bar(c.Healing, max.Heal);
        (TakenFill, TakenRest) = Bar(c.DamageTaken, max.Taken);
    }

    /// <summary>Every number in thousands with one decimal: 52840 → 52.8K, 1120000 → 1,120.0K.</summary>
    public static string Kilo(double value) => (value / 1000).ToString("#,0.0", CultureInfo.InvariantCulture) + "K";

    private static (GridLength Filled, GridLength Empty) Bar(double value, double max)
    {
        var f = max > 0 ? Math.Clamp(value / max, 0, 1) : 0;
        return (new GridLength(f, GridUnitType.Star), new GridLength(1 - f, GridUnitType.Star));
    }
}
