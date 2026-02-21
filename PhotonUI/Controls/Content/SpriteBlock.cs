using CommunityToolkit.Mvvm.ComponentModel;
using PhotonUI.Components;
using PhotonUI.Interfaces;
using PhotonUI.Interfaces.Services;
using PhotonUI.Models;
using PhotonUI.Models.Clips;
using PhotonUI.Models.Properties;
using PhotonUI.Services;
using SDL3;
using System.ComponentModel;
using System.Numerics;

namespace PhotonUI.Controls.Content
{
    public partial class SpriteBlock(IServiceProvider serviceProvider, IBindingService bindingService, ITextureService textureService, IClipService clipService)
        : ImageBlock(serviceProvider, bindingService, textureService), ISpriteProperties, IClipProperties
    {
        protected readonly IClipService ClipService = clipService;

        protected bool IsPlayerDirty = true;
        protected ClipPlayer? Player;
        protected readonly Dictionary<int, IntPtr> FrameCache = [];
        protected IntPtr CachedTexture = IntPtr.Zero;
        protected SDL.FRect CachedDestination;
        protected SDL.FRect CachedSourceRect;

        [ObservableProperty] private bool flipX = SpriteProperties.Default.FlipX;
        [ObservableProperty] private bool flipY = SpriteProperties.Default.FlipY;
        [ObservableProperty] private float rotation = SpriteProperties.Default.Rotation;
        [ObservableProperty] private Vector2 scale = SpriteProperties.Default.Scale;
        [ObservableProperty] private SDL.FPoint anchorPoint = SpriteProperties.Default.AnchorPoint;
        [ObservableProperty] private float playbackSpeed = SpriteProperties.Default.PlaybackSpeed;
        [ObservableProperty] private bool looping = SpriteProperties.Default.Looping;
        [ObservableProperty] private PlaybackDirection direction = SpriteProperties.Default.Direction;
        [ObservableProperty] private bool bakePlayback = SpriteProperties.Default.BakePlayback;

        [ObservableProperty] private int framesWide = ClipProperties.Default.FramesWide;
        [ObservableProperty] private int framesTall = ClipProperties.Default.FramesTall;
        [ObservableProperty] private int offsetX = ClipProperties.Default.OffsetX;
        [ObservableProperty] private int offsetY = ClipProperties.Default.OffsetY;
        [ObservableProperty] private IReadOnlyDictionary<int, ulong> frameDurations = ClipProperties.Default.FrameDurations;

        public void Play()
            => this.Player?.Play();
        public void Seek(int frameIndex)
            => this.Player?.Seek(frameIndex);
        public void SeekToTime(TimeSpan time)
            => this.Player?.SeekToTime(time);
        public void Stop()
            => this.Player?.Stop();
        public void Reset()
            => this.Player?.Reset();

        #region SpriteBlock: Framework

        public override void ApplyStyles(params IStyleProperties[] properties)
        {
            this.ValidateStyles(properties);

            base.ApplyStyles(properties);

            foreach (IStyleProperties prop in properties)
            {
                switch (prop)
                {
                    case ISpriteProperties props:
                        this.ApplyProperties(props);
                        break;
                    case IClipProperties props:
                        this.ApplyProperties(props);
                        break;
                }
            }
        }

        public override void RequestRender(bool invalidate = true)
        {
            this.IsImageDirty = true;
            this.IsPlayerDirty = true;

            base.RequestRender(invalidate);
        }
        public virtual void RequestRenderWithFlags(bool imageDirty = false, bool playerDirty = false, bool invalidate = true)
        {
            if (imageDirty)
                this.IsImageDirty = true;

            if (playerDirty)
                this.IsPlayerDirty = true;

            base.RequestRender(invalidate);
        }

        public override void FrameworkMeasure(Window window)
        {
            Size frameSize = Size.Empty;

            if (this.Player != null && this.Player.CurrentFrame != null)
                frameSize = new Size()
                {
                    Width = this.Player.CurrentFrame.SourceRect.W,
                    Height = this.Player.CurrentFrame.SourceRect.H
                };

            this.IntrinsicSize = Photon.GetMinimumSize(this, frameSize);
        }

        #endregion

        #region SpriteBlock: Hooks

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (!this.IsInitialized)
                return;

            if (this.Window == null)
                throw new InvalidOperationException("Sprite property change: RootWindow == null.");

            base.OnPropertyChanged(e);

            bool invalidateMeasure = false;
            bool invalidateLayout = false;
            bool invalidateRender = false;

            switch (e.PropertyName)
            {
                case nameof(this.PlaybackSpeed):
                    if (this.Player != null)
                        this.Player.PlaybackSpeed = this.PlaybackSpeed;
                    break;

                case nameof(this.Looping):
                    if (this.Player != null)
                        this.Player.Looping = this.Looping;
                    break;

                case nameof(this.Direction):
                    if (this.Player != null)
                        this.Player.Direction = this.Direction;
                    break;

                case nameof(this.FramesWide):
                case nameof(this.FramesTall):
                case nameof(this.OffsetX):
                case nameof(this.OffsetY):
                case nameof(this.FrameDurations):
                    invalidateMeasure = true;
                    invalidateLayout = true;
                    invalidateRender = true;
                    if (this.TryLoadPlayer(this.Window)) this.Player?.Play();
                    break;

                case nameof(this.FlipX):
                case nameof(this.FlipY):
                case nameof(this.Rotation):
                case nameof(this.Scale):
                case nameof(this.AnchorPoint):
                case nameof(this.ImageTintColor):
                case nameof(this.Opacity):
                case nameof(this.ImageBlendMode):
                    invalidateMeasure = true;
                    invalidateLayout = true;
                    invalidateRender = true;
                    this.ClearFrameCache();
                    this.RequestRenderWithFlags(playerDirty: true);
                    break;
            }

            if (invalidateMeasure)
                this.Parent?.RequestMeasure();

            if (invalidateLayout)
                this.Parent?.RequestArrange();

            if (invalidateRender)
                this.RequestRenderWithFlags();
        }

