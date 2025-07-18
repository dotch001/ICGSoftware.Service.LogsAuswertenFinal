using Azure.Identity;
using ICGSoftware.GetAppSettings;
using ICGSoftware.LogHandeling;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;
using Microsoft.Identity.Client;

namespace ICGSoftware.MSGraphEmailHandeling
{
    public class MSGraphEmail(AppSettingsClassConf confidential, Logging Logging)
    {
        private readonly Logging _log = Logging;

        public async Task SendMailWithAttachment(string subject, string text, string attachmentPath)
        {
            await Authentication();

            try
            {
                var jsonContentBytes = await File.ReadAllBytesAsync(Path.Combine(attachmentPath, "ErrorListe.json"));
                var jsonFileName = Path.GetFileName(Path.Combine(attachmentPath, "ErrorListe.json"));

                var scopes = new[] { "https://graph.microsoft.com/.default" };
                var options = new ClientSecretCredentialOptions
                {
                    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
                };

                var clientSecretCredential = new ClientSecretCredential(
                    confidential.TenantId,
                    confidential.ClientId,
                    confidential.ClientSecret,
                    options);

                var graphClient = new GraphServiceClient(clientSecretCredential, scopes);

                for (int i = 0; i < confidential.recipientEmails.Length; i++)
                {
                    var attachment = new FileAttachment
                    {
                        OdataType = "#microsoft.graph.fileAttachment",
                        Name = jsonFileName,
                        ContentBytes = jsonContentBytes,
                        ContentType = "application/json"
                    };

                    var message = new Message
                    {
                        Subject = subject,
                        Body = new ItemBody
                        {
                            ContentType = BodyType.Html,
                            Content = text
                        },
                        ToRecipients = new List<Recipient>
            {
                new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = confidential.recipientEmails[i]
                    }
                }
            },
                        Attachments = new List<Microsoft.Graph.Models.Attachment> { attachment }
                    };

                    var sendMailBody = new SendMailPostRequestBody
                    {
                        Message = message,
                        SaveToSentItems = true
                    };

                    await graphClient.Users[confidential.senderEmail]
                        .SendMail
                        .PostAsync(sendMailBody);
                }
                _log.log("Info", "Email sent successfully");
                return;
            }
            catch (Exception ex)
            {
                _log.log("Error", ex.ToString());
                return;
            }
        }

        public async Task Authentication()
        {
            try
            {

                var confidentialClient = ConfidentialClientApplicationBuilder
                    .Create(confidential.ClientId)
                    .WithClientSecret(confidential.ClientSecret)
                    .WithAuthority($"https://login.microsoftonline.com/{confidential.TenantId}")
                    .Build();

                var scopes = new[] { "https://graph.microsoft.com/.default" };
                var authResult = await confidentialClient.AcquireTokenForClient(scopes).ExecuteAsync();
                var accessToken = authResult.AccessToken;

                return;


            }
            catch (Exception ex)
            {
                _log.log("Error from Authentication:", $"Error sending email: {ex.Message}");
            }
        }
    }
}
