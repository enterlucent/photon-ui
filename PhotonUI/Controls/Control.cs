using CommunityToolkit.Mvvm.ComponentModel;
using PhotonUI.Events;
using PhotonUI.Events.Framework;
using PhotonUI.Events.Platform;
using PhotonUI.Exceptions;
using PhotonUI.Extensions;
using PhotonUI.Interfaces;
using PhotonUI.Interfaces.Services;
using PhotonUI.Models;
using PhotonUI.Models.Properties;
using SDL3;
using System.ComponentModel;
using System.Reflection;

namespace PhotonUI.Controls
{
    public enum TunnelDirection
    {
        TopDown,
        BottomUp
    }

    public partial class Control(IServiceProvider serviceProvider, IBindingService bindingService, IKeyBindingService keyBindingService)
        : ObservableObject, IDisposable, IControlProperties
    {
        protected readonly IServiceProvider ServiceProvider = serviceProvider;
        protected readonly IBindingService BindingService = bindingService;
        protected readonly IKeyBindingService KeyBindingService = keyBindingService;

        public Window? Window;
        public Size IntrinsicSize;
        public SDL.FRect DrawRect;

        public virtual Thickness MarginExtent
            => this.Margin;
        public virtual Thickness PaddingExtent
            => this.Padding;

        [ObservableProperty] private Control? parent = null;
        [ObservableProperty] private string name = typeof(Control).Name;
        [ObservableProperty] private object? dataContext = null;

        [ObservableProperty] private float x = 0;
        [ObservableProperty] private float y = 0;

        [ObservableProperty] private bool isVisible = true;
        [ObservableProperty] private bool isEnabled = true;
        [ObservableProperty] private bool isFocused = false;

        #region Control: Style Properties

        [ObservableProperty] private float minWidth = ControlProperties.Default.MinWidth;
        [ObservableProperty] private float minHeight = ControlProperties.Default.MinHeight;
        [ObservableProperty] private float maxWidth = ControlProperties.Default.MaxWidth;
        [ObservableProperty] private float maxHeight = ControlProperties.Default.MaxHeight;

        [ObservableProperty] private bool focusOnHover = ControlProperties.Default.FocusOnHover;
        [ObservableProperty] private int tabIndex = ControlProperties.Default.TabIndex;
        [ObservableProperty] private int zIndex = ControlProperties.Default.ZIndex;

        [ObservableProperty] private HorizontalAlignment horizontalAlignment = ControlProperties.Default.HorizontalAlignment;
        [ObservableProperty] private VerticalAlignment verticalAlignment = ControlProperties.Default.VerticalAlignment;
        [ObservableProperty] private Thickness margin = ControlProperties.Default.Margin;
        [ObservableProperty] private Thickness padding = ControlProperties.Default.Padding;

        [ObservableProperty] private SDL.Color backgroundColor = ControlProperties.Default.BackgroundColor;
        [ObservableProperty] private float opacity = ControlProperties.Default.Opacity;

        [ObservableProperty] private bool clipToBounds = ControlProperties.Default.ClipToBounds;
        [ObservableProperty] private bool isHitTestVisible = ControlProperties.Default.IsHitTestVisible;

        [ObservableProperty] private SDL.SystemCursor cursor = ControlProperties.Default.Cursor;

        #endregion

        #region Control: Actions

        public Action<Control>? InitializedAction { get; set; }
        public Action<Control>? IntrinsicAction { get; set; }
        public Action<Control>? MeasuredAction { get; set; }
        public Action<Control>? ArrangedAction { get; set; }
        public Action<Control>? RenderedAction { get; set; }

        public Action<Control>? TickAction { get; set; }
        public Action<Control>? LateTickAction { get; set; }
        public Action<Control>? PostTickAction { get; set; }

        public Action<FocusGainedEventArgs>? FocusGainedAction { get; set; }
        public Action<FocusLostEventArgs>? FocusLostAction { get; set; }

        public Action<MouseEnterEventArgs>? MouseEnterAction { get; set; }
        public Action<MouseExitEventArgs>? MouseExitAction { get; set; }

        public Action<MouseClickEventArgs>? MouseClickAction { get; set; }
        public Action<MouseWheelEventArgs>? MouseWheelAction { get; set; }

        #endregion

        #region Control: States

        public bool IsOpaque => this.IsVisible && this.BackgroundColor.A == 255 && this.Opacity == 1f;
        public bool IsInteractable => this.IsEnabled && this.IsVisible && this.IsHitTestVisible;

        public bool IsDescendant(Control child)
        {
            if (child == null) return false;

            List<Control> path = Photon.GetControlPath(this, c => c == child);

            return path.Count > 0;
        }

        public bool IsInitialized { get; protected set; } = false;
        public bool IsIntrinsicDirty { get; set; } = true;
        public bool IsMeasureDirty { get; set; } = true;
        public bool IsLayoutDirty { get; set; } = true;
        public bool IsRenderDirty { get; set; } = true;
        public bool IsBoundryDirty { get; set; } = true;

        #endregion

        #region Control: Service 

        public virtual T Create<T>() where T : class
            => DependencyInjectionExtensions.Create<T>(this.ServiceProvider);

        public virtual void Bind(Control target, string targetProperty, object source, string sourceProperty, bool twoWay = false)
            => this.BindingService.Bind(target, targetProperty, source, sourceProperty, twoWay);
        public virtual void Unbind(Control target, string targetProperty)
            => this.BindingService.Unbind(target, targetProperty);

        public virtual void BindKeys(SDL.Keycode key, SDL.Keymod mod, Action action)
            => this.KeyBindingService.RegisterForControl(this, key, mod, action);
        public virtual void BindStyles<T>(Control target, bool bidirectional = false) where T : IStyleProperties
        {
            Type interfaceType = typeof(T);

            foreach (PropertyInfo prop in interfaceType.GetProperties())
                this.Bind(target, prop.Name, this, prop.Name, bidirectional);
        }

        public virtual T FromControl<T>() where T : struct, IStyleProperties
        {
            Type targetType = typeof(T);

            ConstructorInfo? ctor = targetType.GetConstructors().FirstOrDefault()
                ?? throw new InvalidOperationException($"No constructor found for {targetType.Name}");

            object?[] parameters = [.. ctor.GetParameters().Select(p =>
            {
                string name = p.Name ?? throw new InvalidOperationException($"Constructor parameter has no name in {targetType.Name}");

                PropertyInfo? controlProp = this.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                    ?? throw new InvalidOperationException($"No property {name} on {this.GetType().Name}");

                return controlProp.GetValue(this);
            })];

            return (T)ctor.Invoke(parameters);
        }
        public virtual void ApplyProperties<T>(T props) where T : IStyleProperties
        {
            Type sourceType = typeof(T);
            Type targetType = this.GetType();

            foreach (PropertyInfo prop in sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                PropertyInfo? targetProp =
                    targetType.GetProperty(prop.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (targetProp != null && targetProp.CanWrite)
                {
                    object? value = prop.GetValue(props);

                    targetProp.SetValue(this, value);
                }
            }
        }
        public virtual void ApplyDefault<T>(bool overrideValues = false) where T : struct, IStyleProperties
        {
            Type targetType = typeof(T);

            PropertyInfo? defaultProp = targetType.GetProperty("Default", BindingFlags.Public | BindingFlags.Static)
                ?? throw new InvalidOperationException($"Type {targetType.Name} does not define a static Default property.");

            object? defaultInstance = defaultProp.GetValue(null);

            if (defaultInstance is not T props)
                throw new InvalidOperationException($"Default property on {targetType.Name} is not of type {targetType.Name}.");

            Type sourceType = typeof(T);
            Type controlType = this.GetType();

            foreach (PropertyInfo prop in sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                PropertyInfo? targetProp = controlType.GetProperty(prop.Name,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (targetProp != null && targetProp.CanWrite)
                {
                    object? currentValue = targetProp.GetValue(this);
                    object? defaultValue = prop.GetValue(props);
                    object? typeDefault = targetProp.PropertyType.IsValueType
                        ? Activator.CreateInstance(targetProp.PropertyType)
                        : null;

                    if (overrideValues || Equals(currentValue, typeDefault))
                        targetProp.SetValue(this, defaultValue);
                }
            }
        }

        #endregion

        #region Control: Framework

        protected virtual void ValidateStyles(params IStyleProperties[] properties)
        {
            HashSet<Type> supportedInterfaces = [.. this.GetType().GetInterfaces()];
            List<string> unsupported = [];

            foreach (IStyleProperties prop in properties)
            {
                Type[] propInterfaces = prop?.GetType().GetInterfaces() ?? [];

                bool supported = prop != null && propInterfaces.Any(i => supportedInterfaces.Contains(i));

                if (!supported)
                {
                    string typeName = prop?.GetType().FullName ?? "null";

                    unsupported.Add(typeName);
                }
            }

            if (unsupported.Count > 0)
            {
                string message = $"Control '{this.GetType().Name}' does not support property types: {string.Join(", ", unsupported)}";

                throw new InvalidStyleException(message, typeof(IStyleProperties));
            }
        }
        public virtual void ApplyStyles(params IStyleProperties[] properties)
        {
            this.ValidateStyles(properties);

            foreach (IStyleProperties prop in properties)
            {
                switch (prop)
                {
                    case IControlProperties controlProperties:
                        this.ApplyProperties(controlProperties);
                        break;
                }
            }
        }

        public virtual bool TunnelControls(Func<Control, bool> tunneler, TunnelDirection direction = TunnelDirection.TopDown)
            => tunneler(this);
        public virtual bool BubbleControls(Func<Control, bool> bubbler)
        {
            if (!bubbler(this))
                return false;

            if (this.Parent != null)
                return this.Parent.BubbleControls(bubbler);
            else if (this is not Controls.Window)
                throw new InvalidOperationException($"Control {this.Name} has no parent but is not a Window. Bubble chain broken.");

            return true;
        }

        public virtual void RequestMeasure()
        {
            this.TunnelControls(control =>
            {
                control.IsIntrinsicDirty = true;
                control.IsMeasureDirty = true;
                return true;
            });
        }
        public virtual void RequestArrange()
        {
            this.TunnelControls(control =>
            {
                control.IsLayoutDirty = true;
                return true;
            });
        }
        public virtual void RequestRender(bool invalidate = true)
        {
            this.IsRenderDirty = true;

            if (invalidate)
                Photon.InvalidateRenderChain(this);

            this.TunnelControls(control =>
            {
                if (control != this)
                    control.RequestRender(false);
                return true;
            });
        }

        public virtual void FrameworkEventBubble(Window window, FrameworkEventArgs e)
        {
            if (!this.IsInitialized)
                return;

            this.OnEvent(window, e);

            this.BubbleControls(control =>
            {
                if (!control.IsInitialized)
                    return true;

                control.OnEvent(window, e);
                return true;
            });
        }

        public virtual void FrameworkInitialize(Window window)
        {
            ArgumentNullException.ThrowIfNull(window);

            if (!this.IsInitialized)
            {
                this.Window = window;
                this.IsInitialized = true;

                this.OnInitialize(window);

                window.SetTabStop(this, this.TabIndex);

                if (this.IsFocused)
                    window.SetFocus(this);
            }

            this.TunnelControls(control =>
            {
                if (control != this)
                    control.FrameworkInitialize(window);
                return true;
            });

            this.InitializedAction?.Invoke(this);
        }
        public virtual void FrameworkTick(Window window) { }
        public virtual void FrameworkIntrinsic(Window window, Size content)
        {
            // Set the intrinsic size to the minimum size for this control
            this.IntrinsicSize = Photon.GetMinimumSize(this);
        }
        public virtual void FrameworkMeasure(Window window) { }
        public virtual void FrameworkArrange(Window window, SDL.FPoint anchor)
        {
            // Update our location by extending to the margins
            this.DrawRect.X = anchor.X + this.MarginExtent.Left + this.X;
            this.DrawRect.Y = anchor.Y + this.MarginExtent.Top + this.Y;
        }
        public virtual void FrameworkLateTick(Window window) { }
        public virtual void FrameworkRender(Window window, SDL.Rect? clipRect)
        {
            // Skip rendering if control is not visible
            if (this.IsVisible)
            {
                // Calculate the effective clip area based on the control's draw rect
                Photon.GetControlClipRect(this.DrawRect, this.ClipToBounds, clipRect, out SDL.Rect? effectiveClipRect);

                // Render only if the control is within the clip region or unclipped
                if (effectiveClipRect.HasValue)
                {
                    // Apply the effective clip region
                    Photon.ApplyControlClipRect(window, effectiveClipRect);

                    // Draw the control's background
                    Photon.DrawControlBackground(this);

                    // Restore the original clip region
                    Photon.ApplyControlClipRect(window, clipRect);
                }
            }
        }
        public virtual void FrameworkPostTick(Window window) { }

        #endregion

        #region Control: Hooks

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
                case nameof(this.X):
                case nameof(this.Y):
                case nameof(this.HorizontalAlignment):
                case nameof(this.VerticalAlignment):
                case nameof(this.Margin):
                case nameof(this.Padding):
                case nameof(this.MinWidth):
                case nameof(this.MinHeight):
                case nameof(this.MaxWidth):
                case nameof(this.MaxHeight):
                    invalidateMeasure = true;
                    invalidateLayout = true;
                    invalidateRender = true;
                    break;

                case nameof(this.IsVisible):
                case nameof(this.IsEnabled):
                case nameof(this.Opacity):
                case nameof(this.BackgroundColor):
                case nameof(this.ClipToBounds):
                case nameof(this.ZIndex):
                    invalidateRender = true;
                    break;
            }

            if (invalidateMeasure)
                this.Parent?.RequestMeasure();

            if (invalidateLayout)
                this.Parent?.RequestArrange();

            if (invalidateRender)
                this.RequestRender(true);

            this.FrameworkEventBubble(this.Window, new ChildPropertyChangedEventArgs(this, e));
        }

        public virtual void OnInitialize(Window window) { }
        public virtual void OnTick(Window window)
        {
            this.FrameworkTick(window);
            this.TickAction?.Invoke(this);
        }
        public virtual void OnIntrinsic(Window window, Size content)
        {
            if (this.IsIntrinsicDirty)
            {
                this.FrameworkIntrinsic(window, content);

                this.IntrinsicAction?.Invoke(this);
                this.IsIntrinsicDirty = false;
            }
        }
        public virtual void OnMeasure(Window window)
        {
            if (this.IsMeasureDirty)
            {
                this.FrameworkMeasure(window);

                this.MeasuredAction?.Invoke(this);
                this.IsMeasureDirty = false;
            }
        }
        public virtual void OnArrange(Window window, SDL.FPoint anchor)
        {
            if (this.IsLayoutDirty)
            {
                this.FrameworkArrange(window, anchor);

                this.ArrangedAction?.Invoke(this);
                this.IsLayoutDirty = false;
            }
        }
        public virtual void OnLateTick(Window window)
        {
            this.FrameworkLateTick(window);
            this.LateTickAction?.Invoke(this);
        }
        public virtual void OnRender(Window window, SDL.Rect? clipRect = null)
        {
            if (this.IsRenderDirty)
            {
                this.FrameworkRender(window, clipRect);

                this.RenderedAction?.Invoke(this);
                this.IsRenderDirty = false;
            }
        }
        public virtual void OnPostTick(Window window)
        {
            this.FrameworkPostTick(window);
            this.PostTickAction?.Invoke(this);
        }
        public virtual void OnEvent(Window window, FrameworkEventArgs e)
        {
            if (e.Handled || e.Preview) return;

            switch (e)
            {
                case FocusGainedEventArgs focusGained:
                    this.IsFocused = true;
                    this.FocusGainedAction?.Invoke(focusGained);
                    break;

                case FocusLostEventArgs focusLost:
                    this.IsFocused = false;
                    this.FocusLostAction?.Invoke(focusLost);
                    break;

                case MouseEnterEventArgs mouseEnter:
                    this.MouseEnterAction?.Invoke(mouseEnter);
                    break;

                case MouseExitEventArgs mouseExit:
                    this.MouseExitAction?.Invoke(mouseExit);
                    break;

                case MouseClickEventArgs mouseClick:
                    this.MouseClickAction?.Invoke(mouseClick);
                    break;

                case MouseWheelEventArgs mouseWheel:
                    this.MouseWheelAction?.Invoke(mouseWheel);
                    break;
            }
        }

        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}