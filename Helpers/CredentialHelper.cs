using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using GoogleDriveUpdateService.Interfaces;
using Microsoft.Extensions.Configuration;

namespace GoogleDriveUpdateService.Helpers
{
    public class CredentialHelper : ICredentialHelper
    {
        private readonly IConfiguration _config;

        public CredentialHelper(IConfiguration config)
        {
            _config = config;
        }

        public UserCredential SetupUserCredential()
        {
            // OAuth credentials
            var clientId = _config.GetSection("Google").GetSection("Credentials").GetSection("ClientId").Value!;
            var clientSecret = _config.GetSection("Google").GetSection("Credentials").GetSection("ClientSecret").Value!;

            UserCredential credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                new[] { DriveService.Scope.DriveFile },
                "user",
                CancellationToken.None
            ).Result;

            return credential;
        }
    }
}
