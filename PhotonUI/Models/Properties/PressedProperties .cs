using PhotonUI.Interfaces;
using SDL3;

namespace PhotonUI.Models.Properties
{
    public interface IPressedProperties : IStyleProperties
    {
        SDL.Color PressedBackgroundColor { get; }
        BorderColors PressedBorderColors { get; }
        Thickness PressedBorderThickness { get; }
        SDL.Color PressedTextColor { get; }
        float PressedOpacity { get; }
        float PressedScale { get; }
    }

    public readonly record struct PressedProperties(
        SDL.Color PressedBackgroundColor,
        BorderColors PressedBorderColors,
        Thickness PressedBorderThickness,
        SDL.Color PressedTextColor,
        float PressedOpacity,
        float PressedScale) : IPressedProperties
    {
        public static PressedProperties Default => new(
            new SDL.Color { R = 220, G = 220, B = 220, A = 255 },
            new BorderColors(new SDL.Color { R = 160, G = 160, B = 160, A = 255 }),
            new Thickness(1),
            new SDL.Color { R = 0, G = 0, B = 0, A = 255 },
            0.95f,
            0.98f);
    }
}