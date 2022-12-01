using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Bouvet.AdventOfCode
{
    public class PostToSlack
    {
        private static readonly string COOKIE = System.Environment.GetEnvironmentVariable("ADVCODE_COOKIE", EnvironmentVariableTarget.Process);
        private static readonly string SLACK_HOOK_URL = System.Environment.GetEnvironmentVariable("SLACK_HOOK",  EnvironmentVariableTarget.Process);
        [FunctionName("PostToSlack")]
        public async Task RunAsync([TimerTrigger("0 5 0 1-26 12 *")] TimerInfo myTimer, ILogger log)
        {
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri("https://adventofcode.com/2021/leaderboard/private/view/220070.json"));
            httpRequestMessage.Headers.Add("Cookie", "session=" + PostToSlack.COOKIE);
            log.LogInformation("C# Timer trigger function executed at: {0}", DateTime.Now);
            var httpResponseMessage1 = await new HttpClient((HttpMessageHandler)new HttpClientHandler()
            {
                UseCookies = false
            }).SendAsync(httpRequestMessage);
            log.LogInformation("Got response: {0}", httpResponseMessage1.StatusCode);
            var root = JsonConvert.DeserializeObject<Root>(await httpResponseMessage1.Content.ReadAsStringAsync());
            string str1 = DateTime.Now.ToString("HH:mm");
            var dateTime = DateTime.Now.AddDays(-1.0);
            var stringBuilder = new StringBuilder();
            string str2 = string.Format("God natt! :crescent_moon: Klokken er {0} og her er topp 10 listen etter {1}. desember!", (object)str1, (object)dateTime.Day);
            stringBuilder.AppendLine(str2);
            var list = root.Members.Values.OrderByDescending(q => q.LocalScore).Take<Member>(10).ToList<Member>();
            for (int index = 0; index < list.Count; ++index)
            {
                int count1 = list[index].Stars < 5 ? 0 : list[index].Stars / 5;
                int count2 = list[index].Stars % 5;
                string str3 = Enumerable.Range(0, count1).Aggregate<int, string>("", (Func<string, int, string>)((s, i) => s + ":stars:")) + Enumerable.Range(0, count2).Aggregate<int, string>("", (Func<string, int, string>)((s, i) => s + ":star:"));
                if (list[index].Stars == 50)
                    str3 = ":deal_with_it_parrot:";
                stringBuilder.AppendLine(string.Format("    #{0}: {1} ({2}) {3}", (object)(index + 1), (object)(list[index].Name ?? "Anonym :techy_pingvin:"), (object)list[index].LocalScore, (object)str3));
            }
            stringBuilder.AppendLine("_Stjerneskudd betyr 5 stjerner, hver dag gir mulighet for 2 stjerner. Totalt kan man få 50 stjerner._");
            StringContent stringContent = new StringContent(JsonConvert.SerializeObject((object)new SlackPostMessage()
            {
                Text = stringBuilder.ToString()
            }));
            HttpResponseMessage httpResponseMessage2 = await new HttpClient().PostAsync(PostToSlack.SLACK_HOOK_URL, (HttpContent)stringContent);
        }
    }
    public class Root
    {
        [JsonProperty("members")]
        public Dictionary<int, Member> Members { get; set; }

        [JsonProperty("event")]
        public string Event { get; set; }

        [JsonProperty("owner_id")]
        public string OwnerId { get; set; }
    }

    public class Member
    {
        [JsonProperty("global_score")]
        public int GlobalScore { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("last_star_ts")]
        public int LastStarTimestamp { get; set; }

        [JsonProperty("local_score")]
        public int LocalScore { get; set; }

        [JsonProperty("completion_day_level")]
        public object CompletionDayLevel { get; set; }

        [JsonProperty("stars")]
        public int Stars { get; set; }
    }

    public class SlackPostMessage
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }
}