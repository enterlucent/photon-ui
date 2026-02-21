using CommunityToolkit.Mvvm.ComponentModel;
using PhotonUI.Behaviors;
using PhotonUI.Events;
using PhotonUI.Events.Platform;
using PhotonUI.Extensions;
using PhotonUI.Interfaces;
using PhotonUI.Interfaces.Services;
using PhotonUI.Models;
using PhotonUI.Models.Properties;
using PhotonUI.Services;
using SDL3;
using System.ComponentModel;

namespace PhotonUI.Controls.Content
{
    public record TextControlLineData(int StartIndex, int EndIndex, float PixelWidth, float PixelHeight, int OwnerLineIndex = -1);
    public record TextControlViewportData(int StartLine, int VisibleLines, float HorizontalOffset, float ViewWidth);

    public partial class TextBlock : Control, ITextProperties, IScrollProperties, IScrollHandler
    {
        protected readonly IFontService FontService;
        protected readonly ScrollbarBehavior<TextBlock> ScrollbarBehavior;

        protected IntPtr Font { get; set; } = IntPtr.Zero;
        protected FontMetrics FontMetrics { get; set; } = default;

        protected bool IsTextDirty = true;
        protected List<IntPtr> LineTextureCache = [];

        public List<TextControlLineData> LineDataCache { get; protected set; } = [];

        #region TextBlock: Style Properties

        [ObservableProperty] private string text = string.Empty;

        [ObservableProperty] private string fontFamily = TextProperties.Default.FontFamily;
        [ObservableProperty] private string fontStyle = TextProperties.Default.FontStyle;
        [ObservableProperty] private int fontSize = TextProperties.Default.FontSize;
        [ObservableProperty] private FontRenderMode fontRenderMode = TextProperties.Default.FontRenderMode;
        [ObservableProperty] private TTF.FontStyleFlags fontStyleFlags = TextProperties.Default.FontStyleFlags;
        [ObservableProperty] private TTF.HintingFlags fontHintingFlags = TextProperties.Default.FontHintingFlags;
        [ObservableProperty] private bool fontKerning = TextProperties.Default.FontKerning;

        [ObservableProperty] private SDL.Color textForegroundColor = TextProperties.Default.TextForegroundColor;
        [ObservableProperty] private SDL.Color textBackgroundColor = TextProperties.Default.TextBackgroundColor;
        [ObservableProperty] private SDL.Color textOutlineColor = TextProperties.Default.TextOutlineColor;
        [ObservableProperty] private int textOutlineSize = TextProperties.Default.TextOutlineSize;
        [ObservableProperty] private int textWrapLength = TextProperties.Default.TextWrapLength;
        [ObservableProperty] private TextControlWrapType textWrapType = TextProperties.Default.TextWrapType;

        [ObservableProperty] private ScrollViewportAnchor scrollViewportAnchor = ScrollProperties.Default.ScrollViewportAnchor;
        [ObservableProperty] private ScrollDirection scrollDirection = ScrollProperties.Default.ScrollDirection;
        [ObservableProperty] private float scrollX = ScrollProperties.Default.ScrollX;
        [ObservableProperty] private float scrollY = ScrollProperties.Default.ScrollY;
        [ObservableProperty] private float scrollStepX = ScrollProperties.Default.ScrollStepX;
        [ObservableProperty] private float scrollStepY = ScrollProperties.Default.ScrollStepY;
        [ObservableProperty] private float keyboardScrollStepX = ScrollProperties.Default.KeyboardScrollStepX;
        [ObservableProperty] private float keyboardScrollStepY = ScrollProperties.Default.KeyboardScrollStepY;
        [ObservableProperty] private float wheelScrollStepX = ScrollProperties.Default.WheelScrollStepX;
        [ObservableProperty] private float wheelScrollStepY = ScrollProperties.Default.WheelScrollStepY;
        [ObservableProperty] private float scrollStepMultiplierX = ScrollProperties.Default.ScrollStepMultiplierX;
        [ObservableProperty] private float scrollStepMultiplierY = ScrollProperties.Default.ScrollStepMultiplierY;
        [ObservableProperty] private SDL.Color scrollCaretBackgroundColor = ScrollProperties.Default.ScrollCaretBackgroundColor;
        [ObservableProperty] private SDL.Color scrollTrackBackgroundColor = ScrollProperties.Default.ScrollTrackBackgroundColor;
        [ObservableProperty] private Thickness scrollCaretThickness = ScrollProperties.Default.ScrollCaretThickness;
        [ObservableProperty] private Thickness scrollTrackThickness = ScrollProperties.Default.ScrollTrackThickness;
        [ObservableProperty] private Size minScrollviewSize = ScrollProperties.Default.MinScrollviewSize;

