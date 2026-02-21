using PhotonUI.Interfaces;
using SDL3;

namespace PhotonUI.Models.Properties
{
    public interface ITextSelectionProperties : IStyleProperties
    {
        SDL.Color SelectionColor { get; }
    }

    public readonly record struct TextSelectionProperties(
        SDL.Color SelectionColor) : ITextSelectionProperties
    {
        public static TextSelectionProperties Default => new(
            new SDL.Color { R = 0, G = 120, B = 215, A = 128 }
        );
    }
}