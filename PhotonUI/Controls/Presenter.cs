using CommunityToolkit.Mvvm.ComponentModel;
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

                if (IsInitialized && Window != null)
                {
                    FrameworkEventBubble(Window,
                        new ChildChangedEventArgs(this, oldValue, ChildChangeAction.Removed));
                }
            }

            if (newValue != null)
            {
                if (newValue.Parent != null)
                    throw new InvalidOperationException("Control already has a parent.");

                newValue.Parent = this;

                if (IsInitialized)
                {
                    if (Window == null)
                        throw new InvalidOperationException($"Control '{newValue.Name}' is not associated with a RootWindow.");

                    FrameworkEventBubble(Window, new ChildChangedEventArgs(this, newValue, ChildChangeAction.Added));

                    newValue.FrameworkInitialize(Window);
                }
            }

            RequestArrange();
        }

        #region Presenter: Framework

        public override bool TunnelControls(Func<Control, bool> traveler, TunnelDirection direction = TunnelDirection.TopDown)
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

        public override void FrameworkIntrinsic(Window window, Size content)
        {
            Size? childSize = null;

            if (this.Child != null)
            {
                // Adjust content size for child's margins and padding
                Size childContent = content.Deflate(this.Child.MarginExtent).Deflate(this.Child.PaddingExtent);

                // Get the child's intrinsic size
                this.Child.OnIntrinsic(window, childContent);

                childSize = this.Child.IntrinsicSize;
            }

            // Calculate the intrinsic size, but only if not a Window (for base controls)
            if (this.GetType() != typeof(Window))
                this.IntrinsicSize = Photon.GetMinimumSize(this, childSize);
        }
        public override void FrameworkMeasure(Window window)
        {
            if (this.Child != null)
            {
                // Get available space for child after padding
                SDL.FRect containerRect = this.DrawRect.Deflate(this.PaddingExtent);

                // Stretch within remaining space after total offsets (X + margins)
                float stretchedW = Photon.GetStretchedWidth(
                    this.Child.HorizontalAlignment,
                    this.Child.IntrinsicSize.Width,
                    containerRect.W,
                    this.Child.X + this.Child.MarginExtent.Horizontal);

                float stretchedH = Photon.GetStretchedHeight(
                    this.Child.VerticalAlignment,
                    this.Child.IntrinsicSize.Height,
                    containerRect.H,
                    this.Child.Y + this.Child.MarginExtent.Vertical);

                // Set child's content size within min/max bounds
                this.Child.DrawRect.W = Math.Clamp(stretchedW, this.Child.MinWidth, this.Child.MaxWidth);
                this.Child.DrawRect.H = Math.Clamp(stretchedH, this.Child.MinHeight, this.Child.MaxHeight);
            }
        }
        public override void FrameworkArrange(Window window, SDL.FPoint anchor)
        {
            // Set position based on anchor and margins
            this.DrawRect.X = anchor.X + this.MarginExtent.Left + this.X;
            this.DrawRect.Y = anchor.Y + this.MarginExtent.Top + this.Y;

            if (this.Child != null)
            {
                // Adjust child's position by padding
                float childX = this.DrawRect.X + this.PaddingExtent.Left;
                float childY = this.DrawRect.Y + this.PaddingExtent.Top;

                // Calculate horizontal alignment position for child
                float horizontalPosition =
                    Photon.GetHorizontalAlignment(
                        this.Child.HorizontalAlignment,
                        childX,
                        this.Child.DrawRect.W,
                        this.DrawRect.W);

                // Calculate vertical alignment position for child
                float verticalPosition =
                    Photon.GetVerticalAlignment(
                        this.Child.VerticalAlignment,
                        childY,
                        this.Child.DrawRect.H,
                        this.DrawRect.H);

                // Define final anchor point for child
                SDL.FPoint childAnchor = new() { X = horizontalPosition, Y = verticalPosition };

                // Arrange child based on computed anchor point
                this.Child.OnArrange(window, childAnchor);
            }
        }
        public override void FrameworkRender(Window window, SDL.Rect? clipRect)
        {
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
                }
            }
        }

        #endregion
    }
}