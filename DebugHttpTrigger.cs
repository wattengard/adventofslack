using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Bouvet.AdventOfCode
{
    public static class DebugHttpTrigger
    {
        private static readonly string COOKIE = System.Environment.GetEnvironmentVariable("ADVCODE_COOKIE", EnvironmentVariableTarget.Process);
        private static readonly string SLACK_HOOK_URL = System.Environment.GetEnvironmentVariable("SLACK_HOOK", EnvironmentVariableTarget.Process);

        [FunctionName("DebugHttpTrigger")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            var responseMessage = $"Cookie: {COOKIE}\nHOOK: {SLACK_HOOK_URL}";

            return new OkObjectResult(responseMessage);
        }
    }
}
