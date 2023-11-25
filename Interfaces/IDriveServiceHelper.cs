using Google.Apis.Drive.v3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleDriveUpdateService.Interfaces
{
    public interface IDriveServiceHelper
    {
        DriveService CreateGoogleDriveService();
    }
}
