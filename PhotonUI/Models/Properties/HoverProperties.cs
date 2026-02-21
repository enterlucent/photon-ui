using PhotonUI.Interfaces;
using SDL3;

namespace PhotonUI.Models.Properties
{
    public interface IHoverProperties : IStyleProperties
    {
        SDL.Color HoverBackgroundColor { get; }
        BorderColors HoverBorderColors { get; }
        Thickness HoverBorderThickness { get; }
        SDL.Color HoverTextColor { get; }
        float HoverOpacity { get; }
        float HoverScale { get; }
    }

    public readonly record struct HoverProperties(
        SDL.Color HoverBackgroundColor,
        BorderColors HoverBorderColors,
        Thickness HoverBorderThickness,
        SDL.Color HoverTextColor,
        float HoverOpacity,
        float HoverScale) : IHoverProperties
    {
        public static HoverProperties Default => new(
            new SDL.Color { R = 240, G = 240, B = 240, A = 255 },
            new BorderColors(new SDL.Color { R = 200, G = 200, B = 200, A = 255 }),
            new Thickness(1),
            new SDL.Color { R = 0, G = 0, B = 0, A = 255 },
            1.0f,
            1.0f
        );
    }
}