using Dignus.DependencyInjection.Attributes;
using Dignus.Log;
using Macro.Models.Protocols;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace Macro.Infrastructure.Manager
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Transient)]
    internal class WebApiManager
    {
        public async Task<TResponse> Request<TResponse>(IAPIRequest request) where TResponse : IAPIResponse
        {
            try
            {
                string address = $"{ConstHelper.MacroUri}/{request.GetType().Name}";
                using HttpClient httpClient = new();
                using StringContent content = new(JsonHelper.SerializeObject(request), Encoding.UTF8, "application/json");
                using HttpResponseMessage response = await httpClient.PostAsync(address, content).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                string responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonHelper.DeserializeObject<TResponse>(responseJson);
            }
            catch (Exception exception)
            {
                LogHelper.Error(exception);
                return default(TResponse);
            }
        }

        public Task<TResponse> Request<TRequest, TResponse>(TRequest request) where TRequest : IAPIRequest where TResponse : IAPIResponse
        {
            return Request<TResponse>(request);
        }
    }
}
