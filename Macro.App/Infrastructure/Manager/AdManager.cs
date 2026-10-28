using Dignus.DependencyInjection.Attributes;
using Dignus.Log;
using Macro.Models.Protocols;
using System.Threading.Tasks;

namespace Macro.Infrastructure.Manager
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Singleton)]
    internal class AdManager
    {
        private readonly WebApiManager _webApiManager;

        public AdManager(WebApiManager webApiManager)
        {
            _webApiManager = webApiManager;
        }

        public async Task<string> GetAdUrlAsync()
        {
            OnePickAdUrlResponse response = await _webApiManager.Request<OnePickAdUrlResponse>(new OnePickAdUrl()).ConfigureAwait(false);

            if (response == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(response.AdUrl))
            {
                LogHelper.Fatal("Ad URL is empty.");
            }

            return response.AdUrl;
        }
    }
}
