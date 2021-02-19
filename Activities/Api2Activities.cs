using System;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using Azure.DurableFunctions.Configuration;
using Azure.DurableFunctions.Models;

namespace Azure.DurableFunctions.Activities
{
    public class Api2Activities
    {
        private readonly AppConfiguration _apiConfiguration;

        public Api2Activities(IOptions<AppConfiguration> apiConfigurationOptions)
        {
            _apiConfiguration = apiConfigurationOptions.Value;
        }

        [FunctionName(Constants.Api2PayloadPostActivityFunction)]
        public async Task<bool> PostPayloadToApi([ActivityTrigger] IDurableActivityContext context, ILogger logger)
        {
            // using the context can be retrieved the parammeters passed in the function above
            // in this case I just specify the type of that one and that's it
            var payload = JsonConvert.SerializeObject(context.GetInput<EmployeePayload>());

            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri($"{_apiConfiguration.ApiBaseUrl}");
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            //HttpResponseMessage response = await httpClient.PostAsync($"{ _apiConfiguration.ApiController}/{ _apiConfiguration.ApiAction}", content);

            logger.LogInformation($"{Constants.Api2PayloadPostActivityFunction} complted");
            return await Task.Run(() => true);
        }
    }
}