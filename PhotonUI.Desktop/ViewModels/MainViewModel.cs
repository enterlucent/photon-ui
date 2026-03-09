using PhotonUI.Interfaces.Services;
using PhotonUI.ViewModels;

namespace PhotonUI.Demo.ViewModels
{
    public partial class MainViewModel(IServiceProvider serviceProvider, IBindingService bindingService)
        : ViewModelBase(serviceProvider, bindingService)
    {

    }
}