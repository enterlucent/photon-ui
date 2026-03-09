using CommunityToolkit.Mvvm.ComponentModel;
using PhotonUI.Interfaces;
using PhotonUI.Interfaces.Services;
using PhotonUI.Models;
using PhotonUI.Models.Properties;
using SDL3;
using System.ComponentModel;

namespace PhotonUI.Controls.Content
{
    public partial class ImageBlock(IServiceProvider serviceProvider, IBindingService bindingService, ITextureService textureService)
        : Control(serviceProvider, bindingService), IImageProperties, IStretchProperties
    {
        protected readonly ITextureService TextureService = textureService;

        protected bool IsImageDirty = true;

        protected IntPtr? SourceTexture = null;
        protected Size SourceTextureSize = Size.Empty;
        protected Size SourceControlSize = Size.Empty;

        [ObservableProperty] private string imageSourceName = ImageProperties.Default.ImageSourceName;
        [ObservableProperty] private SDL.FRect? imageSourceRect = ImageProperties.Default.ImageSourceRect;
        [ObservableProperty] private SDL.Color imageTintColor = ImageProperties.Default.ImageTintColor;
        [ObservableProperty] private SDL.BlendMode imageBlendMode = ImageProperties.Default.ImageBlendMode;

        [ObservableProperty] private StretchMode stretchMode = StretchProperties.Default.StretchMode;
        [ObservableProperty] private StretchDirection stretchDirection = StretchProperties.Default.StretchDirection;

        #region Image: Framework

        public override void ApplyStyles(params IStyleProperties[] properties)
        {
            this.ValidateStyles(properties);

            base.ApplyStyles(properties);

            foreach (IStyleProperties prop in properties)
            {
                switch (prop)
                {
                    case IImageProperties props:
                        this.ApplyProperties(props);
                        break;
                    case IStretchProperties props:
                        this.ApplyProperties(props);
                        break;
                }
            }
        }

        public override void RequestRender(bool invalidate = true)
        {
            this.IsImageDirty = true;

            base.RequestRender(invalidate);
        }
        public virtual void RequestRenderWithFlags(bool imageDirty = false, bool invalidate = true)
        {
            if (imageDirty)
                this.IsImageDirty = true;

            base.RequestRender(invalidate);
        }

        public override void FrameworkInitialize(Window window)
        {
            base.FrameworkInitialize(window);

            if (this.LoadSourceTexture(window))
                this.RebuildTexture(window);
        }
        public override void FrameworkRender(Window window, SDL.Rect? clipRect)
        {
            if (this.IsVisible)
            {
                Photon.GetControlClipRect(this.DrawRect, this.ClipToBounds, clipRect, out SDL.Rect? effectiveClipRect);

                if (effectiveClipRect.HasValue)
                {
                    Photon.ApplyControlClipRect(window, effectiveClipRect);

                    Photon.DrawControlBackground(this);

                    if (this.SourceTexture.HasValue && this.SourceTexture.Value != IntPtr.Zero)
                    {
                        if (this.IsImageDirty)
                        {
                            this.RebuildTexture(window);
                            this.IsImageDirty = false;
                        }

                        SDL.FRect destination = new()
                        {
                            X = this.DrawRect.X,
                            Y = this.DrawRect.Y,
                            W = this.SourceControlSize.Width > 0f ? this.SourceControlSize.Width : this.SourceTextureSize.Width,
                            H = this.SourceControlSize.Height > 0f ? this.SourceControlSize.Height : this.SourceTextureSize.Height
                        };

                        if (this.ImageSourceRect.HasValue)
                            Photon.DrawControlTexture(this, this.SourceTexture.Value, destination, this.ImageSourceRect.Value, effectiveClipRect.Value);
                        else
                            Photon.DrawControlTexture(this, this.SourceTexture.Value, destination, effectiveClipRect.Value);
                    }

                    Photon.ApplyControlClipRect(window, clipRect);

                    this.RenderedAction?.Invoke(this);
                }
            }
        }

        #endregion

        #region Image: Hooks

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (!this.IsInitialized)
                return;

            if (this.Window == null)
                throw new InvalidOperationException("Image property change: RootWindow == null.");

            base.OnPropertyChanged(e);

            bool invalidateMeasure = false;
            bool invalidateLayout = false;
            bool invalidateRender = false;
            bool invalidateImage = false;

            switch (e.PropertyName)
            {
                case nameof(this.ImageSourceName):
                case nameof(this.ImageSourceRect):
                    invalidateMeasure = true;
                    invalidateLayout = true;
                    invalidateRender = true;
                    invalidateImage = true;
                    break;
            }

            if (invalidateMeasure)
                this.Parent?.RequestMeasure();

            if (invalidateLayout)
                this.Parent?.RequestArrange();

            if (invalidateRender)
                this.RequestRenderWithFlags(imageDirty: invalidateImage);
        }

        public override void Dispose()
        {
            if (this.SourceTexture.HasValue && this.SourceTexture.Value != IntPtr.Zero)
            {
                SDL.DestroyTexture(this.SourceTexture.Value);

                this.SourceTexture = null;
            }

            GC.SuppressFinalize(this);

            base.Dispose();
        }

        #endregion

        #region Image: Helpers

        protected virtual void RebuildTexture(Window window)
        {
            if (!this.SourceTexture.HasValue || this.SourceTexture.Value == IntPtr.Zero)
                return;

            byte alpha = (byte)Math.Clamp((int)(this.Opacity * 255), 0, 255);

            SDL.SetTextureColorMod(this.SourceTexture.Value, this.ImageTintColor.R, this.ImageTintColor.G, this.ImageTintColor.B);
            SDL.SetTextureAlphaMod(this.SourceTexture.Value, alpha);
            SDL.SetTextureBlendMode(this.SourceTexture.Value, this.ImageBlendMode);

            this.SourceControlSize =
                Photon.GetScaledSize(
                    new Size(this.DrawRect.W, this.DrawRect.H),
                    new Size(this.SourceTextureSize.Width, this.SourceTextureSize.Height),
                    this.FromControl<StretchProperties>());
        }

        protected virtual bool LoadSourceTexture(Window window)
        {
            if (this.SourceTexture.HasValue && this.SourceTexture.Value != IntPtr.Zero)
            {
                SDL.DestroyTexture(this.SourceTexture.Value);

                this.SourceTexture = null;
            }

            if (string.IsNullOrEmpty(this.ImageSourceName))
            {
                this.SourceControlSize = new Size(0);
                this.SourceTextureSize = Size.Empty;

                return false;
            }

            if (this.TextureService.CreateTexture(window.Renderer, this.ImageSourceName, out nint texture, out Size natural))
            {
                this.SourceTexture = texture;
                this.SourceTextureSize = natural;

                return true;
            }

            this.SourceTexture = IntPtr.Zero;
            this.SourceTextureSize = Size.Empty;
            this.SourceControlSize = new Size(0);

            return false;
        }

        #endregion
    }
}