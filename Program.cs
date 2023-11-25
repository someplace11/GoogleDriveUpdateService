using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using GoogleDriveUpdateService.Models;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace GoogleDriveUpdateService
{
    public class Program
    {
        private static TransferFile? _lastUpdateRefFile;

        static void Main(string[] args)
        {
            Console.WriteLine("Application starting...");

            // Setup
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Create OAuth credentials using a client ID and client secret
            var credential = SetupUserCredential(config);

            // Create Drive API service
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = config.GetSection("Google").GetSection("AppName").Value!
            });

            // File names and paths you want to upload, read from appsettings
            var filesToUploadList = new List<TransferFile>();
            foreach (var child in config.GetSection("FileInfo").GetSection("FileList").GetChildren())
            {
                var transferFileToAdd = new TransferFile
                {
                    Name = child.GetSection("FileName").Value!,
                    Path = child.GetSection("FilePath").Value!
                };

                filesToUploadList.Add(transferFileToAdd);
                Console.WriteLine($"{transferFileToAdd.Name} added to updload list");
            }

            // LastUpdatedTime.txt - to get all files' last updated date/time
            _lastUpdateRefFile = new TransferFile
            {
                Name = config.GetSection("FileInfo").GetSection("LastUpdateRefFile").GetSection("LastUpdateRefFileName").Value!,
                Path = config.GetSection("FileInfo").GetSection("LastUpdateRefFile").GetSection("LastUpdateRefFilePath").Value!
            };

            var autoUploadsFolderId = config.GetSection("Google").GetSection("Drive").GetSection("AutoUploadsFolderId").Value!;
            foreach (var fileDirectory in filesToUploadList)
            {
                TraverseDirectory(service, fileDirectory.Path, autoUploadsFolderId);
            }
        }

        private static UserCredential SetupUserCredential(IConfigurationRoot config)
        {
            // OAuth credentials
            var clientId = config.GetSection("Google").GetSection("Credentials").GetSection("ClientId").Value!;
            var clientSecret = config.GetSection("Google").GetSection("Credentials").GetSection("ClientSecret").Value!;

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

        private static void TraverseDirectory(DriveService service, string directoryPath, string parentId)
        {
            // Get all files in the current directory
            var currentDirectoryName = Path.GetFileName(directoryPath);

            // TODO
            // Make this less awful
            var files = new string[0];
            try
            {
                files = Directory.GetFiles(directoryPath);
            }
            catch (IOException ex)
            {
                Console.WriteLine("Writing single file at root...");
                UploadFileToDrive(service, directoryPath, parentId);

                return;
            }

            Console.WriteLine($"Current Directory: {currentDirectoryName}");

            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = currentDirectoryName,
                Parents = new[] { parentId },
                MimeType = "application/vnd.google-apps.folder"
            };

            var createDirectoryRequest = service.Files.Create(fileMetadata);
            createDirectoryRequest.Fields = "id";
            var directory = createDirectoryRequest.Execute();

            foreach (var filePath in files)
            {
                Console.WriteLine($"File: {filePath}");

                // Upload File
                UploadFileToDrive(service, filePath, directory.Id);
            }

            // Get all subdirectories in the current directory
            string[] subdirectories = Directory.GetDirectories(directoryPath);
            foreach (var subdirectory in subdirectories)
            {
                var subdirectoryName = Path.GetFileName(subdirectory);
                Console.WriteLine($"Subdirectory: {subdirectoryName}");

                // Recursive call to traverse subdirectories
                TraverseDirectory(service, subdirectory, directory.Id);
            }
        }

        private static void UploadFileToDrive(DriveService service, string filePath, string parentId)
        {
            var fileName = Path.GetFileName(filePath);

            // File upload parameters
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = fileName, // Name of the file in Google Drive
                Parents =  new[] { parentId }
            };

            FilesResource.CreateMediaUpload request;

            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                request = service.Files.Create(fileMetadata, stream, "application/octet-stream");
                request.Fields = "id";
                request.Upload();
            }

            Console.WriteLine($"Upload Successful - Name: {fileName}, ID: {request.ResponseBody.Id}");

            Console.WriteLine("Updating uploaded files LastUpdateTime...");
            File.AppendAllLines(_lastUpdateRefFile.Path, new List<string> { $"{fileName}, {File.GetLastWriteTime(filePath)}" });
        }
    }
}

// TODO
// Target Ubuntu for deployment

// TODO
// Setup scheduler
// -
// Maybe try to see if it's possible to have a background task that
// goes nearly 100% down while not active and use an internal timer
// to "wake the service". Only really useful if the service is nearly
// completely asleep when not in use since this would be a dumb thing
// to waste resources on running in background. Just use a scheduler.

// TODO
// Fix 'append new line with dictionary<fileName, lastUpdated>' logic to
// override an entry with the same key value to only update lastUpdated

// TODO
// Since (at least game saves) files/directory will be named the same,
// get a list of all files/documents/directories and if there are more
// than n number of files with the same name, delete them starting from
// oldest until number of files with the same name == n
// -
// TLDR: Clean up multiple copies of Google Drive saves up to threshold