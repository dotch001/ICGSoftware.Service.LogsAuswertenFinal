namespace ICGSoftware.GetAppSettings
{
    public class AppSettingsClassDev
    {
        public required string outputFolderForDB { get; set; }
        public required string DBUser { get; set; }
        public required string Question { get; set; }
        public required string startTerm { get; set; }
        public required string[] inputFolderPaths { get; set; }
        public required string outputFolderPath { get; set; }
        public required string outputFolderPathForLogs { get; set; }
        public required string outputFolderPathForDB { get; set; }
        public required bool inform { get; set; }
        public required bool AskAI { get; set; }
        public required string[] models { get; set; }
        public required int chosenModel { get; set; }
        public required int maxSizeInKB { get; set; }
        public required string logFileName { get; set; }
        public required string apiUrl { get; set; }
        public required int IntervallInSeconds { get; set; } = 60 * 60 * 24; // Default to 24 hours
        public required string DBDatabase { get; set; } = "ErrorsKategorisierenDatabase.fdb"; // Default database name       
        public required string DBDataSource { get; set; } = "localhost"; // Default to localhost
        public required int DBPort { get; set; } = 3050; // Default Firebird port
    }
    public class AppSettingsClassConf
    {
        public required string subject { get; set; }
        public required string senderEmail { get; set; }
        public required string[] recipientEmails { get; set; }
        public required string DBPassword { get; set; }
        public required string ApiKey { get; set; }
        public required string ClientId { get; set; }
        public required string ClientSecret { get; set; }
        public required string TenantId { get; set; }
    }
}
