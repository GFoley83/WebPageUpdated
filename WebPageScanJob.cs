using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace WebPageUpdated
{
    public static class WebPageScanJob
    {

#if DEBUG
        // Start the Azure Func straight away when debugging locally
        private const bool runOnStartUp = true;
#else
        private const bool runOnStartUp = false;
#endif
        // private const string runEvery5MinutesBetween8AMAnd6PM = "0 */5 8-18 * * *";

        [FunctionName("WebPageScanJob")]
        public static async Task Run(
            [QueueTrigger("webpage-scan-jobs", Connection = "AzureWebJobsStorage")]AddWebPageUpdatedJobDto job,
            //[TimerTrigger(runEvery5MinutesBetween8AMAnd6PM, RunOnStartup = runOnStartUp)]TimerInfo myTimer,
            [Queue("webpage-scan-jobs", Connection = "AzureWebJobsStorage")]CloudQueue outputQueue,
            ILogger log)
        {
            var pageService = await WebPageService.LoadPage(job.WebPageUrl);
            var eleMd5 = await pageService.GetMd5ValueOfElement(job.PathOfElementToWatch);

            // If element has been updated
            if (!eleMd5.Equals(job.ElementMd5LastRun))
            {
                var screenshot = await pageService.TakeScreenshot();
                var screenshotStorageUrl = await SaveScreenshotToStorage(screenshot);
                await SendNotificationEmail(job, screenshotStorageUrl);
                log.LogInformation("It's updated! - element: \"{0}\" on {1}", job.PathOfElementToWatch, job.WebPageUrl);

                // If we're only watching for one change, then we're finished
                if (!job.WatchIndefinitely)
                {
                    return;
                }
            }

            job.ElementMd5LastRun = eleMd5;

            // Add message to queue and make it visible after a specific time.
            var cqm = new CloudQueueMessage(JsonConvert.SerializeObject(job));
            await outputQueue.AddMessageAsync(cqm, null, TimeSpan.FromMinutes(double.Parse(Environment.GetEnvironmentVariable("HideNewMessagesForInMinutes"))), null, null);

            log.LogInformation("No change for element: \"{0}\" on {1}", job.PathOfElementToWatch, job.WebPageUrl);
        }

        private static Task<string> SaveScreenshotToStorage(string screenshotPath)
        {
            var storageService = new BlobStorageService(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            return storageService.UploadFileToBlob(screenshotPath);
        }

        private async static Task SendNotificationEmail(AddWebPageUpdatedJobDto job, string screenshot)
        {
            var apiKey = Environment.GetEnvironmentVariable("SendGridApiKey");
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("noreply@WebPageUpdated.com", "Webpage Updated");
            var subject = $"{job.WebPageUrl} has been updated!";
            var to = new EmailAddress(job.Email);
            //var plainTextContent = "https://docs.abp.io/en/abp/latest has been updated.";

            var template = await File.ReadAllTextAsync(Environment.CurrentDirectory + "\\email-template.html");
            var renderedTemplate = template
                .Replace("~~Text~~", $"{job.WebPageUrl}, has been updated!")
                .Replace("~~ImageSrc~~", screenshot)
                .Replace("~~WebPage~~", job.WebPageUrl);

            var msg = MailHelper.CreateSingleEmail(from, to, subject, null, renderedTemplate);
            await client.SendEmailAsync(msg);
        }

        //private async Task GetValueFromPageElement()
        //{
        //    var html = @"https://docs.abp.io/en/abp/latest";
        //    HtmlWeb web = new HtmlWeb();
        //    var htmlDoc = web.Load(html);
        //    var selectElementLatestVersion = htmlDoc.DocumentNode
        //        .SelectSingleNode("/html/body/div[1]/div/div/div[1]/div/div[2]/div[1]/div[1]/div/div/div/select/option[1]");

        //    if (selectElementLatestVersion?.InnerText.Trim() == "0.19.0 (latest)")
        //    {
        //        //Add message to queue and make it visible after a specific time.
        //        var cqm = new CloudQueueMessage("This message will appear on the output queue after X minutes");
        //        await outputQueue.AddMessageAsync(cqm, null, TimeSpan.FromMinutes(double.Parse(Environment.GetEnvironmentVariable("HideNewMessagesForInMinutes"))), null, null);
        //        return;
        //    }
        //}
    }
}
