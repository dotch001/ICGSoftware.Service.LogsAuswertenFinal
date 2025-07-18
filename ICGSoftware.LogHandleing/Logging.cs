using ICGSoftware.GetAppSettings;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Core;

namespace ICGSoftware.LogHandeling
{
    public class Logging(IOptions<AppSettingsClassDev> settings)
    {
        private readonly AppSettingsClassDev settings = settings.Value;


        public void log(string TypeOfMessage, string message)
        {
            string outputFolder;
            string outputFile;
            bool isLoggerConfigured = Log.Logger != Logger.None; ;
            int i = 0;

            if (settings.outputFolderPathForLogs == "")
            {
                if (settings.outputFolderPath == "")
                {
                    outputFolder = settings.inputFolderPaths[0] + "\\Logs";
                }
                else
                {
                    outputFolder = settings.outputFolderPath + "\\Logs";
                }
            }
            else
            {
                outputFolder = settings.outputFolderPathForLogs;
            }



            if (!Directory.Exists(outputFolder)) { Directory.CreateDirectory(outputFolder); }

            outputFile = outputFolder + settings.logFileName + i + ".log";


            while (File.Exists(outputFile) && new FileInfo(outputFile).Length / 1024 >= 300)
            {
                i++;
                outputFile = outputFolder + settings.logFileName + i + ".log";
            }
            if (!isLoggerConfigured)
            {
                Log.Logger = new LoggerConfiguration().WriteTo.File(outputFile).CreateLogger();
            }


            if (TypeOfMessage == "Info")
            {
                Log.Information(message);
            }
            else if (TypeOfMessage == "Warning")
            {
                Log.Warning(message);
            }
            else if (TypeOfMessage == "Error")
            {
                Log.Error(message);
            }
            else if (TypeOfMessage == "Debug")
            {
                Log.Debug(message);
            }
        }
    }
}
