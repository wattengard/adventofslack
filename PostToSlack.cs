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
        private static readonly string SLACK_HOOK_URL = System.Environment.GetEnvironmentVariable("SLACK_HOOK", EnvironmentVariableTarget.Process);
        private static readonly string LEADERBOARD_URL = System.Environment.GetEnvironmentVariable("LEADERBOARD_URL", EnvironmentVariableTarget.Process);

        private static readonly Dictionary<int, string> PARROTS = new Dictionary<int, string> {
          {1, ":exceptionally_fast_parrot:"},
          {2, ":ultra_fast_parrot:"},
          {3, ":fast_parrot:"},
          {4, ":parrot_party:"},
          {5, ":bored_parrot:"}
        };


        [FunctionName("PostToSlack")]
        public async Task RunAsync([TimerTrigger("0 5 0 * 12,1 *")] TimerInfo myTimer, ILogger log)
        {
            if (COOKIE == null || SLACK_HOOK_URL == null || LEADERBOARD_URL == null)
            {
                log.LogCritical("One or more of the required environment variables were empty!");
                return;
            }

            var reportingForDay = DateTime.Now.Month == 12 && DateTime.Now.Day == 1
                ? 1
                : DateTime.Now.Day - 1;

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, new Uri(LEADERBOARD_URL));
            httpRequestMessage.Headers.Add("Cookie", "session=" + COOKIE);
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

            var lastDaysFastest = root.Members
                .Where(q => q.Value.CompletionDayLevel.ContainsKey(reportingForDay))
                .ToDictionary(q => q.Value.Name, q => q.Value.CompletionDayLevel[reportingForDay])
                .Where(q => q.Value.Count == 2)
                .ToDictionary(q => q.Key, q => q.Value[2])
                .OrderBy(q => q.Value.StarTime)
                .Take(5)
                .Select((q, idx) => $"    #{idx + 1} {q.Key}, kl. {q.Value.StarTime.ToLocalTime().ToString("HH:mm:ss")} {PARROTS[idx + 1]}");

            var moreThanOneStar = root.Members.Values.Count(q => q.Stars > 0);
            var threeRandos = root.Members
                                .Values.Where(q => q.Stars > 0 && !string.IsNullOrWhiteSpace(q.Name))
                                .OrderBy(q => Guid.NewGuid())
                                .Take(3)
                                .Select(q => new { Name = q.Name, Stars = q.Stars })
                                .ToList();

            stringBuilder.AppendLine("_Stjerneskudd betyr 5 stjerner, hver dag gir mulighet for 2 stjerner. Totalt kan man få 50 stjerner._");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("De fem raskeste til to stjerner i går var:");
            stringBuilder.AppendLine(string.Join("\n", lastDaysFastest));
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"Det er totalt {moreThanOneStar} deltagere med stjerner. Blant annet *{threeRandos[0].Name}* med {threeRandos[0].Stars} stjerner, *{threeRandos[1].Name}* med {threeRandos[1].Stars} stjerner og *{threeRandos[2].Name}* med {threeRandos[2].Stars} stjerner...");

            var stringContent = new StringContent(JsonConvert.SerializeObject((object)new SlackPostMessage()
            {
                Text = stringBuilder.ToString()
            }));
            var httpResponseMessage2 = await new HttpClient().PostAsync(SLACK_HOOK_URL, (HttpContent)stringContent);
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
        public Dictionary<int, Dictionary<int, StarInfoDetails>> CompletionDayLevel { get; set; }

        [JsonProperty("stars")]
        public int Stars { get; set; }
    }

    public class StarInfoDetails
    {
        [JsonProperty("star_index")]
        public int StarIndex { get; set; }
        [JsonProperty("get_star_ts")]
        public int StarTimestamp { get; set; }
        [JsonIgnore]
        public DateTimeOffset StarTime => DateTimeOffset.FromUnixTimeSeconds(StarTimestamp);
    }

    public class SlackPostMessage
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }
}
