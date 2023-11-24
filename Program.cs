﻿using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using GoogleDriveUpdateService.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GoogleDriveUpdateService
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Application starting...");

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // OAuth credentials
            var clientId = config.GetSection("Google").GetSection("Credentials").GetSection("ClientId").Value;
            var clientSecret = config.GetSection("Google").GetSection("Credentials").GetSection("ClientSecret").Value;

            // File names and paths you want to upload, read from appsettings
            var filesUpdatedAtList = new List<FileDto>();
            foreach (var child in config.GetSection("FileInfo").GetSection("FileList").GetChildren())
            {
                var fileDtoToAdd = new FileDto
                {
                    FileName = child.GetSection("FileName").Value!,
                    Path = child.GetSection($"FilePath").Value!
                };

                Console.WriteLine($"Adding file: {fileDtoToAdd.FileName}");
                filesUpdatedAtList.Add(fileDtoToAdd);
            }

            // LastUpdatedTime.txt - to get all files' last updated date/time
            var updatesRefFile = new FileDto
            {
                FileName = config.GetSection("FileInfo").GetSection("RefFile").GetSection("RefFileName").Value!,
                Path = config.GetSection("FileInfo").GetSection("RefFile").GetSection("RefFilePath").Value!
            };

            //Check to see if the file has been updated since the last update
            foreach (var fileDto in filesUpdatedAtList)
            {
                if (File.ReadAllText(updatesRefFile.Path).Contains($"{fileDto.FileName}, {File.GetLastWriteTime(fileDto.Path)}"))
                {
                    Console.WriteLine($"File - {fileDto.FileName} - has not been updated since previous upload...");
                    continue;
                }

                // Create OAuth credentials using a client ID and client secret
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

                // Create Drive API service
                var service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = config.GetSection("Google").GetSection("AppName").Value!
                });

                // File upload parameters
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = fileDto.FileName, // Name of the file in Google Drive
                };
                FilesResource.CreateMediaUpload request;

                using (var stream = new FileStream(fileDto.Path, FileMode.Open))
                {
                    request = service.Files.Create(fileMetadata, stream, "application/octet-stream");
                    request.Fields = "id";
                    request.Upload();
                }

                Console.WriteLine("Updating uploaded files' LastUpdateTime.txt...");
                File.AppendAllLines(updatesRefFile.Path, new List<string> { $"{fileDto.FileName}, {File.GetLastWriteTime(fileDto.Path)}" });

                var file = request.ResponseBody;

                Console.WriteLine($"File ID: {file.Id}");
            }
        }
    }
}

// TODO
// Update this so that instead of uploading a file, upload a 
// a directory, including all child directories/files
// Eventually, update so that this can upload a directory
// with any number of sub-directories/files so that I can
// designate one folder save particular files to to be
// automatically uploaded just by nature of being in that
// directory

// TODO
// Implement memory cache for LastUpdatedTime to replace physical file
// since this will be an always-running background task

// TODO
// Target Ubuntu for deployment

// TODO
// Setup scheduler

// TODO
// Update LastUpdateTime.txt with a new dictionary-readable entry where
// key=fileName, value=lastUpdatedDateTime
// When next upload cycle runs, append a new line with a new dictionary entry
// for {fileName, lastUpdatedDateTime} and leave 'if' check to use '.Contains()'
// which should == true regardless of position in file
// Since this will be a scheduled background service, clear the LastUpdatedTime.txt
// file every 24 hrs or every day at midnight to prevent file clutter build up

// TODO
// Since (at least game saves) files/directory will be named the same,
// get a list of all files/documents/directories and if there are more
// than n number of files with the same name, delete them starting from
// oldest until number of files with the same name == n

// TODO
// Fix 'append new line with dictionary<fileName, lastUpdated>' to
// override an entry with the same key value to only update lastUpdated