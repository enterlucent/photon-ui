using CommunityToolkit.Mvvm.ComponentModel;
using PhotonUI.Controls;
using PhotonUI.Events;
using PhotonUI.Extensions;
using PhotonUI.Interfaces.Services;

namespace PhotonUI.ViewModels
{
    public abstract partial class ViewModelBase(IServiceProvider serviceProvider, IBindingService bindingService)
        : ObservableObject
    {
        protected readonly IServiceProvider ServiceProvider = serviceProvider;
        protected readonly IBindingService BindingService = bindingService;

        public virtual void OnEvent(Window window, PlatformEventArgs e) { }

        public virtual T Create<T>()
            where T : class => DependencyInjectionExtensions.Create<T>(this.ServiceProvider);

        public virtual void Bind(Control target, string targetProperty, object source, string sourceProperty, bool twoWay = false)
            => this.BindingService.Bind(target, targetProperty, source, sourceProperty, twoWay);
        public void Unbind(Control target, string targetProperty)
            => this.BindingService.Unbind(target, targetProperty);
    }
}