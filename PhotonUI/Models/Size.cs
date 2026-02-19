namespace PhotonUI.Models
{
    public struct Size
    {
        public float Width { get; set; }
        public float Height { get; set; }

        public Size(float width, float height)
        {
            this.Width = width;
            this.Height = height;
        }
        public Size(float extent)
        {
            this.Width = extent;
            this.Height = extent;
        }

        public static Size operator +(Size a, Size b)
            => new(a.Width + b.Width, a.Height + b.Height);
        public static Size operator -(Size a, Size b)
            => new(Math.Max(0, a.Width - b.Width), Math.Max(0, a.Height - b.Height));

        public static Size Empty { get; } = new Size(0, 0);
        public readonly bool IsEmpty => this.Width == 0 && this.Height == 0;

        public override readonly string ToString() => $"{this.Width}x{this.Height}";

        public readonly Size Inflate(Thickness thickness)
            => new(this.Width + thickness.Horizontal, this.Height + thickness.Vertical);
        public readonly Size Deflate(Thickness thickness)
            => new(Math.Max(0, this.Width - thickness.Horizontal), Math.Max(0, this.Height - thickness.Vertical));
    }
}