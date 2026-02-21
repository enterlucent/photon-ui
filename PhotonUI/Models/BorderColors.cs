using SDL3;

namespace PhotonUI.Models
{
    public readonly struct BorderColors
    {
        public SDL.Color Top { get; }
        public SDL.Color Right { get; }
        public SDL.Color Bottom { get; }
        public SDL.Color Left { get; }

        public BorderColors(SDL.Color uniform)
            : this(uniform, uniform, uniform, uniform) { }

        public BorderColors(SDL.Color top, SDL.Color right, SDL.Color bottom, SDL.Color left)
        {
            this.Top = top;
            this.Right = right;
            this.Bottom = bottom;
            this.Left = left;
        }

        public BorderColors(SDL.Color lr, SDL.Color tb)
        {
            this.Left = lr;
            this.Right = lr;
            this.Top = tb;
            this.Bottom = tb;
        }

        public BorderColors WithOpacity(float opacity)
        {
            return new BorderColors(
                AdjustAlpha(this.Top, opacity),
                AdjustAlpha(this.Right, opacity),
                AdjustAlpha(this.Bottom, opacity),
                AdjustAlpha(this.Left, opacity)
            );
        }

        private static SDL.Color AdjustAlpha(SDL.Color c, float opacity)
        {
            return new SDL.Color
            {
                R = c.R,
                G = c.G,
                B = c.B,
                A = (byte)(c.A * opacity)
            };
        }
    }
}