using SDL3;

namespace PhotonUI.Services.Interpolators
{
    public sealed class SDLColorInterpolator : IInterpolator<SDL.Color>
    {
        public SDL.Color Lerp(SDL.Color start, SDL.Color end, float progress)
        {
            progress = Math.Clamp(progress, 0f, 1f);

            return new SDL.Color
            {
                R = (byte)(start.R + (end.R - start.R) * progress),
                G = (byte)(start.G + (end.G - start.G) * progress),
                B = (byte)(start.B + (end.B - start.B) * progress),
                A = (byte)(start.A + (end.A - start.A) * progress)
            };
        }
    }
}