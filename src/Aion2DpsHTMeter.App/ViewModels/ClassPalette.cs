using System.Windows.Media;
using AionMeter.Core.Events;

namespace Aion2DpsHTMeter.App.ViewModels;

/// <summary>
/// Colour of the class bar at the left of each row. Values from A2Tools-DPS-Meter's jobColorMap
/// (public/src/js/core.js), which follow the hues of the in-game class emblems.
/// </summary>
public static class ClassPalette
{
    private static readonly Dictionary<GameClass, string> Hex = new()
    {
        [GameClass.Gladiator] = "#4FD1C5",    // 剣星
        [GameClass.Templar] = "#5F8CFF",      // 守護星
        [GameClass.Assassin] = "#7BE35A",     // 殺星
        [GameClass.Ranger] = "#41D98A",       // 弓星
        [GameClass.Sorcerer] = "#9A6BFF",     // 魔道星
        [GameClass.Elementalist] = "#E06BFF", // 精霊星
        [GameClass.Cleric] = "#F2C15A",       // 治癒星
        [GameClass.Chanter] = "#FF9A3D",      // 護法星
        [GameClass.Brawler] = "#E85D5D",      // 拳星
    };

    private const string UnknownHex = "#94A3B8";

    public static Brush Of(GameClass c)
    {
        var brush = (SolidColorBrush)new BrushConverter().ConvertFromString(Hex.GetValueOrDefault(c, UnknownHex))!;
        brush.Freeze();
        return brush;
    }
}
