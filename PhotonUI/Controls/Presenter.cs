using CommunityToolkit.Mvvm.ComponentModel;
using PhotonUI.Events.Framework;
using PhotonUI.Extensions;
using PhotonUI.Interfaces.Services;
using PhotonUI.Models;
using SDL3;

namespace PhotonUI.Controls
{
    public partial class Presenter(IServiceProvider serviceProvider, IBindingService bindingService, IKeyBindingService keyBindingService)
        : Control(serviceProvider, bindingService, keyBindingService)
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
                Size childContent = content.Deflate(this.PaddingExtent).Deflate(this.Child.MarginExtent);

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
                Size stretchedSize = Photon.GetStretchedSize(this, this.Child);

                this.Child.DrawRect.W = stretchedSize.Width;
                this.Child.DrawRect.H = stretchedSize.Height;

                this.Child.OnMeasure(window);
            }
        }
        public override void FrameworkArrange(Window window, SDL.FPoint anchor)
        {
            base.FrameworkArrange(window, anchor);

            if (this.Child != null)
            {
                SDL.FPoint childAnchor = Photon.GetRelativePosition(this, this.Child);

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