using PhotonUI.Interfaces.Services;

namespace PhotonUI.Controls.Composites
{
    public partial class UserWindow(IServiceProvider serviceProvider, IBindingService bindingService)
        : Window(serviceProvider, bindingService)
    { }
}