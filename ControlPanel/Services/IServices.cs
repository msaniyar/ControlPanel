using System;
using System.IO;
using System.Web;
using Newtonsoft.Json.Linq;

namespace ControlPanel.Services
{
    public interface IServices
    {
        bool CredentialsValidation(string username, string pasword);
        bool ZipValidation(string fileExtension);
        string FileProcess(HttpPostedFileBase postedFile);
        bool SendRequest(string username, string password, JObject tree);


    }
}