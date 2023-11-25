using Google.Apis.Drive.v3;
using GoogleDriveUpdateService.Interfaces;
using GFile = Google.Apis.Drive.v3.Data.File;


namespace GoogleDriveUpdateService.Helpers
{
    public class LocalFileHelper : ILocalFileHelper
    {
        private readonly IGoogleDriveCRUDHelper _gdHelper;
        private DriveService _service;

        public LocalFileHelper(IGoogleDriveCRUDHelper gdHelper, IDriveServiceHelper driveServiceHelper)
        {
            _gdHelper = gdHelper;

            _service = driveServiceHelper.CreateGoogleDriveService();
        }

        public void TraverseDirectory(string directoryPath, string parentId)
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
                _gdHelper.UploadFileToDrive(directoryPath, parentId);

                return;
            }

            Console.WriteLine($"Current Directory: {currentDirectoryName}");

            var fileMetadata = new GFile
            {
                Name = currentDirectoryName,
                Parents = new[] { parentId },
                MimeType = "application/vnd.google-apps.folder"
            };

            var createDirectoryRequest = _service.Files.Create(fileMetadata);
            createDirectoryRequest.Fields = "id";
            var directory = createDirectoryRequest.Execute();

            foreach (var filePath in files)
            {
                Console.WriteLine($"File: {filePath}");

                // Upload File
                _gdHelper.UploadFileToDrive(filePath, directory.Id);
            }

            // Get all subdirectories in the current directory
            string[] subdirectories = Directory.GetDirectories(directoryPath);
            foreach (var subdirectory in subdirectories)
            {
                var subdirectoryName = Path.GetFileName(subdirectory);
                Console.WriteLine($"Subdirectory: {subdirectoryName}");

                // Recursive call to traverse subdirectories
                TraverseDirectory(subdirectory, directory.Id);
            }
        }
    }
}
