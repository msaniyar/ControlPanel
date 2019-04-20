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

        /// <summary>
        /// Validate username and password against pre-configured values.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool CredentialsValidation(string username, string password)
        {

            var storedPassword = ConfigurationManager.AppSettings["Password"];
            var storedUserName = ConfigurationManager.AppSettings["UserName"];

            return storedUserName == username && storedPassword == password;
        }

        /// <summary>
        /// Zip file suffix validation.
        /// </summary>
        /// <param name="fileExtension"></param>
        /// <returns></returns>
        public bool ZipValidation(string fileExtension)
        {
            return fileExtension == ".zip";
        }

        /// <summary>
        /// Gets zip file and extract it to directory for further processing.
        /// </summary>
        /// <param name="postedFile"></param>
        /// <returns></returns>
        public string FileProcess(HttpPostedFileBase postedFile)
        {

            DirectoryDelete(Path);
            DirectoryDelete(PathTree);

            postedFile.SaveAs(System.IO.Path.Combine(Path, postedFile.FileName));
            ZipFile.ExtractToDirectory(System.IO.Path.Combine(Path, postedFile.FileName), PathTree);

            return PathTree;
        }

        /// <summary>
        /// Delete directory for re-use according to path.
        /// </summary>
        /// <param name="path"></param>
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

        /// <summary>
        /// HTTP Post request sending to remote application with basic authentication and encryption.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="tree"></param>
        /// <returns></returns>
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


        /// <summary>
        /// Static method for calling Encrypt method.
        /// </summary>
        /// <param name="raw"></param>
        /// <param name="key"></param>
        /// <param name="vector"></param>
        /// <returns></returns>
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


        /// <summary>
        /// Encrypt given string with using AES encryption.
        /// </summary>
        /// <param name="plainText"></param>
        /// <param name="Key"></param>
        /// <param name="IV"></param>
        /// <returns></returns>
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