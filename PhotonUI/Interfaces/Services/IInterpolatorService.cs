using PhotonUI.Services;

namespace PhotonUI.Interfaces.Services
{
    public interface IInterpolatorService
    {
        IInterpolator<T> Get<T>();
        void Register<T>(IInterpolator<T> interpolator);
    }
}