using ICGSoftware.FirebirdHandeling;
using ICGSoftware.GetAppSettings;
using ICGSoftware.LogHandeling;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace ICGSoftware.CountAndSortLogs
{
    public class CountAndSortErrors(AppSettingsClassDev settings, AppSettingsClassConf confidential, Logging Logging)
    {
        private readonly Logging _log = Logging;

        Dictionary<string, List<string>> categoryTimestamps = new Dictionary<string, List<string>>();
        Dictionary<string, int> categoryCounts = new Dictionary<string, int>();

        public async Task Process(string outputFolderPath)
        {
            string[] fileNames = Directory.GetFiles(outputFolderPath);

            Console.WriteLine("Processing files in: " + outputFolderPath);

            foreach (var file in fileNames)
            {
                foreach (string line in File.ReadLines(file))
                {
                    await GetError(line);
                }
            }

            await WriteToFile(outputFolderPath);

            var FirebirdDBHandelingInstance = new FirebirdDBHandeling(settings, confidential, _log);
            await FirebirdDBHandelingInstance.Process(Path.Combine(outputFolderPath, "ErrorListe.json"));
        }


        public Task GetError(string line)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.Contains("[ERR]"))
                return Task.CompletedTask;

            var timestampMatch = Regex.Match(line, @"^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} [+\-]\d{2}:\d{2}");
            string timestamp = timestampMatch.Success ? timestampMatch.Value : "";

            string cleanedLine = Regex.Replace(line, "\".*?\"|'.*?'", "");

            int errIndex = cleanedLine.IndexOf("[ERR]");
            if (errIndex < 0)
                return Task.CompletedTask;

            string rawCategory = cleanedLine.Substring(errIndex);

            string noParams = Regex.Replace(rawCategory, @"\[Parameters=.*?\]", "[Parameters]");
            string normalized = Regex.Replace(noParams, @"\(\d+ms\)", "(ms)").Trim();

            int colonIndex = normalized.IndexOf(":");
            if (colonIndex > 0)
                normalized = normalized.Substring(0, colonIndex + 1);

            CategoriseAndCount(normalized, timestamp);



            return Task.CompletedTask;
        }




        public void CategoriseAndCount(string category, string timestamp)
        {
            if (string.IsNullOrWhiteSpace(category))
                category = "(no category)";

            if (!categoryCounts.ContainsKey(category))
            {
                categoryCounts[category] = 0;
                categoryTimestamps[category] = new List<string>();
            }

            categoryCounts[category]++;
            categoryTimestamps[category].Add(timestamp);
        }

        private async Task WriteToFile(string outputFolder)
        {
            string outputFile = Path.Combine(outputFolder, "ErrorListe.json");

            var allData = new JObject();

            foreach (var category in categoryCounts.Keys)
            {
                var inner = new JObject
                {
                    ["Aufgetreten"] = categoryCounts[category] + " mal",
                    ["Timestamps"] = JToken.FromObject(categoryTimestamps[category])
                };

                allData[category] = inner;
            }

            string jsonString = JsonConvert.SerializeObject(allData, Newtonsoft.Json.Formatting.Indented);

            await File.WriteAllTextAsync(outputFile, jsonString);
        }
    }
}
