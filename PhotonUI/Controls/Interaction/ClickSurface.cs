using CommunityToolkit.Mvvm.ComponentModel;
using PhotonUI.Controls.Decorators;
using PhotonUI.Events;
using PhotonUI.Events.Platform;
using PhotonUI.Extensions;
using PhotonUI.Interfaces;
using PhotonUI.Interfaces.Services;
using PhotonUI.Models;
using PhotonUI.Models.Properties;
using SDL3;
using System.Windows.Input;

namespace PhotonUI.Controls.Interaction
{
    public partial class ClickSurface(IServiceProvider serviceProvider, IBindingService bindingService, IKeyBindingService keyBindingService)
        : Border(serviceProvider, bindingService, keyBindingService), IBorderProperties, IHoverProperties, IPressedProperties
    {
        protected bool IsHovering = false;
        protected bool IsPressed = false;

        [ObservableProperty] private BorderColors borderColors = BorderProperties.Default.BorderColors;
        [ObservableProperty] private Thickness borderThickness = BorderProperties.Default.BorderThickness;

        [ObservableProperty] private SDL.Color hoverBackgroundColor = HoverProperties.Default.HoverBackgroundColor;
        [ObservableProperty] private BorderColors hoverBorderColors = HoverProperties.Default.HoverBorderColors;
        [ObservableProperty] private Thickness hoverBorderThickness = HoverProperties.Default.HoverBorderThickness;
        [ObservableProperty] private SDL.Color hoverTextColor = HoverProperties.Default.HoverTextColor;
        [ObservableProperty] private float hoverOpacity = HoverProperties.Default.HoverOpacity;
        [ObservableProperty] private float hoverScale = HoverProperties.Default.HoverScale;

        [ObservableProperty] private SDL.Color pressedBackgroundColor = PressedProperties.Default.PressedBackgroundColor;
        [ObservableProperty] private BorderColors pressedBorderColors = PressedProperties.Default.PressedBorderColors;
        [ObservableProperty] private Thickness pressedBorderThickness = PressedProperties.Default.PressedBorderThickness;
        [ObservableProperty] private SDL.Color pressedTextColor = PressedProperties.Default.PressedTextColor;
        [ObservableProperty] private float pressedOpacity = PressedProperties.Default.PressedOpacity;
        [ObservableProperty] private float pressedScale = PressedProperties.Default.PressedScale;

        public Action<MouseClickEventArgs>? OnClickAction { get; set; }

        public ICommand? OnClick { get; set; }

        #region ClickSurface: Framework

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

                    case IHoverProperties props:
                        this.ApplyProperties(props);
                        break;

                    case IPressedProperties props:
                        this.ApplyProperties(props);
                        break;
                }
            }
        }

        public override void FrameworkRender(Window window, SDL.Rect? clipRect = null)
        {
            if (this.IsVisible)
            {
                Photon.GetControlClipRect(this.DrawRect, this.ClipToBounds, clipRect, out SDL.Rect? effectiveClipRect);

                if (effectiveClipRect.HasValue)
                {
                    Photon.ApplyControlClipRect(window, effectiveClipRect);

                    Photon.DrawControlBackground(this, this.GetControlPropertiesState());
                    Photon.DrawControlBorder(this, this.GetBorderPropertiesState(), this.GetControlPropertiesState());

                    SDL.Rect modifiedClipRect = effectiveClipRect.Value.Deflate(this.BorderThickness);

                    this.Child?.OnRender(window, modifiedClipRect);

                    Photon.ApplyControlClipRect(window, clipRect);
                }
            }
        }
        #endregion

        #region ClickSurface: Hooks

        public override void OnEvent(Window window, FrameworkEventArgs e)
        {
            if (e.Handled || e.Preview) return;

            base.OnEvent(window, e);

            switch (e)
            {
                case MouseEnterEventArgs:
                    this.IsHovering = true;
                    this.RequestRender();
                    e.Handled = true;
                    break;

                case MouseExitEventArgs:
                    this.IsHovering = false;
                    this.RequestRender();
                    e.Handled = true;
                    break;
            }

            if (e is not MouseClickEventArgs mouseClick) return;

            switch (mouseClick.NativeEvent.Type)
            {
                case (uint)SDL.EventType.MouseButtonDown:
                    if (mouseClick.Clicked == this || this.IsDescendant(mouseClick.Clicked))
                    {
                        this.IsPressed = true;
                        this.RequestRender();
                        window.CaptureMouse(this);
                        e.Handled = true;
                    }
                    break;

                case (uint)SDL.EventType.MouseButtonUp:
                    if (mouseClick.Clicked == this || this.IsDescendant(mouseClick.Clicked))
                    {
                        if (this.IsHovering && this.IsPressed)
                        {
                            this.OnClick?.Execute(mouseClick);
                            this.OnClickAction?.Invoke(mouseClick);
                        }

                        this.IsPressed = false;
                        this.RequestRender();
                        window.ReleaseMouse();
                        e.Handled = true;
                    }
                    break;
            }
        }

        #endregion

        #region ClickSurface: Helpers

        protected virtual ControlProperties GetControlPropertiesState()
        {
            ControlProperties props = this.FromControl<ControlProperties>();

            if (this.IsPressed)
            {
                return props with { BackgroundColor = this.PressedBackgroundColor };
            }
            else if (this.IsHovering)
            {
                return props with { BackgroundColor = this.HoverBackgroundColor };
            }

            return props;
        }
        protected virtual BorderProperties GetBorderPropertiesState()
        {
            BorderProperties props = this.FromControl<BorderProperties>();

            if (this.IsPressed)
            {
                return props with
                {
                    BorderColors = this.PressedBorderColors,
                    BorderThickness = this.PressedBorderThickness
                };
            }
            else if (this.IsHovering)
            {
                return props with
                {
                    BorderColors = this.HoverBorderColors,
                    BorderThickness = this.HoverBorderThickness
                };
            }

            return props;
        }

        #endregion
    }
}