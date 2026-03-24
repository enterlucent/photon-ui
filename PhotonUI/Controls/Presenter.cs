using CommunityToolkit.Mvvm.ComponentModel;
using PhotonUI.Diagnostics;
using PhotonUI.Diagnostics.Events;
using PhotonUI.Diagnostics.Events.Framework;
using PhotonUI.Diagnostics.Events.Platform;
using PhotonUI.Events.Framework;
using PhotonUI.Extensions;
using PhotonUI.Interfaces.Services;
using PhotonUI.Models;
using SDL3;

namespace PhotonUI.Controls
{
    public partial class Presenter(IServiceProvider serviceProvider, IBindingService bindingService)
        : Control(serviceProvider, bindingService)
    {
        [ObservableProperty] private Control? child;

        partial void OnChildChanging(Control? value)
        {
            if (child != null)
                child.Parent = null;
        }
        partial void OnChildChanged(Control? oldValue, Control? newValue)
        {
            if (oldValue != null)
            {
                oldValue.Parent = null;

                if (this.IsInitialized)
                    FrameworkEventBubble(Photon.GetWindow(this), new ChildChangedEventArgs(this, oldValue, ChildChangeAction.Removed));
            }

            if (newValue != null)
            {
                if (newValue.Parent != null)
                    throw new InvalidOperationException("Control already has a parent.");

                newValue.Parent = this;

                if (this.IsInitialized)
                {
                    Window window = Photon.GetWindow(newValue);

                    FrameworkEventBubble(window, new ChildChangedEventArgs(this, newValue, ChildChangeAction.Added));

                    newValue.OnInitialize(window);
                }
            }
        }

        #region Presenter: Framework

        public override bool TunnelControls(Func<Control, bool> traveler, TunnelDirection direction = TunnelDirection.TopDown)
        {
            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [traveler, direction], DiagnosticPhase.Start));

            try
            {
                if (direction == TunnelDirection.TopDown)
                {
                    if (!traveler(this)) return false;
                    if (this.Child != null && !this.Child.TunnelControls(traveler, direction)) return false;
                }
                else
                {
                    if (this.Child != null && !this.Child.TunnelControls(traveler, direction)) return false;
                    if (!traveler(this)) return false;
                }

                return true;
            }
            finally
            {
                PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [traveler, direction], DiagnosticPhase.End));
            }
        }

        public override void FrameworkIntrinsic(Window window, Size content)
        {
            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [window, content], DiagnosticPhase.Start));

            Size? childSize = null;

            if (this.Child != null)
            {
                // Adjust content size for child's margins and padding
                Size childContent = content.Deflate(this.PaddingExtent).Deflate(this.Child.MarginExtent);

                // Get the child's intrinsic size
                this.Child.OnIntrinsic(window, childContent);

                childSize = this.Child.IntrinsicSize;
            }

            // Calculate the intrinsic size, but only if not a Window (for base controls)
            if (this.GetType() != typeof(Window))
                this.IntrinsicSize = Photon.GetMinimumSize(this, childSize);

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [window, content], DiagnosticPhase.End));
        }
        public override void FrameworkMeasure(Window window)
        {
            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [window], DiagnosticPhase.Start));

            if (this.Child != null)
            {
                Size stretchedSize = Photon.GetStretchedSize(this, this.Child);

                this.Child.DrawRect.W = stretchedSize.Width;
                this.Child.DrawRect.H = stretchedSize.Height;

                this.Child.OnMeasure(window);
            }

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [window], DiagnosticPhase.End));
        }
        public override void FrameworkArrange(Window window, SDL.FPoint anchor)
        {
            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [window, anchor], DiagnosticPhase.Start));

            base.FrameworkArrange(window, anchor);

            if (this.Child != null)
            {
                SDL.FPoint childAnchor = Photon.GetRelativePosition(this, this.Child);

                // Arrange child based on computed anchor point
                this.Child.OnArrange(window, childAnchor);
            }

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [window, anchor], DiagnosticPhase.End));
        }
        public override void FrameworkRender(Window window, SDL.Rect? clipRect)
        {
            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [window, clipRect], DiagnosticPhase.Start));

            // Skip rendering if control is not visible
            if (this.IsVisible)
            {
                // Get the effective clipping rectangle by intersecting with parent's clip rect
                Photon.GetControlClipRect(this.DrawRect, this.ClipToBounds, clipRect, out SDL.Rect? effectiveClipRect);

                // Only render if the control is within the clip bounds or unclipped
                if (effectiveClipRect.HasValue)
                {
                    // Apply the effective clipping region
                    Photon.ApplyControlClipRect(window, effectiveClipRect);

                    // Draw the control's background
                    Photon.DrawControlBackground(this);

                    if (this.Child != null)
                    {
                        // Calculate the child's clipping region within the parent bounds
                        SDL.Rect? childClipRect = effectiveClipRect.Deflate(this.PaddingExtent);

                        // Render the child with its clip region
                        this.Child.OnRender(window, childClipRect);
                    }

                    // Restore the original clip region
                    Photon.ApplyControlClipRect(window, clipRect);

                    PhotonDiagnostics.Emit(new RenderControlEventArgs(this));
                }
            }

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [window, clipRect], DiagnosticPhase.End));
        }

        #endregion

        #region Presenter: Hooks

        public override void OnInitialize(Window window)
        {
            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [window], DiagnosticPhase.Start));

            if (!this.IsInitialized)
            {
                this.FrameworkInitialize(window);
                this.IsInitialized = true;
            }

            this.Child?.OnInitialize(window);

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [window], DiagnosticPhase.End));
        }

        #endregion
    }
}