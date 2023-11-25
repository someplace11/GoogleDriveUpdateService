using Google.Apis.Drive.v3;
using Google.Apis.Services;
using GoogleDriveUpdateService.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleDriveUpdateService.Helpers
{
    public class DriveServiceHelper : IDriveServiceHelper
    {
        private readonly IConfiguration _config;
        private readonly ICredentialHelper _credentialHelper;

        public DriveServiceHelper(IConfiguration config, ICredentialHelper credentialHelper)
        {
            _config = config;
            _credentialHelper = credentialHelper;
        }

        public DriveService CreateGoogleDriveService()
        {
            // Create OAuth credentials using a client ID and client secret
            var credential = _credentialHelper.SetupUserCredential();

            // Create Drive API service
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = _config.GetSection("Google").GetSection("AppName").Value!
            });

            return service;
        }
    }
}
