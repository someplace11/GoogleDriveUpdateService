using Google.Apis.Drive.v3;

namespace GoogleDriveUpdateService.Interfaces
{
    public interface IGoogleDriveCRUDHelper
    {
        void UploadFileToDrive(string filePath, string parentId);
        void RemoveExcessCopies();
    }
}
