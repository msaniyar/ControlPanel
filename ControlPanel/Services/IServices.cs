using System;
using System.IO;
using System.Web;
using Newtonsoft.Json.Linq;

namespace ControlPanel.Services
{
    /// <summary>
    /// Interface definition for logic services
    /// </summary>
    public interface IServices
    {
        bool CredentialsValidation(string username, string password);
        bool ZipValidation(string fileExtension);
        string FileProcess(HttpPostedFileBase postedFile);
        string SendRequest(string username, string password, JObject tree);


    }
}