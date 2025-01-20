using DataContainer;
using Dignus.Collections;
using Dignus.DependencyInjection.Attributes;
using Dignus.Log;
using Dignus.Utils;
using Macro.Models.Protocols;

namespace Macro.Infrastructure.Manager
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Singleton)]
    internal class AdManager
    {
        private readonly ArrayQueue<AdData> _adUrls = new ArrayQueue<AdData>();
        private readonly RandomGenerator _randomGenerator = new RandomGenerator();
        private WebApiManager _webApiManager;
        public AdManager(WebApiManager webApiManager)
        {
            _webApiManager = webApiManager;
        }
        public void InitializeAdUrls()
        {
        }
        public string GetRandomAdUrl()
        {
            var response = _webApiManager.Request<OnePickAdUrlResponse>(new OnePickAdUrl());

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
