using CommunityToolkit.Mvvm.ComponentModel;
using PhotonUI.Behaviors;
using PhotonUI.Diagnostics;
using PhotonUI.Diagnostics.Events;
using PhotonUI.Diagnostics.Events.Framework;
using PhotonUI.Diagnostics.Events.Platform;
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
        protected ScrollBehavior<ScrollBlock> ScrollbarBehavior;

        #region ScrollBlock: Style Properties

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

        #region ScrollBlock: Scrollbar Behavior

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

        #region ScrollBlock: Framework

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

        public override void RequestRender(bool invalidate = true)
        {
            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [invalidate], DiagnosticPhase.Start));

            this.IsRenderDirty = true;
            this.ScrollbarBehavior.RequestRender();

            if (invalidate)
                Photon.InvalidateRenderChain(this);

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [invalidate], DiagnosticPhase.End));
        }
        public virtual void RequestRenderWithFlags(bool scrollbarDirty = false, bool invalidate = true)
        {
            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [invalidate], DiagnosticPhase.Start));

            this.IsRenderDirty = true;

            if (scrollbarDirty == true)
                this.ScrollbarBehavior.RequestRender();

            if (invalidate)
                Photon.InvalidateRenderChain(this);

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [invalidate], DiagnosticPhase.End));
        }

        public override void FrameworkIntrinsic(Window window, Size content)
        {
            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [window, content], DiagnosticPhase.Start));

            if (this.Child != null)
            {
                Size childContent = content.Deflate(this.PaddingExtent).Deflate(this.Child.MarginExtent);

                // Get the child's intrinsic size
                this.Child.OnIntrinsic(window, childContent);
            }

            // Calculate the intrinsic size
            this.IntrinsicSize = Photon.GetMinimumSize(this);

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [window, content], DiagnosticPhase.End));
        }
        public override void FrameworkArrange(Window window, SDL.FPoint anchor)
        {
            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [window, anchor], DiagnosticPhase.Start));

            this.DrawRect.X = anchor.X + this.X;
            this.DrawRect.Y = anchor.Y + this.Y;

            if (this.Child != null)
            {
                this.ScrollX = this.ApplyHorizontalScroll(this.Child, this.ScrollX, this.ScrollbarViewport);
                this.ScrollY = this.ApplyVerticalScroll(this.Child, this.ScrollY, this.ScrollbarViewport);

                this.ScrollbarBehavior.ExtentWidth = this.Child.DrawRect.W - this.ScrollbarViewport.W;
                this.ScrollbarBehavior.HorizontalOffset = this.ScrollX;
                this.ScrollbarBehavior.ExtentHeight = this.Child.DrawRect.H - this.ScrollbarViewport.H;
                this.ScrollbarBehavior.VerticalOffset = this.ScrollY;

                this.Child.OnArrange(window, new SDL.FPoint() { X = this.Child.DrawRect.X, Y = this.Child.DrawRect.Y });
            }

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [window, anchor], DiagnosticPhase.End));
        }
        public override void FrameworkRender(Window window, SDL.Rect? clipRect = null)
        {
            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [window, clipRect], DiagnosticPhase.Start));

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

                    PhotonDiagnostics.Emit(new RenderControlEventArgs(this));
                }
            }

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [window, clipRect], DiagnosticPhase.End));
        }

        #endregion

        #region ScrollBlock: Hooks

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (!this.IsInitialized)
                return;

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [e], DiagnosticPhase.Start));

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

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [e], DiagnosticPhase.End));
        }

        public override void OnEvent(Window window, FrameworkEventArgs e)
        {
            if (e.Handled == true || e.Preview == true) return;

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [window, e], DiagnosticPhase.Start));

            if (e is PlatformEventArgs platformEvent)
                this.ScrollbarBehavior?.OnEvent(window, platformEvent.NativeEvent);

            base.OnEvent(window, e);

            switch (e)
            {
                case PointerEnterEventArgs pointerEntered:
                    pointerEntered.Handled = true;
                    break;

                case PointerExitEventArgs pointerExited:
                    pointerExited.Handled = true;
                    break;
            }

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [window, e], DiagnosticPhase.End));
        }

        #endregion

        #region ScrollBlock: Helpers

        public float ApplyHorizontalScroll(Control child, float scrollX, SDL.FRect viewport)
        {
            SDL.FRect scrolled = child.DrawRect;

            float baseX = this.DrawRect.X;

            if (scrolled.W <= viewport.W)
            {
                scrolled.X = baseX;
                child.DrawRect = scrolled;
                return 0;
            }
            else
            {
                float maxScrollX = scrolled.W - viewport.W;
                scrollX = Math.Clamp(scrollX, 0, maxScrollX);
                scrolled.X = baseX - scrollX;
                child.DrawRect = scrolled;
                return scrollX;
            }
        }
        public float ApplyVerticalScroll(Control child, float scrollY, SDL.FRect viewport)
        {
            SDL.FRect scrolled = child.DrawRect;

            float baseY = this.DrawRect.Y;

            if (scrolled.H <= viewport.H)
            {
                scrolled.Y = baseY;
                child.DrawRect = scrolled;
                return 0;
            }
            else
            {
                float maxScrollY = scrolled.H - viewport.H;
                scrollY = Math.Clamp(scrollY, 0, maxScrollY);
                scrolled.Y = baseY - scrollY;
                child.DrawRect = scrolled;
                return scrollY;
            }
        }

        #endregion
    }
}