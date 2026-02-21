using PhotonUI.Controls;

namespace PhotonUI.Interfaces.Services
{
    public interface IBindingService
    {
        void Bind(Control target, string targetProperty, object source, string sourceProperty, bool twoWay = false);
        void Unbind(Control target, string targetProperty);
    }
}