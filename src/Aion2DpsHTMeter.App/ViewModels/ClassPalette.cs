using System.Windows.Media;
using AionMeter.Core.Events;

namespace Aion2DpsHTMeter.App.ViewModels;

/// <summary>
/// Colour of the class bar at the left of each row. Values from A2Tools-DPS-Meter's jobColorMap
/// (public/src/js/core.js), which follow the hues of the in-game class emblems.
/// </summary>
public static class ClassPalette
{
    private static readonly Dictionary<GameClass, Brush> Brushes = new()
    {
        [GameClass.Gladiator] = Make("#4FD1C5"),    // 剣星
        [GameClass.Templar] = Make("#5F8CFF"),      // 守護星
        [GameClass.Assassin] = Make("#7BE35A"),     // 殺星
        [GameClass.Ranger] = Make("#41D98A"),       // 弓星
        [GameClass.Sorcerer] = Make("#9A6BFF"),     // 魔道星
        [GameClass.Elementalist] = Make("#E06BFF"), // 精霊星
        [GameClass.Cleric] = Make("#F2C15A"),       // 治癒星
        [GameClass.Chanter] = Make("#FF9A3D"),      // 護法星
        [GameClass.Brawler] = Make("#E85D5D"),      // 拳星
    };

    private static readonly Brush Unknown = Make("#94A3B8");

    public static Brush Of(GameClass c) => Brushes.GetValueOrDefault(c, Unknown);

    private static Brush Make(string hex)
    {
        var brush = (Brush)new BrushConverter().ConvertFromString(hex)!;
        brush.Freeze();
        return brush;
    }
}
