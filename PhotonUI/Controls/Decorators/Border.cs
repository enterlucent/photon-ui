using CommunityToolkit.Mvvm.ComponentModel;
using PhotonUI.Interfaces;
using PhotonUI.Interfaces.Services;
using PhotonUI.Models;
using PhotonUI.Models.Properties;
using SDL3;
using System.ComponentModel;

namespace PhotonUI.Controls.Decorators
{
    public partial class Border(IServiceProvider serviceProvider, IBindingService bindingService)
        : Presenter(serviceProvider, bindingService), IBorderProperties
    {
        [ObservableProperty] private BorderColors borderColors = BorderProperties.Default.BorderColors;
        [ObservableProperty] private Thickness borderThickness = BorderProperties.Default.BorderThickness;

        public override Thickness PaddingExtent
            => this.Padding + this.BorderThickness;

        #region Border: Framework

        public override void ApplyStyles(params IStyleProperties[] properties)
        {
            this.ValidateStyles(properties);

            base.ApplyStyles(properties);

            foreach (IStyleProperties prop in properties)
            {
                switch (prop)
                {
                    case IBorderProperties props:
                        this.ApplyProperties(props);
                        break;
                }
            }
        }

        public override void FrameworkRender(Window window, SDL.Rect? clipRect)
        {
            // Skip rendering if control is not visible
            if (!this.IsVisible) return;

            // Calculate the effective clip region for the control
            Photon.GetControlClipRect(this.DrawRect, this.ClipToBounds, clipRect, out SDL.Rect? effectiveClipRect);

            // Only render if control intersects the viewport or is unclipped
            if (effectiveClipRect.HasValue)
            {
                // Apply the effective clipping region
                Photon.ApplyControlClipRect(window, effectiveClipRect);

                // Draw the control's background
                base.FrameworkRender(window, clipRect);

                // Draw the control's border 
                Photon.DrawControlBorder(this);

                // Restore the original clip region
                Photon.ApplyControlClipRect(window, clipRect);
            }
        }

        #endregion

        #region Border: Hooks

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

        #endregion
    }
}