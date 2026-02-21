using PhotonUI.Interfaces;
using SDL3;

namespace PhotonUI.Models.Properties
{
    public interface ITextCaretProperties : IStyleProperties
    {
        bool CaretVisibility { get; }
        int CaretBlinkRate { get; }
        int CaretWidth { get; }
        SDL.Color CaretColor { get; }
        Thickness CaretOutline { get; }
        BorderColors CaretOutlineColor { get; }
    }

    public readonly record struct TextCaretProperties(
        bool CaretVisibility,
        int CaretBlinkRate,
        SDL.Color CaretColor,
        int CaretWidth,
        Thickness CaretOutline,
        BorderColors CaretOutlineColor) : ITextCaretProperties
    {
        public static TextCaretProperties Default => new(
            true,
            500,
            new SDL.Color { R = 255, G = 245, B = 245, A = 255 },
            3,
            new Thickness(1),
            new BorderColors(new SDL.Color() { R = 32, G = 32, B = 32, A = 255 })
        );
    }
}