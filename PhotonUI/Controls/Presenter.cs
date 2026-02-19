using CommunityToolkit.Mvvm.ComponentModel;

namespace PhotonUI.Controls
{
    public partial class Presenter(IServiceProvider serviceProvider)
        : Control(serviceProvider)
    {
        [ObservableProperty] private Control? child;

        partial void OnChildChanging(Control? value)
        {
            if (child != null)
                child.Parent = null;
        }
        partial void OnChildChanged(Control? value)
        {
            if (value != null)
            {
                if (value.Parent != null)
                    throw new InvalidOperationException("Control already has a parent.");

                value.Parent = this;

                this.RequestArrange();

                if (IsInitialized)
                {
                    if (Window == null)
                        throw new InvalidOperationException($"Control '{value.Name}' is not associated with a RootWindow.");
                }
            }
        }

        #region Presenter: Framework

        public override bool TunnelControls(Func<Control, bool> traveler, TunnelDirection direction = TunnelDirection.TopDown)
        {
            if (direction == TunnelDirection.TopDown)
            {
                if (!traveler(this)) return false;
                if (this.Child != null && !this.Child.TunnelControls(traveler, direction)) return false;
            }
            else
            {
                if (this.Child != null && !this.Child.TunnelControls(traveler, direction)) return false;
                if (!traveler(this)) return false;
            }

            return true;
        }

        #endregion
    }
}