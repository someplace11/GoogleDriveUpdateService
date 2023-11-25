namespace GoogleDriveUpdateService.Interfaces
{
    public interface ILocalFileHelper
    {
        void TraverseDirectory(string directoryPath, string parentId);
    }
}
