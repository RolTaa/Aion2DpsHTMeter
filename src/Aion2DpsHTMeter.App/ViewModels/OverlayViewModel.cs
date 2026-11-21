using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Threading;
using Aion2DpsHTMeter.App.Services;
using AionMeter.Core;
using AionMeter.Core.Capture;
using AionMeter.Core.Combat;

namespace Aion2DpsHTMeter.App.ViewModels;

/// <summary>An entry of the sort drop-down; ToString is what the closed drop-down shows.</summary>
public sealed record SortOption(SortKey Key, string Label)
{
    public override string ToString() => Label;
}

/// <summary>Reads the live snapshot five times a second and turns it into what the overlay shows.</summary>
public sealed class OverlayViewModel : ObservableObject
{
    private readonly MeterService _meter;
    private readonly AppSettings _settings;
    private readonly DispatcherTimer _timer;

    /// <summary>The row of each player in the fight, by actor id.</summary>
    private readonly Dictionary<uint, PlayerRowViewModel> _rows = [];

    private readonly KillTimeEstimator _killTime = new();

    private string _bossName = "";
    private string _clockLabel = "";
    private string _clock = "--:--";
    private bool _hasBoss;
    private double _bossHp;
    private string _bossHpText = "";
    private string _status = "";
    private string _totalDps = "", _totalHeal = "", _totalTaken = "";
    private SortOption _sort;
    private Brush _panelBackground = Brushes.Black;

    public OverlayViewModel(MeterService meter, AppSettings settings)
    {
        _meter = meter;
        _settings = settings;
        _sort = SortOptions.FirstOrDefault(o => o.Key == settings.SortKey) ?? SortOptions[0];
        SetOpacity(settings.Opacity);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        _timer.Tick += (_, _) => Refresh();
        _timer.Start();
    }

    public IReadOnlyList<SortOption> SortOptions { get; } =
    [
        new(SortKey.Dps, "DPS"),
        new(SortKey.Damage, "総ダメージ"),
        new(SortKey.Healing, "総回復"),
        new(SortKey.DamageTaken, "総被ダメージ"),
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

    /// <summary>Whose rows are shown; the header button switches between 周囲 (everyone) and 個人 (self).</summary>
    public MemberScope Scope
    {
        get => _settings.MemberScope == MemberScope.Self ? MemberScope.Self : MemberScope.Everyone;
        private set
        {
            _settings.MemberScope = value;
            Raise();
            Raise(nameof(ScopeLabel));
            Refresh();
        }
    }

    public string ScopeLabel => Scope == MemberScope.Self ? "個人" : "周囲";

    public void NextScope() => Scope = Scope == MemberScope.Self ? MemberScope.Everyone : MemberScope.Self;

    // Measurement targets, for the settings window.
    public bool TargetArtifact { get => HasTarget(TargetKinds.AbyssArtifact); set => SetTarget(TargetKinds.AbyssArtifact, value); }
    public bool TargetBoss { get => HasTarget(TargetKinds.Boss); set => SetTarget(TargetKinds.Boss, value); }
    public bool TargetTrash { get => HasTarget(TargetKinds.Trash); set => SetTarget(TargetKinds.Trash, value); }

    private bool HasTarget(TargetKinds kind) => (_settings.Targets & kind) != 0;

    private void SetTarget(TargetKinds kind, bool on)
    {
        _settings.Targets = on ? _settings.Targets | kind : _settings.Targets & ~kind;
        Refresh();
    }

    /// <summary>Background opacity of the panel, 0.4–1.</summary>
    public double Opacity
    {
        get => _settings.Opacity;
        set
        {
            SetOpacity(value);
            Raise();
        }
    }

    /// <summary>Rows in display order.</summary>
    public ObservableCollection<PlayerRowViewModel> Players { get; } = [];

    public Brush PanelBackground { get => _panelBackground; private set => Set(ref _panelBackground, value); }
    public string BossName { get => _bossName; private set => Set(ref _bossName, value); }
    /// <summary>What the clock shows: 撃破予測 (time left to kill), 撃破 / 終了 (fight length), 経過 (no boss).</summary>
    public string ClockLabel { get => _clockLabel; private set => Set(ref _clockLabel, value); }
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

    private void SetOpacity(double opacity)
    {
        _settings.Opacity = Math.Clamp(opacity, 0.4, 1);
        var brush = new SolidColorBrush(Color.FromArgb((byte)Math.Round(_settings.Opacity * 255), 0x0C, 0x0E, 0x14));
        brush.Freeze();
        PanelBackground = brush;
    }

    public void Reset()
    {
        _meter.Reset();
        Refresh();
    }

    private void Refresh()
    {
        _meter.Tick();
        var fight = _meter.CurrentFight();
        // The header follows a Dimensional Core on the map first (hit or not), else the fight's main target: the NPC
        // with the most HP, the boss when there is one.
        ShowTarget(_meter.WatchedCore() ?? fight?.Focus ?? fight?.Boss, fight);

        if (fight is null)
        {
            ShowNoFight();
            return;
        }
        Status = "";
        var combatants = SortBy(fight.Combatants.Where(IsShown), _sort.Key);
        UpdateRows(combatants);
        TotalDps = PlayerRowViewModel.Kilo(combatants.Sum(c => c.RecentDps)) + "/" + PlayerRowViewModel.WholeKilo(combatants.Sum(c => c.FocusDamage));
        TotalHeal = PlayerRowViewModel.Kilo(combatants.Sum(c => c.Healing));
        TotalTaken = PlayerRowViewModel.Kilo(combatants.Sum(c => c.DamageTaken));
    }

    /// <summary>Everyone around, or only the local player.</summary>
    private bool IsShown(CombatantSnapshot c) => Scope == MemberScope.Everyone || c.IsSelf;

    /// <summary>Name, HP and clock of the header's target.</summary>
    private void ShowTarget(BossSnapshot? target, EncounterSnapshot? fight)
    {
        HasBoss = target is { Hp: >= 0 };
        BossName = target?.Name ?? fight?.Title ?? "";
        BossHp = HasBoss ? target!.HpFraction : 0;
        BossHpText = HasBoss ? Format.Percent(BossHp) : "";
        (ClockLabel, Clock) = ClockOf(target, fight);
    }

    private (string Label, string Value) ClockOf(BossSnapshot? target, EncounterSnapshot? fight)
    {
        // The fight's length, when the fight is against this target (or there is no target to time).
        var fightIsOnTarget = fight is not null && (target is null || (fight.Focus ?? fight.Boss)?.ActorId == target.ActorId);
        var length = fightIsOnTarget ? Format.Clock(fight!.CombatMs) : "--:--";

        if (target is not { Hp: >= 0 }) return fight is null ? ("", "--:--") : ("経過", length);
        if (target.Hp == 0) return ("撃破", length);
        if (fightIsOnTarget && !fight!.IsActive) return ("終了", length);
        var left = _killTime.Update(target.ActorId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), target.Hp);
        return ("撃破予測", left is { } t ? Format.Clock((long)t.TotalMilliseconds) : "--:--");
    }

