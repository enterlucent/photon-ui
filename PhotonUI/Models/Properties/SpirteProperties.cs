using PhotonUI.Interfaces;
using PhotonUI.Services;
using SDL3;
using System.Numerics;

namespace PhotonUI.Models.Properties
{
    public interface ISpriteProperties : IStyleProperties
    {
        bool FlipX { get; }
        bool FlipY { get; }
        float Rotation { get; }
        Vector2 Scale { get; }
        SDL.FPoint AnchorPoint { get; }
        float PlaybackSpeed { get; }
        bool Looping { get; }
        PlaybackDirection Direction { get; }
        bool BakePlayback { get; }
    }

    public readonly record struct SpriteProperties(
        bool FlipX,
        bool FlipY,
        float Rotation,
        Vector2 Scale,
        SDL.FPoint AnchorPoint,
        float PlaybackSpeed,
        bool Looping,
        PlaybackDirection Direction,
        bool BakePlayback) : ISpriteProperties
    {
        public static SpriteProperties Default => new(
            false,
            false,
            0f,
            new Vector2(1f, 1f),
            new SDL.FPoint() { X = 0.5f, Y = 0.5f },
            1.0f,
            true,
            PlaybackDirection.Forward,
            true
        );
    }
}