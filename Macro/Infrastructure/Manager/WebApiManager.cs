using Dignus.DependencyInjection.Attributes;
using Dignus.Log;
using Dignus.Utils;
using Dignus.Utils.Extensions;
using Macro.Models.Protocols;
using System;
using System.Threading.Tasks;
using Utils;

namespace Macro.Infrastructure.Manager
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Transient)]
    internal class WebApiManager
    {
        private readonly HttpRequester _httpRequester = new HttpRequester();


        public TResponse Request<TResponse>(IAPIRequest request)
            where TResponse : IAPIResponse
        {
            var baseUri = ConstHelper.MacroUri;
#if DEBUG
            //baseUri = "http://localhost:9100/macro";
#endif
            var response = Task.Run<TResponse>(async () =>
            {
                try
                {
                    var url = $"{baseUri}/{(request.GetType().Name)}";
                    var requestBody = JsonHelper.SerializeObject(request);
                    var responseJson = await _httpRequester.PostByJsonAsync(url, requestBody);
                    return JsonHelper.DeserializeObject<TResponse>(responseJson);
                }
                catch (Exception ex)
                {
                    LogHelper.Error(ex);
                }
                return default;

            }).GetResult();

            return response;
        }

        public TResponse Request<TRequest, TResponse>(TRequest request)
            where TRequest : IAPIRequest
            where TResponse : IAPIResponse
        {
            return Request<TResponse>(request);
        }
    }
}
