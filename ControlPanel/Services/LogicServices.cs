using System;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;

namespace ControlPanel.Services
{
    public class LogicServices : IServices
    {

        public string Path = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Uploads");
        public string PathTree = System.IO.Path.Combine(System.AppContext.BaseDirectory, "Tree");

        public bool CredentialsValidation(string username, string password)
        {

            var storedPassword = ConfigurationManager.AppSettings["Password"];
            var storedUserName = ConfigurationManager.AppSettings["UserName"];

            return storedUserName == username && storedPassword == password;
        }

        public bool ZipValidation(string fileExtension)
        {
            return fileExtension == ".zip";
        }

        public string FileProcess(HttpPostedFileBase postedFile)
        {

            DirectoryDelete(Path);
            DirectoryDelete(PathTree);

            postedFile.SaveAs(System.IO.Path.Combine(Path, postedFile.FileName));
            ZipFile.ExtractToDirectory(System.IO.Path.Combine(Path, postedFile.FileName), PathTree);

            return PathTree;
        }

        private static void DirectoryDelete(string path)
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
            var secret = ConfigurationManager.AppSettings["SecretKey"];
            var treeString = JsonConvert.SerializeObject(tree);
            var authenticator = new HttpBasicAuthenticator(username, password);
            var treeEncrypted = Encrypt(treeString, secret);


            var parameters = new JObject
            (
                new JProperty("id", Guid.NewGuid()),
                new JProperty("tree", treeEncrypted)
            );

            request.AddParameter("application/json", parameters, RestSharp.ParameterType.RequestBody);

            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Accept", "application/json");

            var client = new RestClient(endpoint) { Timeout = 30000 };
            authenticator.Authenticate(client, request);
            var response = client.Execute(request);
            return response.StatusCode == HttpStatusCode.OK;
        }

        public static string Encrypt(string value, string password)
        {
            return Encrypt<AesManaged>(value, password);
        }
        public static string Encrypt<T>(string value, string password)
            where T : SymmetricAlgorithm, new()
        {

            var vectorBytes = Encoding.ASCII.GetBytes(ConfigurationManager.AppSettings["vector"]);
            var saltBytes = Encoding.ASCII.GetBytes(ConfigurationManager.AppSettings["salt"]);
            var valueBytes = Encoding.UTF8.GetBytes(value);
            var iterations = ConfigurationManager.AppSettings["iteration"];
            var keySize = ConfigurationManager.AppSettings["keysize"];


            byte[] encrypted;
            var passwordBytes =
                new Rfc2898DeriveBytes(password, saltBytes, int.Parse(iterations));

            using (var cipher = new T())
            {
                var keyBytes = passwordBytes.GetBytes(int.Parse(keySize) / 8);

                cipher.Mode = CipherMode.CBC;

                using (var encrypt = cipher.CreateEncryptor(keyBytes, vectorBytes))
                {
                    using (var to = new MemoryStream())
                    {
                        using (var writer = new CryptoStream(to, encrypt, CryptoStreamMode.Write))
                        {
                            writer.Write(valueBytes, 0, valueBytes.Length);
                            writer.FlushFinalBlock();
                            encrypted = to.ToArray();
                        }
                    }
                }
                cipher.Clear();
            }
            return Convert.ToBase64String(encrypted);
        }

    }
}