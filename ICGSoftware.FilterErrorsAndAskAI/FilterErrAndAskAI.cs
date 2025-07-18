using ICGSoftware.CountAndSortLogs;
using ICGSoftware.GetAppSettings;
using ICGSoftware.LogHandeling;
using ICGSoftware.MSGraphEmailHandeling;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ICGSoftware.FilterErrorsAndAskAI
{
    public class FilterErrAndAskAI(IOptions<AppSettingsClassDev> settings, IOptions<AppSettingsClassConf> confidential, Logging Logging)
    {
        private readonly AppSettingsClassDev settings = settings.Value;
        private readonly AppSettingsClassConf confidential = confidential.Value;
        private readonly Logging _log = Logging;

        public async Task FilterErrors()
        {
            string fileName;
            string filesDate = "";
            string previousFilesDate = filesDate;

            string outputFolderPath = "";
            string outputFile = "";

            DateTime now = DateTime.Now;
            string todaysDate = DateTime.Today.ToString("yyyyMMdd");

            int amountOfFiles = 0;
            int currentFileCount = 0;

            int overridePrevention = 1;
            int madeNewFilesCount = 0;

            bool found = false;
            bool isBetween = false;

            string allResponses = "";

            try
            {
                foreach (var inputPath in settings.inputFolderPaths)
                {
                    if (settings.outputFolderPath == "") { outputFolderPath = inputPath; } else { outputFolderPath = settings.outputFolderPath; }


                    while (Directory.Exists(outputFolderPath + "\\ExtractionLogs" + overridePrevention)) { overridePrevention++; }
                    outputFolderPath = outputFolderPath + "\\ExtractionLogs" + overridePrevention;
                    Directory.CreateDirectory(outputFolderPath);

                    outputFile = outputFolderPath + "\\ExtractionLogs" + filesDate + "_" + madeNewFilesCount + ".txt";


                    foreach (string file in Directory.GetFiles(inputPath))
                    {
                        previousFilesDate = filesDate;

                        fileName = Path.GetFileName(file);
                        filesDate = fileName.Substring(fileName.IndexOf("Api") + 3, 8);

                        amountOfFiles = Directory.GetFiles(inputPath).Length;
                        currentFileCount++;

                        if (filesDate == todaysDate) { inform(currentFileCount + "/" + amountOfFiles + " done (File was made today => file skipped)"); continue; }



                        List<string> extractedLines = new List<string>();

                        if (filesDate != previousFilesDate) { extractedLines.Clear(); madeNewFilesCount = 0; previousFilesDate = filesDate; }

                        outputFile = outputFolderPath + "\\ExtractionLogs" + filesDate + "_" + madeNewFilesCount + ".txt";


                        using (StreamReader reader = new StreamReader(file))
                        {
                            string? lineread;
                            while ((lineread = reader.ReadLine()) != null)
                            {
                                if (lineread.Contains(settings.startTerm, StringComparison.OrdinalIgnoreCase))
                                {
                                    found = true;
                                }

                            }
                        }



                        if (!found) { inform(currentFileCount + "/" + amountOfFiles + " done (No error found in file)"); continue; }

                        foreach (string line in File.ReadLines(file))
                        {

                            if (line.Contains(previousFilesDate.Substring(0, 4)) && isBetween)
                            {
                                isBetween = false;
                                File.WriteAllLines(outputFile, extractedLines);


                                if (File.Exists(outputFile))
                                {
                                    FileInfo fileInfo = new FileInfo(outputFile);
                                    long fileSize = fileInfo.Length;
                                    inform(outputFile + "size in KB: " + fileSize / 1024);

                                    if (fileSize / 1024 >= settings.maxSizeInKB - 20)
                                    {
                                        madeNewFilesCount++;
                                        outputFile = Path.Combine(outputFolderPath + "\\ExtractionLogs" + filesDate + "_" + madeNewFilesCount + ".txt");
                                        inform("Making new output file: " + outputFile);
                                        previousFilesDate = filesDate;
                                        extractedLines.Clear();
                                    }
                                }
                            }

                            if (line.Contains(settings.startTerm)) { isBetween = true; }

                            if (isBetween) { extractedLines.Add(line); }
                        }
                        inform(currentFileCount + "/" + amountOfFiles + " done");
                    }

                    if (settings.AskAI)
                    {
                        _log.log("Info", "Asking AI");
                        foreach (var fileInOutput in Directory.GetFiles(outputFolderPath))
                        {
                            string response = await AskAndGetResponse(fileInOutput);
                            inform(response);
                            allResponses = allResponses + $"<b> <br /><br />----------------------------------------------{fileInOutput}----------------------------------------------<br /><br /> </b>" + response;
                        }
                    }

                    var CountAndSortErrorsInstance = new CountAndSortErrors(settings, confidential, _log);
                    await CountAndSortErrorsInstance.Process(outputFolderPath);
                }
                var MSGraphEmailInstance = new MSGraphEmail(confidential, _log);
                await MSGraphEmailInstance.SendMailWithAttachment(confidential.subject, allResponses, outputFolderPath);
            }
            catch (Exception ex)
            {
                _log.log("Error", ex.ToString());
            }
        }



        public async Task<string> AskAndGetResponse(string fileInOutput)
        {
            string fileAsText;
            using (StreamReader reader = new StreamReader(fileInOutput)) { fileAsText = reader.ReadToEnd(); }

            string model = settings.models[settings.chosenModel];

            string response = await AskQuestionAboutFile(fileAsText, model);

            return response;
        }



        public async Task<string> AskQuestionAboutFile(string FileAsText, string model)
        {

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {confidential.ApiKey}");

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
            new { role = "user", content = settings.Question + FileAsText }
            }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(settings.apiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                return "[API error]";
            }

            var responseString = await response.Content.ReadAsStringAsync();

            var json = JsonNode.Parse(responseString);
            var messageContent = json?["choices"]?[0]?["message"]?["content"]?.ToString();

            var cleanedContent = Regex.Replace(messageContent ?? "", @"\n{3,}", "\n\n").Trim();

            return string.IsNullOrWhiteSpace(cleanedContent) ? "Raw Response: " + responseString : cleanedContent;

        }

        public void inform(string message) { if (settings.inform) { Console.WriteLine(message); } }
    }
}
