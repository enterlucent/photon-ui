using CommunityToolkit.Mvvm.ComponentModel;
using PhotonUI.Extensions;
using PhotonUI.Interfaces;
using PhotonUI.Interfaces.Services;
using PhotonUI.Models.Properties;
using SDL3;
using System.ComponentModel;

namespace PhotonUI.Controls.Layout
{
    public partial class StackBlock(IServiceProvider serviceProvider, IBindingService bindingService)
        : Canvas(serviceProvider, bindingService), IStackProperties
    {
        [ObservableProperty] private Orientation stackOrientation = StackProperties.Default.StackOrientation;
        [ObservableProperty] private StackFillType stackFillType = StackProperties.Default.StackFillType;

        #region StackPanel: Framework

        public override void ApplyStyles(params IStyleProperties[] properties)
        {
            this.ValidateStyles(properties);

            base.ApplyStyles(properties);

            foreach (IStyleProperties prop in properties)
            {
                switch (prop)
                {
                    case IStackProperties props:
                        this.ApplyProperties(props);
                        break;
                }
            }
        }

        public override void FrameworkMeasure(Window window)
        {
            SDL.FRect contentRect = this.DrawRect.Deflate(this.PaddingExtent);

            bool isHorizontal = this.StackOrientation == Orientation.Horizontal;

            foreach (Control child in this.Children)
            {
                if (child == null) continue;

                float stackSize = isHorizontal ? child.IntrinsicSize.Width : child.IntrinsicSize.Height;

                float crossAvailable = isHorizontal ? contentRect.H : contentRect.W;

                float crossOffset = isHorizontal
                    ? child.Y + child.MarginExtent.Vertical
                    : child.X + child.MarginExtent.Horizontal;

                float crossSize = isHorizontal
                    ? Photon.GetStretchedHeight(child.VerticalAlignment, child.IntrinsicSize.Height, crossAvailable, crossOffset)
                    : Photon.GetStretchedWidth(child.HorizontalAlignment, child.IntrinsicSize.Width, crossAvailable, crossOffset);

                if (isHorizontal)
                {
                    child.DrawRect.W = Math.Clamp(stackSize, child.MinWidth, child.MaxWidth);
                    child.DrawRect.H = Math.Clamp(crossSize, child.MinHeight, child.MaxHeight);
                }
                else
                {
                    child.DrawRect.W = Math.Clamp(crossSize, child.MinWidth, child.MaxWidth);
                    child.DrawRect.H = Math.Clamp(stackSize, child.MinHeight, child.MaxHeight);
                }
            }

            int validChildCount = this.Children.Count(c => c != null);

            float totalAvailableSize = isHorizontal
                ? (contentRect.W)
                : (contentRect.H);

            float totalContentSize = 0f;

            foreach (Control child in this.Children)
            {
                if (child == null) continue;

                float stackSize = isHorizontal ? child.DrawRect.W : child.DrawRect.H;
                float stackMargin = isHorizontal
                    ? child.MarginExtent.Horizontal
                    : child.MarginExtent.Vertical;

                totalContentSize += stackSize + stackMargin;
            }

            int index = 0;
            float dummyOffset = 0f;

            foreach (Control child in this.Children)
            {
                if (child == null)
                {
                    index++;

                    continue;
                }

                switch (this.StackFillType)
                {
                    case StackFillType.None:
                        dummyOffset += this.MeasureStackItemNone(child);
                        break;

                    case StackFillType.Equal:
                        dummyOffset += this.MeasureStackItemEqual(child, totalAvailableSize, totalContentSize, validChildCount);
                        break;

                    case StackFillType.First:
                        dummyOffset += this.MeasureStackItemFillIndex(child, totalAvailableSize, totalContentSize, validChildCount, 0);
                        break;

                    case StackFillType.Last:
                        dummyOffset += this.MeasureStackItemFillIndex(child, totalAvailableSize, totalContentSize, validChildCount, validChildCount - 1);
                        break;
                }

                index++;
            }
        }
        public override void FrameworkArrange(Window window, SDL.FPoint anchor)
        {
            this.DrawRect.X = anchor.X + this.MarginExtent.Left + this.X;
            this.DrawRect.Y = anchor.Y + this.MarginExtent.Top + this.Y;

            SDL.FPoint contentStart = new()
            {
                X = this.DrawRect.X + this.PaddingExtent.Left,
                Y = this.DrawRect.Y + this.PaddingExtent.Top
            };

            float offset = 0f;

            foreach (Control child in this.Children)
            {
                if (child == null) continue;

                float horizontalPosition = 0;
                float verticalPosition = 0;

                switch (this.StackFillType)
                {
                    case StackFillType.None:
                        offset = this.ArrangeStackItemNone(child, contentStart, offset, out horizontalPosition, out verticalPosition);
                        break;

                    case StackFillType.Equal:
                        offset = this.ArrangeStackItemEqual(child, contentStart, offset, out horizontalPosition, out verticalPosition);
                        break;

                    case StackFillType.First:
                        offset = this.ArrangeStackItemFillIndex(child, contentStart, offset, out horizontalPosition, out verticalPosition);
                        break;

                    case StackFillType.Last:
                        offset = this.ArrangeStackItemFillIndex(child, contentStart, offset, out horizontalPosition, out verticalPosition);
                        break;
                }

                SDL.FPoint childAnchor = new()
                {
                    X = horizontalPosition,
                    Y = verticalPosition
                };

                child.OnArrange(window, childAnchor);
            }
        }

        #endregion

        #region StackPanel: Hooks

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

            switch (e.PropertyName)
            {
                case nameof(this.StackOrientation):
                case nameof(this.StackFillType):
                    invalidateMeasure = true;
                    invalidateLayout = true;
                    invalidateRender = true;
                    break;
            }

            if (invalidateMeasure)
                this.Parent?.RequestMeasure();

            if (invalidateLayout)
                this.Parent?.RequestArrange();

            if (invalidateRender)
                this.RequestRender();
        }