        #endregion

        public TextBlock(IServiceProvider serviceProvider, IBindingService bindingService, IFontService fontService)
            : base(serviceProvider, bindingService)
        {
            this.FontService = fontService;
            this.ScrollbarBehavior = new(this);
        }

        #region TextBlock: Scrollbar Behavior

        protected TextControlViewportData ViewportCache = new(0, 0, 0, 0);
        protected virtual SDL.FRect ScrollbarViewport
        {
            get
            {
                Size scrollbarSize = this.ScrollbarBehavior.GetScrollbarSize(this.DrawRect);

                return this.DrawRect.Deflate(0, 0, scrollbarSize.Width, scrollbarSize.Height).Deflate(this.PaddingExtent);
            }
        }

        public void ScrollToTop() => this.ScrollbarBehavior.ScrollToTop();
        public void ScrollToFraction(float fraction) => this.ScrollbarBehavior.ScrollToFraction(fraction);
        public void ScrollToBottom() => this.ScrollbarBehavior.ScrollToBottom();

        public virtual void OnScrollbarUpdated(ScrollContext context)
        {
            int startIndex = Photon.GetIndexFromPixelHeight(this.LineDataCache, context.VOffset);
            int maxLine = Math.Max(0, this.LineDataCache.Count - this.ViewportCache.VisibleLines);

            this.ViewportCache = this.ViewportCache with
            {
                StartLine = Math.Clamp(startIndex, 0, maxLine),
                HorizontalOffset = context.HOffset
            };

            this.RequestRenderWithFlags(textDirty: true, scrollbarDirty: true);
        }

        #endregion

        #region TextBlock: Framework

        public override void ApplyStyles(params IStyleProperties[] properties)
        {
            this.ValidateStyles(properties);

            base.ApplyStyles(properties);

            foreach (IStyleProperties prop in properties)
            {
                switch (prop)
                {
                    case ITextProperties fontProps:
                        this.ApplyProperties(fontProps);
                        break;
                    case IScrollProperties scrollProperties:
                        this.ApplyProperties(scrollProperties);
                        break;
                }
            }
        }

        public override void RequestRender(bool invalidate = true)
        {
            this.IsRenderDirty = true;
            this.IsTextDirty = true;
            this.ScrollbarBehavior.RequestRender();

            if (invalidate)
                Photon.InvalidateRenderChain(this);
        }
        public virtual void RequestRenderWithFlags(bool textDirty = false, bool scrollbarDirty = false, bool invalidate = true)
        {
            if (textDirty || scrollbarDirty)
                this.IsTextDirty = true;

            if (scrollbarDirty)
                this.ScrollbarBehavior.RequestRender();

            this.IsRenderDirty = true;

            if (invalidate)
                Photon.InvalidateRenderChain(this);
        }

