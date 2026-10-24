using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Threading;
using Aion2DpsHTMeter.App.Services;
using AionMeter.Core;
using AionMeter.Core.Combat;

namespace Aion2DpsHTMeter.App.ViewModels;

public sealed record SortOption(SortKey Key, string Label);

/// <summary>Polls the tracker five times a second and turns the live snapshot into rows.</summary>
public sealed class OverlayViewModel : ObservableObject
{
    private readonly MeterService _meter;
    private readonly AppSettings _settings;
    private readonly DispatcherTimer _timer;

    private string _bossName = "";
    private string _clock = "00:00";
    private double _bossHp;
    private string _bossHpText = "";
    private bool _hasBoss;
    private string _status = "";
    private string _totalDps = "", _totalHeal = "", _totalTaken = "";
    private SortOption _sort;
    private Brush _panelBackground = Brushes.Black;

    public OverlayViewModel(MeterService meter, AppSettings settings)
    {
        _meter = meter;
        _settings = settings;
        _sort = SortOptions.FirstOrDefault(o => o.Key == settings.SortKey) ?? SortOptions[0];
        ApplyOpacity();
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(200), DispatcherPriority.Background, (_, _) => Refresh(),
            Dispatcher.CurrentDispatcher);
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

    public ObservableCollection<PlayerRowViewModel> Players { get; } = [];

    public string BossName { get => _bossName; private set => Set(ref _bossName, value); }
    public string Clock { get => _clock; private set => Set(ref _clock, value); }
    /// <summary>Boss HP, 0–1.</summary>
    public double BossHp { get => _bossHp; private set => Set(ref _bossHp, value); }
    public string BossHpText { get => _bossHpText; private set => Set(ref _bossHpText, value); }
    public bool HasBoss { get => _hasBoss; private set => Set(ref _hasBoss, value); }
    /// <summary>Capture state, shown while there is no fight to display.</summary>
    public string Status { get => _status; private set => Set(ref _status, value); }

    public string TotalDps { get => _totalDps; private set => Set(ref _totalDps, value); }
    public string TotalHeal { get => _totalHeal; private set => Set(ref _totalHeal, value); }
    public string TotalTaken { get => _totalTaken; private set => Set(ref _totalTaken, value); }

    public Brush PanelBackground { get => _panelBackground; private set => Set(ref _panelBackground, value); }

    public void Reset()
    {
        _meter.Reset();
        Refresh();
    }

    private void ApplyOpacity()
    {
        var a = (byte)Math.Round(Math.Clamp(_settings.Opacity, 0.4, 1) * 255);
        var brush = new SolidColorBrush(Color.FromArgb(a, 0x0C, 0x0E, 0x14));
        brush.Freeze();
        PanelBackground = brush;
    }

    private void Refresh()
    {
        _meter.Tick();
        var snap = _meter.Snapshot();
        if (snap is null)
        {
            Players.Clear();
            HasBoss = false;
            BossName = "";
            Clock = "00:00";
            TotalDps = TotalHeal = TotalTaken = "";
            Status = _meter.CaptureStatus.Message;
            return;
        }

        Status = "";
        Clock = Format.Clock(snap.CombatMs);
        HasBoss = snap.Boss is not null;
        BossName = snap.Boss?.Name ?? snap.Title;
        BossHp = snap.Boss?.HpFraction ?? 0;
        BossHpText = snap.Boss is null ? "" : "HP " + Format.Percent(BossHp);

        var rows = Ordered(snap.Combatants, _sort.Key);
        var max = (rows.Select(c => c.Dps).DefaultIfEmpty().Max(),
                   rows.Select(c => c.Healing).DefaultIfEmpty().Max(),
                   rows.Select(c => c.DamageTaken).DefaultIfEmpty().Max());

        while (Players.Count > rows.Count) Players.RemoveAt(Players.Count - 1);
        while (Players.Count < rows.Count) Players.Add(new PlayerRowViewModel());
        for (var i = 0; i < rows.Count; i++) Players[i].Update(i + 1, rows[i], max);

        TotalDps = PlayerRowViewModel.Kilo(snap.PartyDps);
        TotalHeal = PlayerRowViewModel.Kilo(rows.Sum(c => c.Healing));
        TotalTaken = PlayerRowViewModel.Kilo(rows.Sum(c => c.DamageTaken));
    }

    /// <summary>Sorted by the chosen metric; the unknown-summons row always goes last.</summary>
    private static List<CombatantSnapshot> Ordered(IReadOnlyList<CombatantSnapshot> list, SortKey key)
    {
        Func<CombatantSnapshot, double> metric = key switch
        {
            SortKey.Healing => c => c.Healing,
            SortKey.DamageTaken => c => c.DamageTaken,
            _ => c => c.Dps,
        };
        return list.OrderBy(c => c.IsUnknownSummons).ThenByDescending(metric).ToList();
    }
}