    /// <summary>Updates each player's row in place and puts the rows in the given order.</summary>
    private void UpdateRows(List<CombatantSnapshot> combatants)
    {
        var max = new PartyMax(
            combatants.Select(c => c.FocusDamage).DefaultIfEmpty().Max(),
            combatants.Select(c => c.Healing).DefaultIfEmpty().Max(),
            combatants.Select(c => c.DamageTaken).DefaultIfEmpty().Max());

        var order = new List<PlayerRowViewModel>(combatants.Count);
        foreach (var c in combatants)
        {
            if (!_rows.TryGetValue(c.ActorId, out var row)) _rows[c.ActorId] = row = new PlayerRowViewModel();
            row.Update(order.Count + 1, c, max);
            order.Add(row);
        }

        // Players who left the fight (a new fight started, or the meter was reset).
        var present = combatants.Select(c => c.ActorId).ToHashSet();
        foreach (var id in _rows.Keys.Where(id => !present.Contains(id)).ToList()) _rows.Remove(id);

        ShowInOrder(order);
    }

    /// <summary>Moves, adds and removes rows so <see cref="Players"/> matches <paramref name="order"/>.</summary>
    private void ShowInOrder(List<PlayerRowViewModel> order)
    {
        for (var i = 0; i < order.Count; i++)
        {
            var at = Players.IndexOf(order[i]);
            if (at == i) continue;
            if (at < 0) Players.Insert(i, order[i]);
            else Players.Move(at, i);
        }
        while (Players.Count > order.Count) Players.RemoveAt(Players.Count - 1);
    }

    private void ShowNoFight()
    {
        _rows.Clear();
        Players.Clear();
        var capture = _meter.CaptureStatus;
        Status = capture.State == CaptureState.Capturing ? "計測対象との戦闘を待っています" : capture.Message;
        TotalDps = TotalHeal = TotalTaken = "";
    }

    /// <summary>Highest value of the chosen metric first; the unknown-summons row always goes last.</summary>
    private static List<CombatantSnapshot> SortBy(IEnumerable<CombatantSnapshot> combatants, SortKey key)
    {
        Func<CombatantSnapshot, double> metric = key switch
        {
            SortKey.Damage => c => c.FocusDamage,
            SortKey.Healing => c => c.Healing,
            SortKey.DamageTaken => c => c.DamageTaken,
            _ => c => c.RecentDps,
        };
        return combatants.OrderBy(c => c.IsUnknownSummons).ThenByDescending(metric).ToList();
    }
}
