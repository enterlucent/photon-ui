using PhotonUI.Events.Framework;
using PhotonUI.Extensions;
using PhotonUI.Interfaces.Services;
using PhotonUI.Models;
using SDL3;
using System.Collections.ObjectModel;

namespace PhotonUI.Controls
{
    public partial class Canvas(IServiceProvider serviceProvider, IBindingService bindingService, IKeyBindingService keyBindingService)
        : Control(serviceProvider, bindingService, keyBindingService)
    {
        protected ObservableCollection<Control> Children = [];

        public Action<Control>? ChildAddedAction { get; set; }
        public Action<Control>? ChildRemovedAction { get; set; }

        public virtual void AddChild(Control control)
        {
            ArgumentNullException.ThrowIfNull(control, nameof(control));

            if (this.Children.Contains(control))
                throw new InvalidOperationException("Control already a child of the parent.");

            if (control.Parent != null)
                throw new InvalidOperationException("Control already has a parent.");

            control.Parent = this;

            this.Children.Add(control);

            if (this.IsInitialized)
            {
                if (this.Window == null)
                    throw new InvalidOperationException($"Control '{this.Name}' is not associated with a RootWindow.");

                control.FrameworkInitialize(this.Window);

                ChildChangedEventArgs args = new(this, control, ChildChangeAction.Added);

                this.FrameworkEventBubble(this.Window, args);
            }

            this.ChildAddedAction?.Invoke(control);
        }
        public virtual void RemoveChild(Control control)
        {
            if (!this.Children.Contains(control))
                throw new InvalidOperationException("Control is not a child of the parent.");

            control.Parent = null;

            this.Children.Remove(control);

            if (this.IsInitialized)
            {
                if (this.Window == null)
                    throw new InvalidOperationException($"Control '{this.Name}' is not associated with a RootWindow.");

                ChildChangedEventArgs args = new(this, control, ChildChangeAction.Removed);

                this.FrameworkEventBubble(this.Window, args);
            }

            this.ChildRemovedAction?.Invoke(control);
        }

        #region Canvas: Framework

        public override bool TunnelControls(Func<Control, bool> traveler, TunnelDirection direction = TunnelDirection.TopDown)
        {
            if (direction == TunnelDirection.TopDown)
            {
                if (!traveler(this)) return false;

                if (this.Children != null)
                    foreach (Control child in this.Children)
                        if (!child.TunnelControls(traveler, direction)) return false;
            }
            else
            {
                if (this.Children != null)
                    foreach (Control child in this.Children)
                        if (!child.TunnelControls(traveler, direction)) return false;

                if (!traveler(this)) return false;
            }

            return true;
        }

        public override void FrameworkIntrinsic(Window window, Size content)
        {
            // Initialize extreme values for bounding box calculations
            float minX = float.PositiveInfinity;
            float maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity;
            float maxY = float.NegativeInfinity;

            // Iterate through each child control
            foreach (Control child in this.Children)
            {
                // Pass full content to child (margins treated as positioning offsets)
                Size childContent = content.Deflate(child.PaddingExtent);

                // Get the child's intrinsic size
                child.OnIntrinsic(window, childContent);

                // Envelope includes child's full positioned extent (X/Y + margins + content)
                float childLeft = child.X + child.MarginExtent.Left;
                float childRight = child.X + child.MarginExtent.Horizontal + child.IntrinsicSize.Width;
                float childTop = child.Y + child.MarginExtent.Top;
                float childBottom = child.Y + child.MarginExtent.Vertical + child.IntrinsicSize.Height;

                // Update bounding box with child's full extent
                minX = Math.Min(minX, childLeft);
                maxX = Math.Max(maxX, childRight);
                minY = Math.Min(minY, childTop);
                maxY = Math.Max(maxY, childBottom);
            }

            // Calculate the overall control size based on children's positioned bounding box
            Size? offsetEnvelope = this.Children.Count > 0 ? new(maxX - minX, maxY - minY) : null;

            // Set the intrinsic size of the control based on the children's layout
            this.IntrinsicSize = Photon.GetMinimumSize(this, offsetEnvelope);
        }
        public override void FrameworkMeasure(Window window)
        {
            foreach (Control child in this.Children)
            {
                // Calculate available space for the child after padding
                SDL.FRect containerRect = this.DrawRect.Deflate(this.PaddingExtent);

                // Stretch within remaining space after total offsets (X/Y + margins)
                float stretchedW = Photon.GetStretchedWidth(
                    child.HorizontalAlignment,
                    child.IntrinsicSize.Width,
                    containerRect.W,
                    child.X + child.MarginExtent.Horizontal);

                float stretchedH = Photon.GetStretchedHeight(
                    child.VerticalAlignment,
                    child.IntrinsicSize.Height,
                    containerRect.H,
                    child.Y + child.MarginExtent.Vertical);

                // Set child's content size within min/max bounds (DrawRect excludes margins)
                child.DrawRect.W = Math.Clamp(stretchedW, child.MinWidth, child.MaxWidth);
                child.DrawRect.H = Math.Clamp(stretchedH, child.MinHeight, child.MaxHeight);
            }
        }
        public override void FrameworkArrange(Window window, SDL.FPoint anchor)
        {
            // Set control's position, including margins and offsets
            this.DrawRect.X = anchor.X + this.MarginExtent.Left + this.X;
            this.DrawRect.Y = anchor.Y + this.MarginExtent.Top + this.Y;

            foreach (Control child in this.Children)
            {
                // Child's base position: parent content 
                float childX = this.DrawRect.X + this.PaddingExtent.Left;
                float childY = this.DrawRect.Y + this.PaddingExtent.Top;

                // Calculate child's alignment position within parent content area
                float horizontalPosition = Photon.GetHorizontalAlignment(
                    child.HorizontalAlignment,
                    childX,
                    child.DrawRect.W,
                    this.DrawRect.W);

                float verticalPosition = Photon.GetVerticalAlignment(
                    child.VerticalAlignment,
                    childY,
                    child.DrawRect.H,
                    this.DrawRect.H);

                // Define final anchor point for child
                SDL.FPoint childAnchor = new() { X = horizontalPosition, Y = verticalPosition };

                // Arrange child based on computed anchor point
                child.OnArrange(window, childAnchor);
            }
        }
        public override void FrameworkRender(Window window, SDL.Rect? clipRect)
        {
            // Skip rendering if the control is not visible
            if (this.IsVisible)
            {
                // Calculate the effective clip area after considering control's bounds and provided clip rect
                Photon.GetControlClipRect(this.DrawRect, this.ClipToBounds, clipRect, out SDL.Rect? effectiveClipRect);

                // Only render if the control is within the clip region or is unclipped
                if (effectiveClipRect.HasValue)
                {
                    // Apply the updated clipping region
                    Photon.ApplyControlClipRect(window, effectiveClipRect);

                    // Draw the control's background
                    Photon.DrawControlBackground(this);

                    // Render each child in order of their ZIndex
                    foreach (Control child in this.Children.OrderBy(c => c.ZIndex))
                    {
                        // Clip for child: only parent's padding (margins handled as positioning offsets)
                        SDL.Rect? childClipRect = effectiveClipRect.Deflate(this.PaddingExtent);

                        // Request the child to render with its own clip region
                        child.OnRender(window, childClipRect);
                    }

                    // Restore the original clip region
                    Photon.ApplyControlClipRect(window, clipRect);
                }
            }
        }

        #endregion
    }
}