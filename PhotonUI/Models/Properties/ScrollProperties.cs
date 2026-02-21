using PhotonUI.Interfaces;
using SDL3;

namespace PhotonUI.Models.Properties
{
    public record ScrollContext(float HOffset, float HFullSize, float VOffset, float VFullSize)
    {
        public float HNormalized => this.HFullSize > 0 ? Math.Clamp(this.HOffset / this.HFullSize, 0f, 1f) : 0f;
        public float VNormalized => this.VFullSize > 0 ? Math.Clamp(this.VOffset / this.VFullSize, 0f, 1f) : 0f;
        public (float HOffset, float HFullSize, float VOffset, float VFullSize) Deconstruct() => (this.HOffset, this.HFullSize, this.VOffset, this.VFullSize);
    }

    public enum ScrollViewportAnchor
    {
        Top,
        Bottom,
        Left,
        Right,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public enum ScrollDirection
    {
        None,
        Vertical,
        Horizontal,
        Both
    }

    public interface IScrollProperties : IStyleProperties
    {
        ScrollViewportAnchor ScrollViewportAnchor { get; }
        ScrollDirection ScrollDirection { get; }
        float ScrollX { get; }
        float ScrollY { get; }
        float ScrollStepX { get; }
        float ScrollStepY { get; }
        float KeyboardScrollStepX { get; }
        float KeyboardScrollStepY { get; }
        float WheelScrollStepX { get; }
        float WheelScrollStepY { get; }
        float ScrollStepMultiplierX { get; }
        float ScrollStepMultiplierY { get; }

        SDL.Color ScrollCaretBackgroundColor { get; }
        SDL.Color ScrollTrackBackgroundColor { get; }
        Thickness ScrollTrackThickness { get; }
        Thickness ScrollCaretThickness { get; }

        Size MinScrollviewSize { get; }
    }

    public readonly record struct ScrollProperties(
        ScrollViewportAnchor ScrollViewportAnchor,
        ScrollDirection ScrollDirection,
        float ScrollX,
        float ScrollY,
        float ScrollStepX,
        float ScrollStepY,
        float KeyboardScrollStepX,
        float KeyboardScrollStepY,
        float WheelScrollStepX,
        float WheelScrollStepY,
        float ScrollStepMultiplierX,
        float ScrollStepMultiplierY,
        SDL.Color ScrollCaretBackgroundColor,
        SDL.Color ScrollTrackBackgroundColor,
        Thickness ScrollTrackThickness,
        Thickness ScrollCaretThickness,
        Size MinScrollviewSize
    ) : IScrollProperties
    {
        public static ScrollProperties Default => new(
            ScrollViewportAnchor.Top,
            ScrollDirection.None,
            0f, 0f,
            32f, 32f,
            32f, 32f,
            32f, 32f,
            1f, 1f,
            new SDL.Color() { R = 55, G = 55, B = 55, A = 255 },
            new SDL.Color() { R = 200, G = 200, B = 200, A = 255 },
            new Thickness(8),
            new Thickness(8),
            new Size(32, 32)
        );
    }
}
