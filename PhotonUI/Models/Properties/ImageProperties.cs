using PhotonUI.Interfaces;
using SDL3;

namespace PhotonUI.Models.Properties
{
    public interface IImageProperties : IStyleProperties
    {
        string ImageSourceName { get; }
        SDL.FRect? ImageSourceRect { get; }
        SDL.Color ImageTintColor { get; }
        SDL.BlendMode ImageBlendMode { get; }
    }

    public readonly record struct ImageProperties(
        string ImageSourceName,
        SDL.FRect? ImageSourceRect,
        SDL.Color ImageTintColor,
        SDL.BlendMode ImageBlendMode) : IImageProperties
    {
        public static ImageProperties Default => new(
            string.Empty,
            null,
            new SDL.Color { R = 255, G = 255, B = 255, A = 255 },
            SDL.BlendMode.Blend
        );
    }
}