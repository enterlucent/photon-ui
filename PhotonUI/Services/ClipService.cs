using PhotonUI.Components;
using PhotonUI.Interfaces.Clips;
using PhotonUI.Interfaces.Services;
using PhotonUI.Models.Clips;
using SDL3;

namespace PhotonUI.Services
{
    public enum PlaybackDirection
    {
        Forward,
        Reverse
    }

    public class ClipService : IClipService
    {
        public ClipSequence BuildFromSpriteSheet(IntPtr texture, int framesWide, int framesTall, int offsetX = 0, int offsetY = 0)
        {
            SDL.GetTextureSize(texture, out float texWidth, out float texHeight);

            int frameWidth = (int)(texWidth - offsetX) / framesWide;
            int frameHeight = (int)(texHeight - offsetY) / framesTall;

            ClipSequence clip = new();

            for (int y = 0; y < framesTall; y++)
            {
                for (int x = 0; x < framesWide; x++)
                {
                    SDL.FRect rect = new()
                    {
                        X = offsetX + x * frameWidth,
                        Y = offsetY + y * frameHeight,
                        W = frameWidth,
                        H = frameHeight
                    };

                    clip.AddFrame(new ClipFrame(texture, rect));
                }
            }

            return clip;
        }
        public ClipPlayer GetPlayer(IntPtr texture, int framesWide, int framesTall, int offsetX = 0, int offsetY = 0)
        {
            ClipSequence sequence = this.BuildFromSpriteSheet(texture, framesWide, framesTall, offsetX, offsetY);

            return new ClipPlayer(sequence);
        }
    }
}