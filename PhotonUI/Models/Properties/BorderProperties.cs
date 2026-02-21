using PhotonUI.Interfaces;
using SDL3;

namespace PhotonUI.Models.Properties
{
    public interface IBorderProperties : IStyleProperties
    {
        BorderColors BorderColors { get; }
        Thickness BorderThickness { get; }
    }

    public readonly record struct BorderProperties(
        BorderColors BorderColors,
        Thickness BorderThickness) : IBorderProperties
    {
        public static BorderProperties Default => new(
            new BorderColors(new SDL.Color { R = 255, G = 255, B = 255, A = 255 }),
            new Thickness(1)
        );
    }
}