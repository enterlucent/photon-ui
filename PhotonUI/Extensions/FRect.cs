using PhotonUI.Models;
using SDL3;

namespace PhotonUI.Extensions
{
    public static class Rect
    {
        public static SDL.Rect Offset(this SDL.Rect rect, float dx, float dy)
            => new() { X = rect.X + (int)dx, Y = rect.Y + (int)dy, W = rect.W, H = rect.H };
        public static SDL.Rect Deflate(this SDL.Rect rect, float left, float top, float right, float bottom)
            => new()
            {
                X = rect.X + (int)left,
                Y = rect.Y + (int)top,
                W = rect.W - (int)(left + right),
                H = rect.H - (int)(top + bottom)
            };
        public static SDL.Rect Inflate(this SDL.Rect rect, float left, float top, float right, float bottom)
            => rect.Deflate(-left, -top, -right, -bottom);

        public static SDL.Rect Offset(this SDL.Rect rect, Size size)
            => rect.Offset(size.Width, size.Height);
        public static SDL.Rect Deflate(this SDL.Rect rect, Thickness thickness)
            => rect.Deflate(thickness.Left, thickness.Top, thickness.Right, thickness.Bottom);
        public static SDL.Rect Inflate(this SDL.Rect rect, Thickness thickness)
            => rect.Inflate(thickness.Left, thickness.Top, thickness.Right, thickness.Bottom);

        public static SDL.Rect Deflate(this SDL.Rect rect, Size size)
            => rect.Deflate(size.Width / 2f, size.Height / 2f, size.Width / 2f, size.Height / 2f);
        public static SDL.Rect Inflate(this SDL.Rect rect, Size size)
            => rect.Inflate(size.Width / 2f, size.Height / 2f, size.Width / 2f, size.Height / 2f);

        public static string ToStr(this SDL.Rect rect, string? format = null)
            => $"({rect.X},{rect.Y},{rect.W},{rect.H}){(format != null ? $":{format}" : "")}";
    }

    public static class RectNullable
    {
        public static SDL.Rect? ToRect(this SDL.FRect? rect)
            => rect.HasValue ? rect.Value.ToRect() : null;

        public static SDL.Rect? Offset(this SDL.Rect? rect, float dx, float dy)
            => rect.HasValue ? rect.Value.Offset(dx, dy) : null;
        public static SDL.Rect? Deflate(this SDL.Rect? rect, float left, float top, float right, float bottom)
            => rect.HasValue ? rect.Value.Deflate(left, top, right, bottom) : null;
        public static SDL.Rect? Inflate(this SDL.Rect? rect, float left, float top, float right, float bottom)
            => rect.HasValue ? rect.Value.Inflate(left, top, right, bottom) : null;

        public static SDL.Rect? Offset(this SDL.Rect? rect, Size size)
            => rect.Offset(size.Width, size.Height);
        public static SDL.Rect? Deflate(this SDL.Rect? rect, Thickness thickness)
            => rect.Deflate(thickness.Left, thickness.Top, thickness.Right, thickness.Bottom);
        public static SDL.Rect? Inflate(this SDL.Rect? rect, Thickness thickness)
            => rect.Inflate(thickness.Left, thickness.Top, thickness.Right, thickness.Bottom);

        public static SDL.Rect? Deflate(this SDL.Rect? rect, Size size)
            => rect.Deflate(size.Width / 2f, size.Height / 2f, size.Width / 2f, size.Height / 2f);
        public static SDL.Rect? Inflate(this SDL.Rect? rect, Size size)
            => rect.Inflate(size.Width / 2f, size.Height / 2f, size.Width / 2f, size.Height / 2f);

        public static string ToStr(this SDL.Rect? rect, string? format = null)
            => rect.HasValue ? rect.Value.ToStr(format) : "null";
    }

    public static class FRect
    {
        public static SDL.Rect ToRect(this SDL.FRect rect)
            => new()
            {
                X = (int)rect.X,
                Y = (int)rect.Y,
                W = (int)rect.W,
                H = (int)rect.H
            };

        public static SDL.FRect Offset(this SDL.FRect rect, float dx, float dy)
            => new() { X = rect.X + dx, Y = rect.Y + dy, W = rect.W, H = rect.H };
        public static SDL.FRect Deflate(this SDL.FRect rect, float left, float top, float right, float bottom)
            => new()
            {
                X = rect.X + left,
                Y = rect.Y + top,
                W = rect.W - (left + right),
                H = rect.H - (top + bottom)
            };
        public static SDL.FRect Inflate(this SDL.FRect rect, float left, float top, float right, float bottom)
            => rect.Deflate(-left, -top, -right, -bottom);

        public static SDL.FRect Offset(this SDL.FRect rect, Size size)
            => rect.Offset(size.Width, size.Height);
        public static SDL.FRect Deflate(this SDL.FRect rect, Thickness thickness)
            => rect.Deflate(thickness.Left, thickness.Top, thickness.Right, thickness.Bottom);
        public static SDL.FRect Inflate(this SDL.FRect rect, Thickness thickness)
            => rect.Inflate(thickness.Left, thickness.Top, thickness.Right, thickness.Bottom);

        public static SDL.FRect Deflate(this SDL.FRect rect, Size size)
            => rect.Deflate(size.Width / 2f, size.Height / 2f, size.Width / 2f, size.Height / 2f);
        public static SDL.FRect Inflate(this SDL.FRect rect, Size size)
            => rect.Inflate(size.Width / 2f, size.Height / 2f, size.Width / 2f, size.Height / 2f);

        public static string ToStr(this SDL.FRect rect, string? format = null)
            => $"({rect.X:F1},{rect.Y:F1},{rect.W:F1},{rect.H:F1}){(format != null ? $":{format}" : "")}";
    }

    public static class FRectNullable
    {
        public static SDL.FRect? Offset(this SDL.FRect? rect, float dx, float dy)
            => rect.HasValue ? rect.Value.Offset(dx, dy) : null;
        public static SDL.FRect? Deflate(this SDL.FRect? rect, float left, float top, float right, float bottom)
            => rect.HasValue ? rect.Value.Deflate(left, top, right, bottom) : null;
        public static SDL.FRect? Inflate(this SDL.FRect? rect, float left, float top, float right, float bottom)
            => rect.HasValue ? rect.Value.Inflate(left, top, right, bottom) : null;

        public static SDL.FRect? Offset(this SDL.FRect? rect, Size size)
            => rect.Offset(size.Width, size.Height);
        public static SDL.FRect? Deflate(this SDL.FRect? rect, Thickness thickness)
            => rect.Deflate(thickness.Left, thickness.Top, thickness.Right, thickness.Bottom);
        public static SDL.FRect? Inflate(this SDL.FRect? rect, Thickness thickness)
            => rect.Inflate(thickness.Left, thickness.Top, thickness.Right, thickness.Bottom);

        public static SDL.FRect? Deflate(this SDL.FRect? rect, Size size)
            => rect.Deflate(size.Width / 2f, size.Height / 2f, size.Width / 2f, size.Height / 2f);
        public static SDL.FRect? Inflate(this SDL.FRect? rect, Size size)
            => rect.Inflate(size.Width / 2f, size.Height / 2f, size.Width / 2f, size.Height / 2f);

        public static string ToStr(this SDL.FRect? rect, string? format = null)
            => rect.HasValue ? rect.Value.ToStr(format) : "null";
    }
}