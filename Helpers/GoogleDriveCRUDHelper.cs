using Google.Apis.Drive.v3;
using Google.Apis.Services;
using GoogleDriveUpdateService.Interfaces;
using Microsoft.Extensions.Configuration;
using GFile = Google.Apis.Drive.v3.Data.File;


namespace GoogleDriveUpdateService.Helpers
{
    public class GoogleDriveCRUDHelper : IGoogleDriveCRUDHelper
    {
        private readonly IConfiguration _config;
        private readonly ICredentialHelper _credentialHelper;
        private readonly string _autoUploadsFolderId;
        private DriveService _service;

        public GoogleDriveCRUDHelper(IConfiguration config, ICredentialHelper credentialHelper, IDriveServiceHelper driveServiceHelper)
        {
            _config = config;
            _credentialHelper = credentialHelper;

            _autoUploadsFolderId = config.GetSection("Google").GetSection("Drive").GetSection("AutoUploadsFolderId").Value!;

            _service = driveServiceHelper.CreateGoogleDriveService();
        }

        public void UploadFileToDrive(string filePath, string parentId)
        {
            var fileName = Path.GetFileName(filePath);

            // File upload parameters
            var fileMetadata = new GFile()
            {
                Name = fileName, // Name of the file in Google Drive
                Parents = new[] { parentId }
            };

            FilesResource.CreateMediaUpload request;

            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                request = _service.Files.Create(fileMetadata, stream, "application/octet-stream");
                request.Fields = "id";
                request.Upload();
            }

            Console.WriteLine($"Upload Successful - Name: {fileName}, ID: {request.ResponseBody.Id}");
        }

        public void RemoveExcessCopies()
        {
            var request = _service.Files.List();
            request.Q = $"trashed=false and '{_autoUploadsFolderId}' in parents";
            request.Fields = "files(id, name, modifiedTime)";

            var result = request.Execute();
            var children = result.Files;

            Console.WriteLine("Children in AutoUploads:");
            var fileCountDict = new Dictionary<string, int>();
            foreach (var child in children)
            {
                Console.WriteLine($"{child.Name}: {child.ModifiedTimeDateTimeOffset}");

                if (!fileCountDict.ContainsKey(child.Name))
                {
                    Console.WriteLine($"Adding new fileName to dictionary {child.Name}...");
                    fileCountDict.Add(child.Name, 1);
                }
                else
                {
                    fileCountDict[child.Name]++;
                }

                Console.WriteLine($"{child.Name}: {fileCountDict[child.Name]}");

                if (fileCountDict[child.Name] > 3)
                {
                    var oldestCopy = children
                        .Where(f => f.Name == child.Name)
                        .OrderBy(f => f.ModifiedTimeDateTimeOffset)
                        .FirstOrDefault();

                    if (oldestCopy != null)
                    {
                        Console.WriteLine($"Deleting oldest copy of {oldestCopy.Name} from {oldestCopy.ModifiedTimeDateTimeOffset}");
                        _service.Files.Delete(oldestCopy.Id).Execute();
                    }
                }
            }
        }
    }
}
