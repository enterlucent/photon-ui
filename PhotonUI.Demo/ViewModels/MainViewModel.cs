using PhotonUI.Interfaces.Services;
using PhotonUI.ViewModels;

namespace NucleusAF.Console.ViewModel
{
    public partial class MainViewModel(IServiceProvider serviceProvider, IBindingService bindingService)
        : ViewModelBase(serviceProvider, bindingService)
    {

    }
}