        public override void FrameworkInitialize(Window window)
        {
            base.FrameworkInitialize(window);

            this.RebuildFont();
        }
        public override void FrameworkArrange(Window window, SDL.FPoint anchor)
        {
            base.FrameworkArrange(window, anchor);

            this.BuildLineDataCache(this.Text, this.Font, this.TextWrapType, this.ScrollbarViewport.W);

            Size contentSize = new(
                Photon.GetControlLineMaxPixelWidth(this.LineDataCache),
                Photon.GetControlLinePixelHeight(this.LineDataCache));

            int visibleLines = Math.Max(1, (int)Math.Round(this.ScrollbarViewport.H / this.FontMetrics.SkipLine));
            int maxLine = Math.Max(0, this.LineDataCache.Count - visibleLines);
            int viewportHeight = visibleLines * this.FontMetrics.SkipLine;

            this.ViewportCache = this.ViewportCache with
            {
                StartLine = Math.Clamp(this.ViewportCache.StartLine, 0, maxLine),
                VisibleLines = visibleLines,
                ViewWidth = contentSize.Width,
            };

            this.ScrollbarBehavior.ExtentWidth = Math.Max(0, contentSize.Width - this.ScrollbarViewport.W);
            this.ScrollbarBehavior.ExtentHeight = Math.Max(0, contentSize.Height - viewportHeight);
        }
        public override void FrameworkRender(Window window, SDL.Rect? clipRect = null)
        {
            if (this.IsVisible)
            {
                Photon.GetControlClipRect(this.DrawRect, this.ClipToBounds, clipRect, out SDL.Rect? effectiveClipRect);

                if (effectiveClipRect.HasValue)
                {
                    if (this.IsTextDirty)
                    {
                        this.RebuildTextureCache(window, this.FromControl<TextProperties>());
                        this.IsTextDirty = false;
                    }

                    Photon.ApplyControlClipRect(window, effectiveClipRect);
                    Photon.DrawControlBackground(this);
                    Photon.ApplyControlClipRect(window, this.ScrollbarViewport.ToRect());

                    this.DrawText(window, this.ScrollbarViewport.ToRect());

                    this.ScrollbarBehavior.OnRender(window, this.DrawRect.ToRect());

                    Photon.ApplyControlClipRect(window, clipRect);
                }
            }
        }

        #endregion

        #region TextBlock: Hooks

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (!this.IsInitialized)
                return;

            if (this.Window == null)
                throw new InvalidOperationException($"Control '{this.Name}' is not associated to a RootWindow.");

            base.OnPropertyChanged(e);

            bool invalidateMeasure = false;
            bool invalidateLayout = false;
            bool invalidateRender = false;
            bool invalidateText = false;
            bool invalidateScroll = false;
            bool invalidateFont = false;

            switch (e.PropertyName)
            {
                case nameof(this.FontFamily):
                case nameof(this.FontStyle):
                case nameof(this.FontSize):
                case nameof(this.FontRenderMode):
                case nameof(this.FontStyleFlags):
                case nameof(this.FontHintingFlags):
                case nameof(this.FontKerning):
                    invalidateMeasure = true;
                    invalidateLayout = true;
                    invalidateRender = true;
                    invalidateText = true;
                    invalidateFont = true;
                    break;

                case nameof(this.Text):
                case nameof(this.TextWrapLength):
                case nameof(this.TextWrapType):
                case nameof(this.TextOutlineSize):
                    invalidateMeasure = true;
                    invalidateLayout = true;
                    invalidateRender = true;
                    invalidateText = true;
                    break;

                case nameof(this.TextForegroundColor):
                case nameof(this.TextBackgroundColor):
                case nameof(this.TextOutlineColor):
                    invalidateRender = true;
                    invalidateText = true;
                    break;

                case nameof(this.ScrollDirection):
                case nameof(this.ScrollX):
                case nameof(this.ScrollY):
                    invalidateLayout = true;
                    invalidateRender = true;
                    invalidateScroll = true;
                    break;

                case nameof(this.ScrollCaretBackgroundColor):
                case nameof(this.ScrollTrackBackgroundColor):
                case nameof(this.ScrollTrackThickness):
                case nameof(this.ScrollCaretThickness):
                    invalidateRender = true;
                    invalidateScroll = true;
                    break;
            }

