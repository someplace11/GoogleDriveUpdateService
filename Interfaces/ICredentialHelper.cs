using Google.Apis.Auth.OAuth2;

namespace GoogleDriveUpdateService.Interfaces
{
    public interface ICredentialHelper
    {
        UserCredential SetupUserCredential();
    }
}
