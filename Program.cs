using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using GFile = Google.Apis.Drive.v3.Data.File;
using Google.Apis.Services;
using GoogleDriveUpdateService.Models;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace GoogleDriveUpdateService
{
    public class Program
    {
        private static TransferFile? _lastUpdateRefFile;
        private static string? _autoUploadsFolderId;

        static void Main(string[] args)
        {
            Console.WriteLine("Application starting...");
            var stopwatch = Stopwatch.StartNew();

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
            _autoUploadsFolderId = config.GetSection("Google").GetSection("Drive").GetSection("AutoUploadsFolderId").Value!;
            _lastUpdateRefFile = new TransferFile
            {
                Name = config.GetSection("FileInfo").GetSection("LastUpdateRefFile").GetSection("LastUpdateRefFileName").Value!,
                Path = config.GetSection("FileInfo").GetSection("LastUpdateRefFile").GetSection("LastUpdateRefFilePath").Value!
            };

            foreach (var fileDirectory in filesToUploadList)
            {
                TraverseDirectory(service, fileDirectory.Path, _autoUploadsFolderId);
            }

            // Get all direct children of !AutoUploads and if any of the names are duplicates
            // delete until only 3 left, starting from oldest
            GetAutoUploadChildren(service);

            stopwatch.Stop();
            Console.WriteLine($"Elapsed Time: {stopwatch.Elapsed}");
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
        }

        private static void GetAutoUploadChildren(DriveService service)
        {
            var request = service.Files.List();
            request.Q = $"trashed=false and '{_autoUploadsFolderId}' in parents";
            request.Fields = "files(id, name, modifiedTime)";

            var result = request.Execute();
            var children = result.Files;

            // 1) Get a list of all direct child of AutoUploads Names, Ids, UpdateTimes
            // 2) If >3 of any entry, delete the oldest
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

                    Console.WriteLine($"Deleting oldest copy of {oldestCopy.Name} from {oldestCopy.ModifiedTimeDateTimeOffset}");

                    //DeleteGoogleDriveFile(service, oldestCopy!.Id);
                    service.Files.Delete(oldestCopy.Id).Execute();
                }
            }
        }
    }
}

// TODO - TOP PRIORITY
// Clean up and split out into real project

// TODO - TOP PRIORITY
// Create new functionality to copy files from one directory into 
// another
// - Mostly for save files so a quick copy can be made and zipped
//   and copied to new parent directory to upload from, hopefully
//   without potentially dealing with file locks or screwing up
//   the file owners task

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

// TODO
// Create config file to replace appsettings.json

// TODO
// Create a Path helper class (if not already part of Path) in order
// to remove TransferFile class and just stick with using paths for
// for everything