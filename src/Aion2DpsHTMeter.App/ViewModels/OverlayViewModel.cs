using System.Windows.Media;
using System.Windows.Threading;
using Aion2DpsHTMeter.App.Services;
using AionMeter.Core;
using AionMeter.Core.Combat;

namespace Aion2DpsHTMeter.App.ViewModels;

public sealed record SortOption(SortKey Key, string Label);

/// <summary>Reads the live snapshot five times a second and turns it into what the overlay shows.</summary>
public sealed class OverlayViewModel : ObservableObject
{
    private readonly MeterService _meter;
    private readonly AppSettings _settings;
    private readonly DispatcherTimer _timer;

    private IReadOnlyList<PlayerRowViewModel> _players = [];
    private string _bossName = "";
    private string _clock = "00:00";
    private bool _hasBoss;
    private double _bossHp;
    private string _bossHpText = "";
    private string _status = "";
    private string _totalDps = "", _totalHeal = "", _totalTaken = "";
    private SortOption _sort;

    public OverlayViewModel(MeterService meter, AppSettings settings)
    {
        _meter = meter;
        _settings = settings;
        _sort = SortOptions.FirstOrDefault(o => o.Key == settings.SortKey) ?? SortOptions[0];
        PanelBackground = PanelBrush(settings.Opacity);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _timer.Tick += (_, _) => Refresh();
        _timer.Start();
    }

    public IReadOnlyList<SortOption> SortOptions { get; } =
    [
        new(SortKey.Dps, "DPS順"),
        new(SortKey.Healing, "回復量順"),
        new(SortKey.DamageTaken, "被ダメージ順"),
    ];

    public SortOption Sort
    {
        get => _sort;
        set
        {
            if (!Set(ref _sort, value)) return;
            _settings.SortKey = value.Key;
            Refresh();
        }
    }

    public Brush PanelBackground { get; }

    public IReadOnlyList<PlayerRowViewModel> Players { get => _players; private set => Set(ref _players, value); }
    public string BossName { get => _bossName; private set => Set(ref _bossName, value); }
    public string Clock { get => _clock; private set => Set(ref _clock, value); }
    public bool HasBoss { get => _hasBoss; private set => Set(ref _hasBoss, value); }
    /// <summary>Boss HP, 0–1.</summary>
    public double BossHp { get => _bossHp; private set => Set(ref _bossHp, value); }
    public string BossHpText { get => _bossHpText; private set => Set(ref _bossHpText, value); }
    /// <summary>Capture state, shown while there is no fight to display.</summary>
    public string Status { get => _status; private set => Set(ref _status, value); }
    public string TotalDps { get => _totalDps; private set => Set(ref _totalDps, value); }
    public string TotalHeal { get => _totalHeal; private set => Set(ref _totalHeal, value); }
    public string TotalTaken { get => _totalTaken; private set => Set(ref _totalTaken, value); }

    public void Reset()
    {
        _meter.Reset();
        Refresh();
    }

    private void Refresh()
    {
        _meter.Tick();
        var snap = _meter.Snapshot();
        if (snap is null || snap.Combatants.Count == 0)
        {
            ShowEmpty();
            return;
        }

        var combatants = SortBy(snap.Combatants, _sort.Key);
        var maxDps = combatants.Max(c => c.Dps);
        var maxHeal = combatants.Max(c => c.Healing);
        var maxTaken = combatants.Max(c => c.DamageTaken);
        Players = combatants.Select((c, i) => PlayerRowViewModel.From(i + 1, c, maxDps, maxHeal, maxTaken)).ToList();

        Status = "";
        Clock = Format.Clock(snap.CombatMs);
        HasBoss = snap.Boss is not null;
        BossName = snap.Boss?.Name ?? snap.Title;
        BossHp = snap.Boss?.HpFraction ?? 0;
        BossHpText = HasBoss ? "HP " + Format.Percent(BossHp) : "";
        TotalDps = PlayerRowViewModel.Kilo(snap.PartyDps);
        TotalHeal = PlayerRowViewModel.Kilo(combatants.Sum(c => c.Healing));
        TotalTaken = PlayerRowViewModel.Kilo(combatants.Sum(c => c.DamageTaken));
    }

    private void ShowEmpty()
    {
        Players = [];
        Status = _meter.CaptureStatus.Message;
        Clock = "00:00";
        HasBoss = false;
        BossName = "";
        TotalDps = TotalHeal = TotalTaken = "";
    }

    /// <summary>Highest value of the chosen metric first; the unknown-summons row always goes last.</summary>
    private static List<CombatantSnapshot> SortBy(IEnumerable<CombatantSnapshot> combatants, SortKey key)
    {
        Func<CombatantSnapshot, double> metric = key switch
        {
            SortKey.Healing => c => c.Healing,
            SortKey.DamageTaken => c => c.DamageTaken,
            _ => c => c.Dps,
        };
        return combatants.OrderBy(c => c.IsUnknownSummons).ThenByDescending(metric).ToList();
    }

    private static Brush PanelBrush(double opacity)
    {
        var alpha = (byte)Math.Round(Math.Clamp(opacity, 0.4, 1) * 255);
        var brush = new SolidColorBrush(Color.FromArgb(alpha, 0x0C, 0x0E, 0x14));
        brush.Freeze();
        return brush;
    }
}
