using Google.Apis.Drive.v3;
using Google.Apis.Services;
using GoogleDriveUpdateService.Interfaces;
using GoogleDriveUpdateService.Models;
using Microsoft.Extensions.Configuration;

namespace GoogleDriveUpdateService
{
    public class Service : IService
    {
        private readonly IConfiguration _config;
        private readonly ILocalFileHelper _localFileHelper;
        private readonly IGoogleDriveCRUDHelper _gdHelper;

        public Service (IConfiguration config, ILocalFileHelper localFileHelper, IGoogleDriveCRUDHelper gdHelper)
        {
            _config = config;
            _localFileHelper = localFileHelper;
            _gdHelper = gdHelper;
        }

        public void MainStartup()
        {
            // File names and paths you want to upload, read from appsettings
            var filesToUploadList = new List<TransferFile>();
            foreach (var child in _config.GetSection("FileInfo").GetSection("FileList").GetChildren())
            {
                var transferFileToAdd = new TransferFile
                {
                    Name = child.GetSection("FileName").Value!,
                    Path = child.GetSection("FilePath").Value!
                };

                filesToUploadList.Add(transferFileToAdd);
                Console.WriteLine($"{transferFileToAdd.Name} added to updload list");
            }

            var autoUploadsFolderId = _config.GetSection("Google").GetSection("Drive").GetSection("AutoUploadsFolderId").Value!;

            foreach (var fileDirectory in filesToUploadList)
            {
                _localFileHelper.TraverseDirectory(fileDirectory.Path, autoUploadsFolderId);
            }

            // Get all direct children of !AutoUploads and if any of the names are duplicates
            // delete until only 3 left, starting from oldest
            _gdHelper.RemoveExcessCopies();
        }
    }
}
