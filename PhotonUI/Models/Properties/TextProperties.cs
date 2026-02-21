using PhotonUI.Interfaces;
using PhotonUI.Services;
using SDL3;

namespace PhotonUI.Models.Properties
{
    public enum TextControlWrapType
    {
        None,
        Soft,
        Hard,
        AutoSoft,
        AutoHard
    }

    public interface ITextProperties : IStyleProperties
    {
        string FontFamily { get; }
        string FontStyle { get; }
        int FontSize { get; }

        FontRenderMode FontRenderMode { get; }
        TTF.FontStyleFlags FontStyleFlags { get; }
        TTF.HintingFlags FontHintingFlags { get; }
        bool FontKerning { get; }

        SDL.Color TextForegroundColor { get; }
        SDL.Color TextBackgroundColor { get; }
        SDL.Color TextOutlineColor { get; }

        int TextOutlineSize { get; }

        TextControlWrapType TextWrapType { get; }
        int TextWrapLength { get; }
    }

    public readonly record struct TextProperties(
        string FontFamily,
        string FontStyle,
        int FontSize,
        FontRenderMode FontRenderMode,
        TTF.FontStyleFlags FontStyleFlags,
        TTF.HintingFlags FontHintingFlags,
        bool FontKerning,
        SDL.Color TextForegroundColor,
        SDL.Color TextBackgroundColor,
        SDL.Color TextOutlineColor,
        int TextOutlineSize,
        int TextWrapLength,
        TextControlWrapType TextWrapType) : ITextProperties
    {
        public static TextProperties Default => new(
            "DejaVu-Sans-Mono",
            "Regular",
            16,
            FontRenderMode.Blended,
            TTF.FontStyleFlags.Normal,
            TTF.HintingFlags.Normal,
            true,
            new SDL.Color { R = 223, G = 223, B = 223, A = 255 },
            new SDL.Color { R = 0, G = 0, B = 0, A = 0 },
            new SDL.Color { R = 32, G = 32, B = 32, A = 255 },
            1,
            0,
            TextControlWrapType.None
        );
    }
}