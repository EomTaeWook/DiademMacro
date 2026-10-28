using System;
using DataContainer;
using Dignus.Collections;
using Dignus.DependencyInjection.Attributes;
using Dignus.Log;
using Macro.Models.Protocols;
using System.Threading.Tasks;

namespace Macro.Infrastructure.Manager
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Singleton)]
    internal class AdManager
    {
        private readonly ArrayQueue<AdData> _adUrls = new ArrayQueue<AdData>();
        private readonly Random _randomGenerator = new Random();
        private readonly WebApiManager _webApiManager;
        public AdManager(WebApiManager webApiManager)
        {
            _webApiManager = webApiManager;
        }
        public void InitializeAdUrls()
        {
        }
        public async Task<string> GetRandomAdUrl()
        {
            OnePickAdUrlResponse response = await _webApiManager.Request<OnePickAdUrlResponse>(new OnePickAdUrl()).ConfigureAwait(false);

            if (response == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(response.AdUrl))
            {
                LogHelper.Fatal($"ad Url is empty.");
            }

            return response.AdUrl;
        }
    }
}