            if (invalidateMeasure)
                this.Parent?.RequestMeasure();

            if (invalidateLayout)
                this.Parent?.RequestArrange();

            if (invalidateRender)
                this.RequestRenderWithFlags(invalidateText, invalidateScroll, true);

            if (invalidateFont)
                this.RebuildFont();
        }

        public override void OnEvent(Window window, FrameworkEventArgs e)
        {
            if (e.Handled == true || e.Preview == true) return;

            if (e is PlatformEventArgs platformEvent)
                this.ScrollbarBehavior?.OnEvent(window, platformEvent.NativeEvent);

            base.OnEvent(window, e);

            switch (e)
            {
                case MouseEnterEventArgs mouseEntered:
                    mouseEntered.Handled = true;
                    break;

                case MouseExitEventArgs mouseExited:
                    mouseExited.Handled = true;
                    break;
            }
        }

        public override void Dispose()
        {
            if (this.LineTextureCache != null)
            {
                foreach (IntPtr tex in this.LineTextureCache)
                    if (tex != IntPtr.Zero)
                        SDL.DestroyTexture(tex);

                this.LineTextureCache.Clear();
                this.LineTextureCache = [];
            }

            GC.SuppressFinalize(this);

            base.Dispose();
        }

        #endregion

        #region TextBlock: Helpers

        protected virtual void RebuildFont()
        {
            this.Font = this.FontService.GetFont(this.FontFamily, this.FontStyle, this.FontSize);
            this.FontMetrics = new FontMetrics(this.Font, this.FromControl<TextProperties>());
            this.ScrollStepY = this.FontMetrics.SkipLine;
        }
        public virtual void BuildLineDataCache(float available)
            => this.BuildLineDataCache(this.Text, this.Font, this.TextWrapType, available);

        protected IntPtr CreateLineTexture(Window window, TextControlLineData lineInfo, TextProperties props)
        {
            string lineText = this.Text[lineInfo.StartIndex..lineInfo.EndIndex];
            if (string.IsNullOrEmpty(lineText))
                return IntPtr.Zero;

            // Outline pass
            TextProperties oProps = props with
            {
                TextForegroundColor = this.TextOutlineColor,
                TextOutlineSize = this.TextOutlineSize
            };

            // Foreground pass  
            TextProperties fgProps = props with
            {
                TextForegroundColor = this.TextForegroundColor,
                TextOutlineSize = 0
            };

            IntPtr oTexture = Photon.CreateFontTexture(window, this.Font, oProps, lineText);
            IntPtr fgTexture = Photon.CreateFontTexture(window, this.Font, fgProps, lineText);
            IntPtr target = Photon.CompositeTextures(window, oTexture, fgTexture, this.TextOutlineSize, this.TextOutlineSize);

            SDL.DestroyTexture(oTexture);
            SDL.DestroyTexture(fgTexture);

            // Crop to viewport
            float remainingWidth = Math.Max(0, lineInfo.PixelWidth - this.ViewportCache.HorizontalOffset);
            SDL.FRect src = new()
            {
                X = this.ViewportCache.HorizontalOffset,
                Y = 0,
                W = remainingWidth,
                H = lineInfo.PixelHeight
            };

            IntPtr cropped = Photon.CropTexture(window, target, src);
            if (cropped != IntPtr.Zero)
            {
                SDL.DestroyTexture(target);
                return cropped;
            }

            return target;
        }


