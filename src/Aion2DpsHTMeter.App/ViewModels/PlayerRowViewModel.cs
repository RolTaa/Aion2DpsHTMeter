using System.Globalization;
using System.Windows.Media;
using AionMeter.Core.Combat;

namespace Aion2DpsHTMeter.App.ViewModels;

/// <summary>
/// One player row: class bar with the rank on it, then the DPS (name on top), healing and damage-taken bars.
/// Bar values are 0–1, relative to the party's largest value of the same kind.
/// </summary>
public sealed record PlayerRowViewModel(
    int Rank,
    string Name,
    Brush ClassBrush,
    bool IsSelf,
    string DpsText,
    string HealText,
    string TakenText,
    double DpsBar,
    double HealBar,
    double TakenBar)
{
    public static PlayerRowViewModel From(int rank, CombatantSnapshot c, double maxDps, long maxHeal, long maxTaken) => new(
        Rank: rank,
        Name: c.IsUnknownSummons ? "召喚獣(所有者不明)" : c.Name,
        ClassBrush: ClassPalette.Of(c.Class),
        IsSelf: c.IsSelf,
        DpsText: Kilo(c.Dps),
        HealText: Kilo(c.Healing),
        TakenText: Kilo(c.DamageTaken),
        DpsBar: Ratio(c.Dps, maxDps),
        HealBar: Ratio(c.Healing, maxHeal),
        TakenBar: Ratio(c.DamageTaken, maxTaken));

    /// <summary>Every number in thousands with one decimal: 52840 → 52.8K, 1120000 → 1,120.0K.</summary>
    public static string Kilo(double value) => (value / 1000).ToString("#,0.0", CultureInfo.InvariantCulture) + "K";

    private static double Ratio(double value, double max) => max > 0 ? value / max : 0;
}
