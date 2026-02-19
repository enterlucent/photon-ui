namespace PhotonUI.Models
{
    public readonly struct Thickness(float left, float top, float right, float bottom)
    {
        public float Left { get; } = left;
        public float Top { get; } = top;
        public float Right { get; } = right;
        public float Bottom { get; } = bottom;

        public static Thickness operator +(Thickness a, Thickness b)
            => new(
                a.Left + b.Left,
                a.Top + b.Top,
                a.Right + b.Right,
                a.Bottom + b.Bottom);
        public static Thickness operator -(Thickness a, Thickness b)
            => new(
                a.Left - b.Left,
                a.Top - b.Top,
                a.Right - b.Right,
                a.Bottom - b.Bottom);

        public Thickness(float uniform) : this(uniform, uniform, uniform, uniform) { }
        public Thickness(float lr, float tb) : this(lr, tb, lr, tb) { }

        public float Horizontal => this.Left + this.Right;
        public float Vertical => this.Top + this.Bottom;

        public static Thickness Empty => new(0);
    }
}