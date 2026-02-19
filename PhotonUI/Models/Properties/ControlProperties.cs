using PhotonUI.Interfaces;
using SDL3;

namespace PhotonUI.Models.Properties
{
    public enum HorizontalAlignment
    {
        Left,
        Center,
        Right,
        Stretch
    }
    public enum VerticalAlignment
    {
        Top,
        Center,
        Bottom,
        Stretch
    }

    public interface IControlProperties : IStyleProperties
    {
        SDL.Color BackgroundColor { get; }
        float Opacity { get; }
        Thickness Margin { get; }
        Thickness Padding { get; }
        HorizontalAlignment HorizontalAlignment { get; }
        VerticalAlignment VerticalAlignment { get; }
        SDL.SystemCursor Cursor { get; }
        bool ClipToBounds { get; }
        float MinHeight { get; }
        float MinWidth { get; }
        float MaxHeight { get; }
        float MaxWidth { get; }
        bool IsHitTestVisible { get; }
        bool FocusOnHover { get; }
        int TabIndex { get; }
        int ZIndex { get; }
    }

    public readonly record struct ControlProperties(
        SDL.Color BackgroundColor,
        float Opacity,
        Thickness Margin,
        Thickness Padding,
        HorizontalAlignment HorizontalAlignment,
        VerticalAlignment VerticalAlignment,
        SDL.SystemCursor Cursor,
        bool ClipToBounds,
        float MinHeight,
        float MinWidth,
        float MaxHeight,
        float MaxWidth,
        bool IsHitTestVisible,
        bool FocusOnHover,
        int TabIndex,
        int ZIndex
    ) : IControlProperties
    {
        public static ControlProperties Default => new(
            new SDL.Color { R = 0, G = 0, B = 0, A = 0 },
            1.0f,
            Thickness.Empty,
            Thickness.Empty,
            HorizontalAlignment.Left,
            VerticalAlignment.Top,
            SDL.SystemCursor.Default,
            true,
            0f,
            0f,
            float.PositiveInfinity,
            float.PositiveInfinity,
            true,
            true,
            -1,
            0
        );
    }
}
