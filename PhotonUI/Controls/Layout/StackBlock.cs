using CommunityToolkit.Mvvm.ComponentModel;
using PhotonUI.Extensions;
using PhotonUI.Interfaces;
using PhotonUI.Interfaces.Services;
using PhotonUI.Models;
using PhotonUI.Models.Properties;
using SDL3;
using System.ComponentModel;

namespace PhotonUI.Controls.Layout
{
    public partial class StackBlock(IServiceProvider serviceProvider, IBindingService bindingService)
        : Canvas(serviceProvider, bindingService), IStackProperties
    {
        [ObservableProperty] private Orientation stackOrientation = StackProperties.Default.StackOrientation;
        [ObservableProperty] private float spacing = StackProperties.Default.Spacing;

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

        public override void FrameworkIntrinsic(Window window, Size content)
        {
            bool isHorizontal = this.StackOrientation == Orientation.Horizontal;

            float totalStackSize = 0f;
            float maxCrossSize = 0f;

            foreach (Control child in this.Children)
            {
                if (child == null) continue;

                Size childContent = content.Deflate(child.PaddingExtent);

                child.OnIntrinsic(window, childContent);

                float stackSize = isHorizontal ? child.IntrinsicSize.Width : child.IntrinsicSize.Height;
                float crossSize = isHorizontal ? child.IntrinsicSize.Height : child.IntrinsicSize.Width;
                float stackMargin = isHorizontal ? child.MarginExtent.Horizontal : child.MarginExtent.Vertical;

                totalStackSize += stackSize + stackMargin + this.Spacing;
                maxCrossSize = Math.Max(maxCrossSize, crossSize);
            }

            totalStackSize -= this.Spacing;

            Size stackContent = isHorizontal
                ? new Size(totalStackSize, maxCrossSize)
                : new Size(maxCrossSize, totalStackSize);

            this.IntrinsicSize = Photon.GetMinimumSize(this, stackContent);
        }
        public override void FrameworkMeasure(Window window)
        {
            bool isHorizontal = this.StackOrientation == Orientation.Horizontal;

            foreach (Control child in this.Children)
            {
                if (child == null) continue;

                bool allowStretchW = !isHorizontal;
                bool allowStretchH = isHorizontal;

                Size stretched = Photon.GetStretchedSize(this, child, allowStretchW, allowStretchH);

                if (isHorizontal)
                {
                    child.DrawRect.W = child.IntrinsicSize.Width;
                    child.DrawRect.H = stretched.Height;
                }
                else
                {
                    child.DrawRect.W = stretched.Width;
                    child.DrawRect.H = child.IntrinsicSize.Height;
                }

                child.OnMeasure(window);
            }
        }
        public override void FrameworkArrange(Window window, SDL.FPoint anchor)
        {
            this.DrawRect.X = anchor.X + this.X;
            this.DrawRect.Y = anchor.Y + this.Y;

            bool isHorizontal = this.StackOrientation == Orientation.Horizontal;
            float offset = 0f;

            SDL.FRect contentRect = this.DrawRect.Deflate(this.PaddingExtent);

            foreach (Control child in this.Children)
            {
                if (child == null) continue;

                float crossX = contentRect.X + child.MarginExtent.Left;
                float crossY = contentRect.Y + child.MarginExtent.Top;

                if (isHorizontal && child.VerticalAlignment != VerticalAlignment.Top)
                {
                    if (child.VerticalAlignment == VerticalAlignment.Center)
                        crossY += (contentRect.H - child.DrawRect.H) / 2;
                    else if (child.VerticalAlignment == VerticalAlignment.Bottom)
                        crossY += contentRect.H - child.DrawRect.H - child.MarginExtent.Bottom;
                }
                else if (!isHorizontal && child.HorizontalAlignment != HorizontalAlignment.Left)
                {
                    if (child.HorizontalAlignment == HorizontalAlignment.Center)
                        crossX += (contentRect.W - child.DrawRect.W) / 2;
                    else if (child.HorizontalAlignment == HorizontalAlignment.Right)
                        crossX += contentRect.W - child.DrawRect.W - child.MarginExtent.Right;
                }

                SDL.FPoint childAnchor = new()
                {
                    X = crossX + (isHorizontal ? offset : 0),
                    Y = crossY + (isHorizontal ? 0 : offset)
                };

                child.OnArrange(window, childAnchor);

                offset += (isHorizontal ? child.DrawRect.W : child.DrawRect.H) +
                          (isHorizontal ? child.MarginExtent.Horizontal : child.MarginExtent.Vertical) +
                          this.Spacing;
            }
        }

        #endregion

        #region StackPanel: Hooks

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (!this.IsInitialized)
                return;

            base.OnPropertyChanged(e);

            bool invalidateMeasure = false;
            bool invalidateLayout = false;
            bool invalidateRender = false;

            switch (e.PropertyName)
            {
                case nameof(this.StackOrientation):
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
    }
}