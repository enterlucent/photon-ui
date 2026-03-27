using CommunityToolkit.Mvvm.ComponentModel;
using PhotonUI.Controls.Decorators;
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
using System.Windows.Input;

namespace PhotonUI.Controls.Interaction
{
    public partial class ClickSurface(IServiceProvider serviceProvider, IBindingService bindingService)
        : Border(serviceProvider, bindingService), IBorderProperties, IPressedProperties
    {
        protected bool IsHovering = false;
        protected bool IsPressed = false;

        #region ClickSurface: Style Properties

        [ObservableProperty] private BorderColors borderColors = BorderProperties.Default.BorderColors;
        [ObservableProperty] private Thickness borderThickness = BorderProperties.Default.BorderThickness;

        [ObservableProperty] private SDL.Color pressedBackgroundColor = PressedProperties.Default.PressedBackgroundColor;
        [ObservableProperty] private BorderColors pressedBorderColors = PressedProperties.Default.PressedBorderColors;
        [ObservableProperty] private Thickness pressedBorderThickness = PressedProperties.Default.PressedBorderThickness;
        [ObservableProperty] private SDL.Color pressedTextColor = PressedProperties.Default.PressedTextColor;
        [ObservableProperty] private float pressedOpacity = PressedProperties.Default.PressedOpacity;
        [ObservableProperty] private float pressedScale = PressedProperties.Default.PressedScale;

        #endregion

        #region ClickSurface: Actions

        public Action<PointerPressEventArgs>? OnClickAction { get; set; }

        #endregion

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

                    case IPressedProperties props:
                        this.ApplyProperties(props);
                        break;
                }
            }
        }

        public override void FrameworkRender(Window window, SDL.Rect? clipRect = null)
        {
            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [window, clipRect], DiagnosticPhase.Start));

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

                    PhotonDiagnostics.Emit(new RenderControlEventArgs(this));
                }
            }

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [window, clipRect], DiagnosticPhase.End));
        }
        #endregion

        #region ClickSurface: Hooks

        public ICommand? OnClick { get; set; }

        public override void OnEvent(Window window, FrameworkEventArgs e)
        {
            if (e.Handled || e.Preview) return;

            PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [window, e], DiagnosticPhase.Start));

            try
            {
                base.OnEvent(window, e);

                switch (e)
                {
                    case PointerEnterEventArgs:
                        this.IsHovering = true;
                        this.RequestRender();
                        e.Handled = true;
                        break;

                    case PointerExitEventArgs:
                        this.IsHovering = false;
                        this.RequestRender();
                        e.Handled = true;
                        break;
                }

                if (e is not PointerPressEventArgs pointerPress) return;

                switch (pointerPress.NativeEvent.Type)
                {
                    case (uint)SDL.EventType.MouseButtonDown:
                        if (pointerPress.Pressed == this || this.IsDescendant(pointerPress.Pressed))
                        {
                            this.IsPressed = true;
                            this.RequestRender();
                            window.CapturePointer(this);
                            e.Handled = true;
                        }
                        break;

                    case (uint)SDL.EventType.MouseButtonUp:
                        if (pointerPress.Pressed == this || this.IsDescendant(pointerPress.Pressed))
                        {
                            if (this.IsHovering && this.IsPressed)
                            {
                                this.OnClick?.Execute(pointerPress);
                                this.OnClickAction?.Invoke(pointerPress);
                            }

                            this.IsPressed = false;
                            this.RequestRender();
                            window.ReleasePointer();
                            e.Handled = true;
                        }
                        break;
                }

            }
            finally
            {
                PhotonDiagnostics.Emit(new ControlMethodEventArgs(this, [window, e], DiagnosticPhase.End));
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

            return props;
        }

        #endregion
    }
}