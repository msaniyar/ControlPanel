using System.IO;
using System.IO.Compression;
using System.Web;

namespace ControlPanel.Services
{
    public class FileService : IServices
    {
        public void FileProcess(HttpPostedFileBase postedFile)
        {
            var path = Path.Combine(System.AppContext.BaseDirectory, "Uploads");
            var pathTree = Path.Combine(System.AppContext.BaseDirectory, "Tree");
            DirectoryDelete(path);
            DirectoryDelete(pathTree);

            postedFile.SaveAs(path + Path.GetFileName(postedFile.FileName));
            ZipFile.ExtractToDirectory(Path.Combine(path, postedFile.FileName), pathTree);
        }

        public void DirectoryDelete (string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            else
            {
                var directoryInfo = new DirectoryInfo(path);
                foreach (var file in directoryInfo.GetFiles())
                {
                    file.Delete();
                }
                foreach (var directory in directoryInfo.GetDirectories())
                {
                    directory.Delete(true);
                }
            }
        }
    }
}