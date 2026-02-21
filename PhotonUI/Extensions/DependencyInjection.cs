using Microsoft.Extensions.DependencyInjection;

namespace PhotonUI.Extensions
{
    public static class DependencyInjectionExtensions
    {
        public static T Create<T>(IServiceProvider provider, params object[] args)
            where T : class
        {
            T? resolved = provider.GetService<T>();

            if (resolved != null)
                return resolved;

            try
            {
                return ActivatorUtilities.CreateInstance<T>(provider, args);
            }
            catch
            {
                return (T)Activator.CreateInstance(typeof(T), args)!;
            }
        }
    }
}