        public override void OnInitialize(Window window)
        {
            base.OnInitialize(window);

            if (this.TryLoadPlayer(window))
                if (this.Player != null)
                    this.IsPlayerDirty = false;
        }
        public override void OnTick(Window window)
        {
            base.OnTick(window);

            if (this.IsImageDirty)
            {
                this.ClearFrameCache();

                if (this.TryLoadPlayer(window))
                {
                    this.Player?.Play();
                    this.IsPlayerDirty = false;
                }

                this.IsImageDirty = false;
            }

            if (this.Player == null || !this.Player.IsPlaying) return;

            ClipFrame frame = this.Player.CurrentFrame;

            int currentIndex = this.Player.CurrentFrameIndex;

            if (this.FrameCache.TryGetValue(currentIndex, out IntPtr cached))
            {
                this.CachedTexture = cached;
                this.CachedSourceRect = frame.SourceRect;
                this.RequestRender(invalidate: true);
            }
            else
            {
                this.RebuildTexture(window, frame, currentIndex);
                this.RequestRender(invalidate: true);
            }
        }
        public override void OnRender(Window window, SDL.Rect? clipRect = null)
        {
            if (this.IsRenderDirty)
            {
                if (this.IsVisible)
                {
                    Photon.GetControlClipRect(this.DrawRect, this.ClipToBounds, clipRect, out SDL.Rect? effectiveClipRect);

                    if (effectiveClipRect.HasValue)
                    {
                        Photon.ApplyControlClipRect(window, effectiveClipRect);

                        Photon.DrawControlBackground(this);

                        if (this.BakePlayback)
                        {
                            Photon.DrawControlTexture(this, this.CachedTexture, this.CachedDestination, this.CachedSourceRect, effectiveClipRect);
                        }
                        else
                        {
                            SDL.FRect destintation = this.CachedDestination;

                            if (this.Scale != Vector2.One)
                                destintation = Photon.ScaleRect(destintation, this.Scale, this.AnchorPoint);

                            Photon.DrawControlTextureRotated(this, this.CachedTexture, destintation, this.CachedSourceRect, this.Rotation, this.AnchorPoint, this.GetFlipMode(), effectiveClipRect);
                        }

                        Photon.ApplyControlClipRect(window, clipRect);
                    }
                }

                this.IsRenderDirty = false;
                this.RenderedAction?.Invoke(this);
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            foreach (nint tex in this.FrameCache.Values)
                if (tex != IntPtr.Zero)
                    SDL.DestroyTexture(tex);

            this.FrameCache.Clear();

            if (this.CachedTexture != IntPtr.Zero)
            {
                SDL.DestroyTexture(this.CachedTexture);

                this.CachedTexture = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);
        }

        #endregion

        #region SpriteBlock: Helpers

        protected virtual SDL.FlipMode GetFlipMode()
        {
            SDL.FlipMode flipMode = SDL.FlipMode.None;
            if (this.FlipX && this.FlipY) flipMode = SDL.FlipMode.HorizontalAndVertical;
            else if (this.FlipX) flipMode = SDL.FlipMode.Horizontal;
            else if (this.FlipY) flipMode = SDL.FlipMode.Vertical;
            return flipMode;
        }

        protected virtual void ClearFrameCache()
        {
            foreach (IntPtr tex in this.FrameCache.Values)
            {
                if (tex != IntPtr.Zero)
                    SDL.DestroyTexture(tex);
            }
            this.FrameCache.Clear();
        }

        protected virtual bool TryLoadPlayer(Window window)
        {
            this.Player = null;

            if (string.IsNullOrEmpty(this.ImageSourceName) || this.SourceTexture == null || this.SourceTexture == IntPtr.Zero)
                return false;

            try
            {
                this.Player = this.ClipService.GetPlayer(this.SourceTexture.Value, this.FramesWide, this.FramesTall, this.OffsetX, this.OffsetY);

                return this.Player != null;
            }
            catch
            {
                this.Player = null;

                return false;
            }
        }

        protected virtual void RebuildTexture(Window window, ClipFrame frame, int frameIndex)
        {
            //TODO: Commented Out
            /*
            if (this.FrameCache.TryGetValue(frameIndex, out IntPtr oldTex) && oldTex != IntPtr.Zero)
            {
                SDL.DestroyTexture(oldTex);

                this.FrameCache.Remove(frameIndex);
            }

            IntPtr newTex = Photon.CreateTexture(
                window.Renderer,
                (int)frame.SourceRect.W,
                (int)frame.SourceRect.H,
                SDL.PixelFormat.ARGB8888,
                SDL.TextureAccess.Target
            );

            if (newTex == IntPtr.Zero)
                return;

            byte alpha = (byte)Math.Clamp((int)(this.Opacity * 255), 0, 255);
            SDL.SetTextureColorMod(newTex, this.ImageTintColor.R, this.ImageTintColor.G, this.ImageTintColor.B);
            SDL.SetTextureAlphaMod(newTex, alpha);
            SDL.SetTextureBlendMode(newTex, this.ImageBlendMode);

            if (this.BakePlayback)
            {
                SDL.FRect dest = new() { X = 0, Y = 0, W = frame.SourceRect.W, H = frame.SourceRect.H };

                Photon.DrawTextureRotated(
                    window,
                    frame.Texture,
                    frame.SourceRect,
                    dest,
                    this.Rotation,
                    this.AnchorPoint,
                    this.GetFlipMode(),
                    newTex);
            }
            else
            {
                SDL.FRect dest = new() { X = 0, Y = 0, W = frame.SourceRect.W, H = frame.SourceRect.H };

                Photon.DrawTexture(window, frame.Texture, dest, newTex, frame.SourceRect);
            }

            this.FrameCache[frameIndex] = newTex;
            this.CachedTexture = newTex;
            this.CachedSourceRect = new SDL.FRect { X = 0, Y = 0, W = frame.SourceRect.W, H = frame.SourceRect.H };

            float availableW = this.Width > 0 ? this.Width : this.Parent?.ContentRect.W ?? frame.SourceRect.W;
            float availableH = this.Height > 0 ? this.Height : this.Parent?.ContentRect.H ?? frame.SourceRect.H;

            this.SourceControlSize = Photon.GetScaledSize(
                new Size(availableW, availableH),
                new Size(this.ContentRect.W, this.ContentRect.H),
                this.FromControl<StretchProperties>()
            );

            SDL.FRect destRect = new()
            {
                X = this.RenderRect.X,
                Y = this.RenderRect.Y,
                W = this.SourceControlSize.Width,
                H = this.SourceControlSize.Height
            };

            if (this.BakePlayback && this.Scale != Vector2.One)
                destRect = Photon.ScaleRect(destRect, this.Scale, this.AnchorPoint);

            this.CachedDestination = destRect;
            */
        }

        #endregion
    }
}