using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace WebPageUpdated
{
    public static class AddWebPageUpdatedJob
    {
        [FunctionName("AddWebPageUpdatedJob")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            [Queue("webpage-scan-jobs", Connection = "AzureWebJobsStorage")] ICollector<string> msg,
            ILogger log)
        {
            var request = await req.GetJsonBody<AddWebPageUpdatedJobDto, AddWebPageUpdatedJobValidator>();

            if (!request.IsValid)
            {
                log.LogInformation($"Invalid form data.");
                return request.ToBadRequest();
            }

            using (var pageService = await WebPageService.LoadPage(request.Value.WebPageUrl))
            {
                // Get current element value so that when we run the job, we can tell if it's changed.
                request.Value.ElementMd5LastRun = await pageService.GetMd5ValueOfElement(request.Value.PathOfElementToWatch);
            }

            msg.Add(JsonConvert.SerializeObject(request.Value));

            return new NoContentResult();
        }
    }
}
