using SDL3;

namespace PhotonUI.Models.Clips
{
    public class ClipFrame(IntPtr texture, SDL.FRect sourceRect, int intervalMs = 100)
    {
        public IntPtr Texture { get; } = texture;
        public SDL.FRect SourceRect { get; } = sourceRect;
        public int IntervalMs { get; } = intervalMs;
    }
}