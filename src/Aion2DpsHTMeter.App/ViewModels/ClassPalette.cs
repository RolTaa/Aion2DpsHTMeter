using System.Windows.Media;
using AionMeter.Core.Events;

namespace Aion2DpsHTMeter.App.ViewModels;

/// <summary>Colour of the class bar at the left of each row (same values as the UI mockup).</summary>
public static class ClassPalette
{
    private static readonly Dictionary<GameClass, SolidColorBrush> Brushes = new()
    {
        [GameClass.Gladiator] = Make(0xE8, 0xA3, 0x3D),    // 剣星
        [GameClass.Templar] = Make(0x5B, 0x8D, 0xEF),      // 守護星
        [GameClass.Assassin] = Make(0xB0, 0x5C, 0xE0),     // 殺星
        [GameClass.Ranger] = Make(0x6C, 0xCB, 0x5F),       // 弓星
        [GameClass.Sorcerer] = Make(0xE8, 0x57, 0x4A),     // 魔道星
        [GameClass.Elementalist] = Make(0x3E, 0xC6, 0xC6), // 精霊星
        [GameClass.Cleric] = Make(0xEF, 0xE3, 0xBD),       // 治癒星
        [GameClass.Chanter] = Make(0xE0, 0x7A, 0xB8),      // 護法星
        [GameClass.Brawler] = Make(0xC7, 0xD3, 0x6B),
    };

    private static readonly SolidColorBrush Unknown = Make(0x6B, 0x72, 0x80);

    public static SolidColorBrush Of(GameClass c) => Brushes.TryGetValue(c, out var b) ? b : Unknown;

    private static SolidColorBrush Make(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
