using PhotonUI.Interfaces.Services;
using PhotonUI.ViewModels;

namespace PhotonUI.Desktop.ViewModels
{
    public partial class MainViewModel(IServiceProvider serviceProvider, IBindingService bindingService)
        : ViewModelBase(serviceProvider, bindingService)
    {

    }
}