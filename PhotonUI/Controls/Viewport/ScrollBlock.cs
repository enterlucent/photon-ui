using CommunityToolkit.Mvvm.ComponentModel;
using PhotonUI.Behaviors;
using PhotonUI.Events;
using PhotonUI.Events.Platform;
using PhotonUI.Extensions;
using PhotonUI.Interfaces;
using PhotonUI.Interfaces.Services;
using PhotonUI.Models;
using PhotonUI.Models.Properties;
using SDL3;
using System.ComponentModel;

namespace PhotonUI.Controls.Viewport
{
    public partial class ScrollBlock : Presenter, IScrollProperties, IScrollHandler
    {
        protected ScrollbarBehavior<ScrollBlock> ScrollbarBehavior;

        #region ScrollBox: Style Properties

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

        public ScrollBlock(IServiceProvider serviceProvider, IBindingService bindingService)
            : base(serviceProvider, bindingService)
        {
            this.ScrollbarBehavior = new(this);
        }

        #region ScrollBox: Scrollbar Behavior

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

        public void OnScrollbarUpdated(ScrollContext context)
        {
            this.ScrollX = Math.Clamp(context.HOffset, 0f, context.HFullSize);
            this.ScrollY = Math.Clamp(context.VOffset, 0f, context.VFullSize);

            this.RequestRender();
        }

        #endregion

        #region ScrollBox: Framework

        public override void ApplyStyles(params IStyleProperties[] properties)
        {
            this.ValidateStyles(properties);

            base.ApplyStyles(properties);

            foreach (IStyleProperties prop in properties)
            {
                switch (prop)
                {
                    case IScrollProperties props:
                        this.ApplyProperties(props);
                        break;
                }
            }
        }

        public override void FrameworkIntrinsic(Window window, Size content)
        {
            if (this.Child != null)
            {
                // Adjust content size for child's margins and padding
                Size childContent = content.Deflate(this.Child.MarginExtent).Deflate(this.Child.PaddingExtent);

                // Get the child's intrinsic size
                this.Child.OnIntrinsic(window, childContent);
            }

            // Calculate the intrinsic size
            this.IntrinsicSize = Photon.GetMinimumSize(this);
        }
        public override void FrameworkArrange(Window window, SDL.FPoint anchor)
        {
            base.FrameworkArrange(window, anchor);

            if (this.Child != null)
            {
                this.ScrollX = Photon.ApplyHorizontalScroll(this.Child, this.ScrollX, this.ScrollbarViewport);
                this.ScrollY = Photon.ApplyVerticalScroll(this.Child, this.ScrollY, this.ScrollbarViewport);

                this.ScrollbarBehavior.ExtentWidth = this.Child.DrawRect.W - this.ScrollbarViewport.W;
                this.ScrollbarBehavior.HorizontalOffset = this.ScrollX;
                this.ScrollbarBehavior.ExtentHeight = this.Child.DrawRect.H - this.ScrollbarViewport.H;
                this.ScrollbarBehavior.VerticalOffset = this.ScrollY;
            }
        }
        public override void FrameworkRender(Window window, SDL.Rect? clipRect = null)
        {
            if (this.IsVisible)
            {
                // Compute effective clip rect: intersection of parent clip, control bounds, and ClipToBounds
                Photon.GetControlClipRect(this.DrawRect, this.ClipToBounds, clipRect, out SDL.Rect? effectiveClipRect);

                if (effectiveClipRect.HasValue)
                {
                    // Apply hierarchy clipping (affects all subsequent drawing)
                    Photon.ApplyControlClipRect(window, effectiveClipRect);

                    // Draw ScrollViewer background within clip
                    Photon.DrawControlBackground(this);

                    if (this.Child != null)
                    {
                        // Render child content (already scroll-offset in Arrange, clipped to viewport)
                        this.Child.OnRender(window, this.ScrollbarViewport.ToRect());

                        // Render scrollbars over content area (they handle their own thumb positioning)
                        this.ScrollbarBehavior.OnRender(window, this.DrawRect.ToRect());
                    }

                    // Restore parent clip state (unwind clipping stack)
                    Photon.ApplyControlClipRect(window, clipRect);
                }
            }
        }

        #endregion

        #region ScrollBox: Hooks

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (!this.IsInitialized)
                return;

            if (this.Window == null)
                throw new InvalidOperationException($"Control '{this.Name}' is not associated to a RootWindow.");

            base.OnPropertyChanged(e);

            bool invalidateLayout = false;
            bool invalidateRender = false;

            switch (e.PropertyName)
            {
                case nameof(this.ScrollX):
                case nameof(this.ScrollY):
                    invalidateLayout = true;
                    invalidateRender = true;
                    break;

                case nameof(this.ScrollDirection):
                    invalidateLayout = true;
                    invalidateRender = true;
                    break;
            }

            if (invalidateLayout)
                this.Parent?.RequestArrange();

            if (invalidateRender)
                this.RequestRender();
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

        #endregion
    }
}