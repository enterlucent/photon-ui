using CommunityToolkit.Mvvm.ComponentModel;
using PhotonUI.Controls.Content;
using PhotonUI.Extensions;
using PhotonUI.Interfaces;
using PhotonUI.Interfaces.Services;
using PhotonUI.Models;
using PhotonUI.Models.Properties;
using SDL3;
using System.ComponentModel;

namespace PhotonUI.Controls.Interaction
{
    public partial class TextButton : ClickSurface, IBorderProperties
    {
        public TextBlock TextLabel { get; protected set; }

        [ObservableProperty] private BorderColors borderColors = BorderProperties.Default.BorderColors;
        [ObservableProperty] private Thickness borderThickness = BorderProperties.Default.BorderThickness;

        public TextButton(IServiceProvider serviceProvider, IBindingService bindingService, IKeyBindingService keyBindingService)
            : base(serviceProvider, bindingService, keyBindingService)
        {
            this.TextLabel = this.Create<TextBlock>();
        }

        #region Button: Framework

        public override void ApplyStyles(params IStyleProperties[] properties)
        {
            this.ValidateStyles(properties);

            base.ApplyStyles(properties);

            foreach (IStyleProperties prop in properties)
            {
                switch (prop)
                {
                    case ITextProperties props:
                        this.ApplyProperties(props);
                        break;
                }
            }
        }

        public override void RequestRender(bool invalidate = true)
        {
            this.TextLabel.RequestRender(false);

            base.RequestRender(invalidate);
        }

        #endregion

        #region Button: Hooks

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
                case nameof(this.BorderThickness):
                    invalidateMeasure = true;
                    invalidateLayout = true;
                    invalidateRender = true;
                    break;

                case nameof(this.BorderColors):
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

        public override void FrameworkInitialize(Window window)
        {
            base.FrameworkInitialize(window);

            this.TextLabel.ApplyStyles(this);
            this.TextLabel.VerticalAlignment = VerticalAlignment.Center;
            this.TextLabel.HorizontalAlignment = HorizontalAlignment.Center;
            this.TextLabel.TextBackgroundColor = new SDL.Color();
            this.TextLabel.MinHeight = 0;
            this.TextLabel.MinWidth = 0;

            this.BackgroundColor = new SDL.Color() { R = 0, G = 0, B = 0, A = 255 };
            this.BorderColors = new(new SDL.Color() { R = 255, G = 255, B = 255, A = 255 });
            this.BorderThickness = new(2);

            this.Child = this.TextLabel;
        }
        public override void FrameworkMeasure(Window window)
        {
            if (this.Child != null)
            {
                SDL.FRect contentRect = this.DrawRect.Deflate(this.PaddingExtent);

                if (contentRect.W <= 0 || contentRect.H <= 0) return;

                this.TextLabel.BuildLineDataCache(contentRect.W);

                Size contentSize = new(
                    Photon.GetControlLineMaxPixelWidth(this.TextLabel.LineDataCache),
                    Photon.GetControlLinePixelHeight(this.TextLabel.LineDataCache));

                // Stretch within remaining space after total offsets (X + margins)
                float stretchedW = Photon.GetStretchedWidth(
                    this.Child.HorizontalAlignment,
                    contentSize.Width,
                    contentRect.W,
                    this.Child.X + this.Child.MarginExtent.Horizontal);

                float stretchedH = Photon.GetStretchedHeight(
                    this.Child.VerticalAlignment,
                    contentSize.Height,
                    contentRect.H,
                    this.Child.Y + this.Child.MarginExtent.Vertical);

                // Set child's content size within min/max bounds
                this.Child.DrawRect.W = Math.Clamp(stretchedW, this.Child.MinWidth, this.Child.MaxWidth);
                this.Child.DrawRect.H = Math.Clamp(stretchedH, this.Child.MinHeight, this.Child.MaxHeight);

                // Set child's content size within min/max bounds
                this.Child.DrawRect.W = Math.Clamp(this.Child.DrawRect.W, 0, contentRect.W);
                this.Child.DrawRect.H = Math.Clamp(this.Child.DrawRect.H, 0, contentRect.H);
            }
        }

        public override void FrameworkRender(Window window, SDL.Rect? clipRect = null)
        {
            if (this.IsVisible)
            {
                Photon.GetControlClipRect(this.DrawRect, this.ClipToBounds, clipRect, out SDL.Rect? effectiveClipRect);

                if (effectiveClipRect.HasValue)
                {
                    SDL.Rect modifiedClipRect = effectiveClipRect.Value.Deflate(this.PaddingExtent);

                    Photon.ApplyControlClipRect(window, effectiveClipRect);
                    Photon.DrawControlBackground(this, this.GetControlPropertiesState());
                    Photon.DrawControlBorder(this, this.GetBorderPropertiesState(), this.GetControlPropertiesState());

                    Photon.ApplyControlClipRect(window, modifiedClipRect);
                    this.TextLabel.OnRender(window, modifiedClipRect);
                    Photon.ApplyControlClipRect(window, clipRect);
                }
            }
        }

        #endregion

        #region ClickSurface: Helpers

        protected override ControlProperties GetControlPropertiesState()
        {
            ControlProperties props = this.FromControl<ControlProperties>();

            if (this.IsPressed)
            {
                this.TextLabel.BackgroundColor = this.PressedBackgroundColor;

                return props with { BackgroundColor = this.PressedBackgroundColor };
            }
            else if (this.IsHovering)
            {
                this.TextLabel.BackgroundColor = this.HoverBackgroundColor;

                return props with { BackgroundColor = this.HoverBackgroundColor };
            }

            this.TextLabel.BackgroundColor = this.BackgroundColor;

            return props;
        }

        #endregion
    }
}