        protected virtual void BuildLineDataCache(string text, IntPtr font, TextControlWrapType wrapType, float availableWidth)
        {
            float effectiveWrap;

            if (wrapType == TextControlWrapType.AutoSoft || wrapType == TextControlWrapType.AutoHard)
            {
                effectiveWrap = availableWidth;
            }
            else if (wrapType == TextControlWrapType.Soft || wrapType == TextControlWrapType.Hard)
            {
                effectiveWrap = this.TextWrapLength > 0 ? this.TextWrapLength : availableWidth;
            }
            else
            {
                effectiveWrap = float.PositiveInfinity;
            }

            List<TextControlLineData> lines = Photon.GetTextControlData(font, text, wrapType, (int)effectiveWrap, this.FontMetrics);

            if (lines == null || lines.Count == 0)
                lines = [new TextControlLineData(0, 0, 0, this.FontMetrics.SkipLine)];

            this.LineDataCache = lines;
        }

        protected virtual void NormalizeTextureCache(int lineCount)
        {
            if (lineCount < this.LineTextureCache.Count)
            {
                for (int i = lineCount; i < this.LineTextureCache.Count; i++)
                    if (this.LineTextureCache[i] != IntPtr.Zero)
                        SDL.DestroyTexture(this.LineTextureCache[i]);

                this.LineTextureCache.RemoveRange(lineCount, this.LineTextureCache.Count - lineCount);
            }
            else if (lineCount > this.LineTextureCache.Count)
            {
                for (int i = this.LineTextureCache.Count; i < lineCount; i++)
                    this.LineTextureCache.Add(IntPtr.Zero);
            }

            if (string.IsNullOrEmpty(this.Text) || this.Font == IntPtr.Zero)
            {
                for (int i = 0; i < this.LineTextureCache.Count; i++)
                {
                    if (this.LineTextureCache[i] != IntPtr.Zero)
                    {
                        SDL.DestroyTexture(this.LineTextureCache[i]);

                        this.LineTextureCache[i] = IntPtr.Zero;
                    }
                }
                return;
            }
        }
        protected virtual void RebuildTextureCache(Window window, TextProperties props)
        {
            this.NormalizeTextureCache(this.ViewportCache.VisibleLines);

            if (string.IsNullOrEmpty(this.Text)) return;

            for (int slot = 0; slot < this.ViewportCache.VisibleLines; slot++)
            {
                int lineIndex = this.ViewportCache.StartLine + slot;

                if (lineIndex >= this.LineDataCache.Count)
                    break;

                TextControlLineData lineInfo = this.LineDataCache[lineIndex];

                if (this.LineTextureCache[slot] != IntPtr.Zero)
                {
                    SDL.DestroyTexture(this.LineTextureCache[slot]);
                    this.LineTextureCache[slot] = IntPtr.Zero;
                }

                this.LineTextureCache[slot] = this.CreateLineTexture(window, lineInfo, props);
            }
        }

        protected virtual void DrawText(Window window, SDL.Rect? clipRect)
        {
            if (!clipRect.HasValue)
                return;

            for (int slot = 0; slot < this.ViewportCache.VisibleLines; slot++)
            {
                int lineIndex = this.ViewportCache.StartLine + slot;

                if (lineIndex >= this.LineDataCache.Count)
                    break;

                TextControlLineData lineInfo = this.LineDataCache[lineIndex];

                float remainingWidth = lineInfo.PixelWidth - this.ViewportCache.HorizontalOffset;
                if (remainingWidth <= 0)
                    continue;

                IntPtr texture = this.LineTextureCache[slot];

                if (texture == IntPtr.Zero)
                    continue;

                SDL.GetTextureSize(texture, out float texW, out float texH);

                float drawWidth = Math.Min(texW, remainingWidth);

                if (drawWidth <= 0)
                    continue;

                SDL.FRect destination = new()
                {
                    X = clipRect.Value.X,
                    Y = clipRect.Value.Y + (slot * lineInfo.PixelHeight),
                    W = drawWidth,
                    H = texH
                };

                Photon.DrawControlTexture(this, texture, destination, clipRect);
            }
        }

        #endregion
    }
}