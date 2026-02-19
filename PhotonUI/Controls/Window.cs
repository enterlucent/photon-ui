using SDL3;

namespace PhotonUI.Controls
{
    public partial class Window(IServiceProvider serviceProvider)
        : Presenter(serviceProvider)
    {
        protected IntPtr WindowBackTexture;

        public IntPtr BackTexture => this.WindowBackTexture;

        #region Window: Platform

        public IntPtr Handle { get; protected set; }
        public IntPtr Renderer { get; protected set; }

        #endregion

        #region Window: External

        public void GetScreenshot(string path)
        {
            ArgumentNullException.ThrowIfNull(this);

            if (this.BackTexture == IntPtr.Zero)
                throw new InvalidOperationException("BackTexture not created.");

            IntPtr renderer = this.Renderer;
            IntPtr texture = this.BackTexture;

            if (!SDL.GetTextureSize(texture, out float texWf, out float texHf))
                throw new InvalidOperationException("Failed to query texture size.");

            int texW = (int)texWf;
            int texH = (int)texHf;

            if (texW <= 0 || texH <= 0) throw new InvalidOperationException("Invalid texture size.");

            IntPtr prevTarget = SDL.GetRenderTarget(renderer);

            SDL.SetRenderTarget(renderer, texture);
            SDL.Rect fullRect = new() { X = 0, Y = 0, W = texW, H = texH };

            IntPtr surface = SDL.RenderReadPixels(renderer, fullRect);

            if (surface == IntPtr.Zero)
            {
                SDL.SetRenderTarget(renderer, prevTarget);

                throw new InvalidOperationException($"RenderReadPixels failed: {SDL.GetError()}");
            }

            SDL.SetRenderTarget(renderer, prevTarget);

            try
            {
                if (!SDL.SavePNG(surface, path))
                    throw new InvalidOperationException($"SavePNG failed: {SDL.GetError()}");
            }
            finally
            {
                try
                { SDL.DestroySurface(surface); }
                catch
                { try { SDL.Free(surface); } catch { } }
            }
        }

        #endregion
    }
}