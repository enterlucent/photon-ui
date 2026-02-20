using CommunityToolkit.Mvvm.ComponentModel;
using PhotonUI.Events;
using PhotonUI.Events.Framework;
using PhotonUI.Exceptions;
using PhotonUI.Interfaces;
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

    public partial class Control(IServiceProvider serviceProvider)
        : ObservableObject, IDisposable, IControlProperties
    {
        protected readonly IServiceProvider ServiceProvider = serviceProvider;

        public Window? Window;

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
        public Action<Control>? MeasuredAction { get; set; }
        public Action<Control>? ArrangedAction { get; set; }
        public Action<Control>? RenderedAction { get; set; }

        public Action<Control>? TickAction { get; set; }
        public Action<Control>? LateTickAction { get; set; }
        public Action<Control>? PostTickAction { get; set; }

        public Action<FocusGainedEventArgs>? FocusGainedAction { get; set; }
        public Action<FocusLostEventArgs>? FocusLostAction { get; set; }

        #endregion

        #region Control: States

        public bool IsOpaque => this.IsVisible && this.BackgroundColor.A == 255 && this.Opacity == 1f;
        public bool IsInteractable => this.IsEnabled && this.IsVisible && this.IsHitTestVisible;

        public bool IsInitialized { get; protected set; } = false;
        public bool IsMeasureDirty { get; set; } = true;
        public bool IsLayoutDirty { get; set; } = true;
        public bool IsRenderDirty { get; set; } = true;
        public bool IsBoundryDirty { get; set; } = true;

        #endregion

        #region Control: Service 

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

        public virtual void RequestMeasure()
        {
            this.TunnelControls(control =>
            {
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

            this.TunnelControls(control =>
            {
                if (control != this)
                    control.RequestRender(false);
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
        public virtual void FrameworkIntrinsic(Window window, Size content) { }
        public virtual void FrameworkMeasure(Window window) { }
        public virtual void FrameworkArrange(Window window, SDL.FPoint anchor) { }
        public virtual void FrameworkLateTick(Window window) { }
        public virtual void FrameworkRender(Window window, SDL.Rect? clipRect) { }
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
        }

        public virtual void OnInitialize(Window window) { }
        public virtual void OnTick(Window window) { }
        public virtual void OnMeasure(Window window) { }
        public virtual void OnArrange(Window window, SDL.FPoint anchor) { }
        public virtual void OnLateTick(Window window) { }
        public virtual void OnRender(Window window, SDL.Rect? clipRect = null) { }
        public virtual void OnPostTick(Window window) { }
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
            }
        }

        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}