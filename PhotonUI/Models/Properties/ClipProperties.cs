using PhotonUI.Interfaces;
using PhotonUI.Services;

namespace PhotonUI.Models.Properties
{
    public interface IClipProperties : IStyleProperties
    {
        int FramesWide { get; }
        int FramesTall { get; }
        int OffsetX { get; }
        int OffsetY { get; }
        IReadOnlyDictionary<int, ulong> FrameDurations { get; }
    }

    public readonly record struct ClipProperties(
        int FramesWide,
        int FramesTall,
        int OffsetX,
        int OffsetY,
        float PlaybackSpeed,
        bool Looping,
        PlaybackDirection Direction,
        IReadOnlyDictionary<int, ulong> FrameDurations
    ) : IClipProperties
    {
        public static ClipProperties Default => new(
            1,
            1,
            0,
            0,
            1.0f,
            true,
            PlaybackDirection.Forward,
            new Dictionary<int, ulong> { { 0, 1000 } }
        );
    }
}