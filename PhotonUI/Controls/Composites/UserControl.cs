using PhotonUI.Interfaces.Services;

namespace PhotonUI.Controls.Composites
{
    public abstract class UserControl(IServiceProvider serviceProvider, IBindingService bindingService, IKeyBindingService keyBindingService)
        : Presenter(serviceProvider, bindingService, keyBindingService)
    { }
}