using Dignus.DependencyInjection;

namespace Macro.Infrastructure.Manager
{
    public class ServiceResolver
    {
        private static ServiceContainer _serviceContainer;
        public static void SetContainer(ServiceContainer serviceContainer)
        {
            _serviceContainer = serviceContainer;
        }
        public static T GetService<T>()
        {
            return _serviceContainer.GetService<T>();
        }
    }
}