        #endregion

        #region StackPanel: Helpers

        private float MeasureStackItemNone(Control child)
        {
            bool isHorizontal = this.StackOrientation == Orientation.Horizontal;

            float childStackSize = isHorizontal ? child.DrawRect.W : child.DrawRect.H;
            float childStackMargin = isHorizontal
                ? child.MarginExtent.Horizontal
                : child.MarginExtent.Vertical;

            return childStackSize + childStackMargin;
        }
        private float MeasureStackItemEqual(Control child, float totalAvailableSize, float totalContentSize, int validChildCount)
        {
            bool isHorizontal = this.StackOrientation == Orientation.Horizontal;

            float extraPerChild = validChildCount > 0
                ? (totalAvailableSize - totalContentSize) / validChildCount
                : 0;

            float childNaturalSize = isHorizontal ? child.DrawRect.W : child.DrawRect.H;
            float targetStackSize = childNaturalSize + extraPerChild;

            if (isHorizontal)
            {
                child.DrawRect.W = Math.Clamp(targetStackSize, child.MinWidth, child.MaxWidth);
            }
            else
            {
                child.DrawRect.H = Math.Clamp(targetStackSize, child.MinHeight, child.MaxHeight);
            }

            float childStackSize = isHorizontal ? child.DrawRect.W : child.DrawRect.H;
            float childStackMargin = isHorizontal
                ? child.MarginExtent.Horizontal
                : child.MarginExtent.Vertical;

            return childStackSize + childStackMargin;
        }
        private float MeasureStackItemFillIndex(Control child, float totalAvailableSize, float totalContentSize, int validChildCount, int targetIndex)
        {
            bool isHorizontal = this.StackOrientation == Orientation.Horizontal;

            float extraForTarget = validChildCount > 0
                ? (totalAvailableSize - totalContentSize)
                : 0;

            int childIndex = this.Children.IndexOf(child);

            if (childIndex == targetIndex)
            {
                float childNaturalSize = isHorizontal ? child.DrawRect.W : child.DrawRect.H;
                float targetStackSize = childNaturalSize + extraForTarget;

                if (isHorizontal)
                {
                    child.DrawRect.W = Math.Clamp(targetStackSize, child.MinWidth, child.MaxWidth);
                }
                else
                {
                    child.DrawRect.H = Math.Clamp(targetStackSize, child.MinHeight, child.MaxHeight);
                }
            }

            float childStackSize = isHorizontal ? child.DrawRect.W : child.DrawRect.H;
            float childStackMargin = isHorizontal
                ? child.MarginExtent.Horizontal
                : child.MarginExtent.Vertical;

            return childStackSize + childStackMargin;
        }

