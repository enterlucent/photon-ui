using PhotonUI.Interfaces.Services;

namespace PhotonUI.Controls.Composites
{
    public partial class UserWindow(IServiceProvider serviceProvider, IBindingService bindingService, IInputService inputService, IAnimationBuilder animationBulder)
        : Window(serviceProvider, bindingService, inputService, animationBulder)
    { }
}