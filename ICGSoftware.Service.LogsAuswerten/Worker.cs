using ICGSoftware.FilterErrorsAndAskAI;
using ICGSoftware.GetAppSettings;
using ICGSoftware.LogHandeling;
using Microsoft.Extensions.Options;


namespace ICGSoftware.Service
{
    public class Worker : BackgroundService
    {
        private readonly AppSettingsClassDev _appSettingsClassDev;
        private readonly AppSettingsClassConf _appSettingsClassConf;
        private readonly FilterErrAndAskAI _FilterErrAndAskAI;
        private readonly Logging _log;



        public Worker(IOptions<AppSettingsClassDev> appSettingsClassDev, IOptions<AppSettingsClassConf> appSettingsClassConf, FilterErrAndAskAI FilterAndAsk, Logging log)
        {

            _appSettingsClassDev = appSettingsClassDev.Value;
            _appSettingsClassConf = appSettingsClassConf.Value;
            _FilterErrAndAskAI = FilterAndAsk;
            _log = log;



        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {

                    _log.log("Info", "Worker started");
                    await _FilterErrAndAskAI.FilterErrors();
                    await Task.Delay(_appSettingsClassDev.IntervallInSeconds * 1000, stoppingToken);
                    _log.log("Info", "Worker finished");


                }
                catch (Exception ex)
                {
                    _log.log("Error", ex + " Worker crashed");
                    break;
                }
            }
        }
    }
}