        private float ArrangeStackItemNone(Control child, SDL.FPoint contentStart, float offset, out float horizontalPosition, out float verticalPosition)
        {
            bool isHorizontal = this.StackOrientation == Orientation.Horizontal;

            float baseX = isHorizontal ? contentStart.X + offset : contentStart.X;
            float baseY = !isHorizontal ? contentStart.Y + offset : contentStart.Y;

            float containerW = this.DrawRect.W - this.PaddingExtent.Horizontal;
            float containerH = this.DrawRect.H - this.PaddingExtent.Vertical;

            horizontalPosition = Photon.GetHorizontalAlignment(
                child.HorizontalAlignment, baseX, child.DrawRect.W, containerW);

            verticalPosition = Photon.GetVerticalAlignment(
                child.VerticalAlignment, baseY, child.DrawRect.H, containerH);

            float childStackSize = isHorizontal ? child.DrawRect.W : child.DrawRect.H;
            float childStackMargin = isHorizontal
                ? child.MarginExtent.Horizontal
                : child.MarginExtent.Vertical;

            offset += childStackSize + childStackMargin;

            return offset;
        }
        private float ArrangeStackItemEqual(Control child, SDL.FPoint contentStart, float offset, out float horizontalPosition, out float verticalPosition)
        {
            bool isHorizontal = this.StackOrientation == Orientation.Horizontal;

            float baseX = isHorizontal ? contentStart.X + offset : contentStart.X;
            float baseY = !isHorizontal ? contentStart.Y + offset : contentStart.Y;

            float containerW = this.DrawRect.W - this.PaddingExtent.Horizontal;
            float containerH = this.DrawRect.H - this.PaddingExtent.Vertical;

            horizontalPosition = Photon.GetHorizontalAlignment(
                child.HorizontalAlignment, baseX, child.DrawRect.W, containerW);
            verticalPosition = Photon.GetVerticalAlignment(
                child.VerticalAlignment, baseY, child.DrawRect.H, containerH);

            float childStackSize = isHorizontal ? child.DrawRect.W : child.DrawRect.H;
            float childStackMargin = isHorizontal
                ? child.MarginExtent.Horizontal
                : child.MarginExtent.Vertical;

            offset += childStackSize + childStackMargin;

            return offset;
        }
        private float ArrangeStackItemFillIndex(Control child, SDL.FPoint contentStart, float offset, out float horizontalPosition, out float verticalPosition)
        {
            bool isHorizontal = this.StackOrientation == Orientation.Horizontal;

            float baseX = isHorizontal ? contentStart.X + offset : contentStart.X;
            float baseY = !isHorizontal ? contentStart.Y + offset : contentStart.Y;

            float containerW = this.DrawRect.W - this.PaddingExtent.Horizontal;
            float containerH = this.DrawRect.H - this.PaddingExtent.Vertical;

            horizontalPosition = Photon.GetHorizontalAlignment(
                child.HorizontalAlignment, baseX, child.DrawRect.W, containerW);
            verticalPosition = Photon.GetVerticalAlignment(
                child.VerticalAlignment, baseY, child.DrawRect.H, containerH);

            float childStackSize = isHorizontal ? child.DrawRect.W : child.DrawRect.H;
            float childStackMargin = isHorizontal
                ? child.MarginExtent.Horizontal
                : child.MarginExtent.Vertical;

            offset += childStackSize + childStackMargin;

            return offset;
        }

        #endregion
    }
}