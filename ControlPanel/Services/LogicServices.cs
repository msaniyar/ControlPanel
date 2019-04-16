using System;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Web;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace ControlPanel.Services
{
    public class LogicServices : IServices
    {

        public string path = Path.Combine(System.AppContext.BaseDirectory, "Uploads");
        public string pathTree = Path.Combine(System.AppContext.BaseDirectory, "Tree");

        public bool CredentialsValidation(string username, string password)
        {

            var storedPassword = ConfigurationManager.AppSettings["Password"];
            var storedUserName = ConfigurationManager.AppSettings["UserName"];

            if (storedUserName != username || storedPassword != password)
            {
                return false;
            }

            return true;
        }

        public bool ZipValidation(string fileExtension)
        {

            if (fileExtension != ".zip")
            {
                return false;
            }

            return true;
        }

        public string FileProcess(HttpPostedFileBase postedFile)
        {

            DirectoryDelete(path);
            DirectoryDelete(pathTree);

            postedFile.SaveAs(Path.Combine(path, postedFile.FileName));
            ZipFile.ExtractToDirectory(Path.Combine(path, postedFile.FileName), pathTree);

            return pathTree;
        }

        private void DirectoryDelete(string path)
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

        public bool SendRequest(string username, string password, JObject tree)
        {
            var request = new RestRequest(Method.POST);
            var endpoint = ConfigurationManager.AppSettings["RemoteURL"];
            var parameters = new JObject
            (
                new JProperty("username", username),
                new JProperty("password", password),
                new JProperty("tree", tree)
            );

            //request.AddJsonBody(request.JsonSerializer.Serialize(tree));
            request.AddParameter("application/json", parameters, RestSharp.ParameterType.RequestBody);

            request.AddHeader("Content-Type", "application/json, application/xml, text/json, text/x-json, text/javascript, text/xml");
            request.AddHeader("Accept", "application/json, application/xml, text/json, text/x-json, text/javascript, text/xml");

            var client = new RestClient(endpoint) { Timeout = 30000 };

            var response = client.Execute(request);
            if (!(response.StatusCode == HttpStatusCode.OK))
            {
                return false;
            }

            return true;


        }
    }
}