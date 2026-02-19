using CommunityToolkit.Mvvm.ComponentModel;
using NucleusAF.Photon.Models;
using SDL3;
using System.ComponentModel;

namespace NucleusAF.Photon.Controls
{
    public enum TunnelDirection
    {
        TopDown,
        BottomUp
    }

    public partial class Control(IServiceProvider serviceProvider)
        : ObservableObject, IDisposable
    {
        protected readonly IServiceProvider ServiceProvider = serviceProvider;

        [ObservableProperty] private Control? parent = null;
        [ObservableProperty] private string name = typeof(Control).Name;
        [ObservableProperty] private object? dataContext = null;

        [ObservableProperty] private float x = 0;
        [ObservableProperty] private float y = 0;

        [ObservableProperty] private bool isVisible = true;
        [ObservableProperty] private bool isEnabled = true;
        [ObservableProperty] private bool isFocused = false;

        #region Control: Style Properties

        [ObservableProperty] private float minWidth;
        [ObservableProperty] private float minHeight;
        [ObservableProperty] private float maxWidth;
        [ObservableProperty] private float maxHeight;

        [ObservableProperty] private bool focusOnHover;
        [ObservableProperty] private int tabIndex;
        [ObservableProperty] private int zIndex;

        [ObservableProperty] private Thickness margin;
        [ObservableProperty] private Thickness padding;

        [ObservableProperty] private SDL.Color backgroundColor;
        [ObservableProperty] private float opacity;

        [ObservableProperty] private bool clipToBounds;
        [ObservableProperty] private bool isHitTestVisible;

        [ObservableProperty] private SDL.SystemCursor cursor;

        #endregion

        #region Control: Actions

        public Action<Control>? InitializedAction { get; set; }
        public Action<Control>? MeasuredAction { get; set; }
        public Action<Control>? ArrangedAction { get; set; }
        public Action<Control>? RenderedAction { get; set; }

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

        #region Control: Framework

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

        #endregion

        #region Control: Hooks

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
                case nameof(this.X):
                case nameof(this.Y):
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

        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}