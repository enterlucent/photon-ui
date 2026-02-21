using PhotonUI.Interfaces.Services;

namespace PhotonUI.Controls.Composites
{
    public abstract class UserControl(IServiceProvider serviceProvider, IBindingService bindingService)
        : Presenter(serviceProvider, bindingService)
    { }
}