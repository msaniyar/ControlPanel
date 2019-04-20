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
            var vector = ConfigurationManager.AppSettings["vector"];

            var treeString = JsonConvert.SerializeObject(tree);
            var authenticator = new HttpBasicAuthenticator(username, password);
            var treeEncrypted = EncryptAesManaged(treeString, secret, vector);


            var parameters = new JObject
            (
                new JProperty("id", Guid.NewGuid()),
                new JProperty("tree", treeEncrypted)
            );

            request.AddParameter("application/json", parameters, RestSharp.ParameterType.RequestBody);

            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Accept", "application/json");

            var client = new RestClient(endpoint + "Data/Add") {Timeout = 30000};
            authenticator.Authenticate(client, request);
            var response = client.Execute(request);
            return response.StatusCode == HttpStatusCode.OK;
        }

        public static string EncryptAesManaged(string raw, string key, string vector)
        {
            try
            {

                // Create Aes that generates a new key and initialization vector (IV).    
                // Same key must be used in encryption and decryption    
                using (var aes = new AesManaged())
                {
                    aes.Mode = CipherMode.ECB;
                    // Encrypt string    
                    return Convert.ToBase64String(Encrypt(raw, Convert.FromBase64String(key), Convert.FromBase64String(vector)));
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static byte[] Encrypt(string plainText, byte[] Key, byte[] IV)
        {
            byte[] encrypted;
            // Create a new AesManaged.    
            using (var aes = new AesManaged())
            {
                // Create encryptor    
                var encryptor = aes.CreateEncryptor(Key, IV);
                // Create MemoryStream    
                using (var ms = new MemoryStream())
                {
                    // Create crypto stream using the CryptoStream class. This class is the key to encryption    
                    // and encrypts and decrypts data from any given stream. In this case, we will pass a memory stream    
                    // to encrypt    
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        // Create StreamWriter and write data to a stream    
                        using (var sw = new StreamWriter(cs))
                            sw.Write(plainText);
                        encrypted = ms.ToArray();
                    }
                }
            }

            // Return encrypted data    
            return encrypted;

        }
    }
}