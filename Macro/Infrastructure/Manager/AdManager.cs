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
            var resonse = _webApiManager.Request<GetAdUrlListResponse>(new GetAdUrlList());

            if (resonse == null)
            {
                LogHelper.Error($"failed to initialize AdManager: response is null.");
                return;
            }
            _adUrls.AddRange(resonse.AdUrls);
        }
        public string GetRandomAdUrl()
        {
            if (_adUrls.Count == 0)
            {
                return string.Empty;
            }
            var totalProbability = 0;

            foreach (var item in _adUrls)
            {
                totalProbability += item.Probability;
            }

            var probability = _randomGenerator.Next(0, totalProbability) + 1;

            var cumulativeProbability = 0;
            foreach (var item in _adUrls)
            {
                cumulativeProbability += item.Probability;
                if (probability <= cumulativeProbability)
                {
                    return item.Url;
                }
            }

            LogHelper.Error("no matching Ad URL found.");
            return string.Empty;
        }
    }
}
