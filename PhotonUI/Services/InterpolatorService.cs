using PhotonUI.Interfaces.Services;

namespace PhotonUI.Services
{
    public interface IInterpolator { }
    public interface IInterpolator<T> : IInterpolator
    {
        T Lerp(T start, T end, float progress);
    }

    public class InterpolatorService : IInterpolatorService
    {
        private readonly Dictionary<Type, object> map = [];

        public InterpolatorService(IEnumerable<IInterpolator> interpolators)
        {
            foreach (object interpolator in interpolators)
            {
                Type? iface =
                    interpolator
                        .GetType()
                        .GetInterfaces()
                        .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IInterpolator<>));

                if (iface != null)
                {
                    Type targetType = iface.GetGenericArguments()[0];

                    this.map[targetType] = interpolator;
                }
            }
        }

        public void Register<T>(IInterpolator<T> interpolator)
            => this.map[typeof(T)] = interpolator;

        public IInterpolator<T> Get<T>()
        {
            if (this.map.TryGetValue(typeof(T), out object? impl))
                return (IInterpolator<T>)impl;

            throw new InvalidOperationException($"No interpolator registered for {typeof(T)}");
        }
    }
}