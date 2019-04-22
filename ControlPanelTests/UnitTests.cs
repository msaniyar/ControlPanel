using System;
using System.Configuration;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Web;
using ControlPanel.Services;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Moq;
using System.Text;

namespace ControlPanelTests
{
    [TestFixture]
    public class UnitTests
    {
        private readonly IServices _services;

        public UnitTests()
        {
            _services = new LogicServices();
        }
        [Test, Order(1)]
        public void CredentialsValidationTest()
        {
            var username = "test";
            var password = "test123";
            var result = _services.CredentialsValidation(username, password);

            Assert.That(result, Is.True, "Validation code is wrong.");

        }

        [Test, Order(1)]
        public void CredentialsValidationFailTest()
        {
            var username = "xyz";
            var password = "xyz";
            var result = _services.CredentialsValidation(username, password);

            Assert.That(result, Is.False, "Validation method fails for wrong credentials.");

        }

        [Test, Order(2)]
        public void SendRequestTest()
        {
            var username = "test";
            var password = "test123";
            var parameters = new JObject
            (
                new JProperty("id", Guid.NewGuid()),
                new JProperty("tree", "{\"test.txt\":\"2019-04-13T16:31:38Z\"}")
            );

            var result = _services.SendRequest(username, password, parameters);

            //Data management end is setup with online mock endpoint in app.config.
            Assert.That(result, Does.Contain("File read and sent to the database"), "Request send fail.");

        }

        [Test, Order(3)]
        public void ZipValidationSuccessTest()
        {
            var result = _services.ZipValidation(".zip");
            Assert.That(result, Is.True, "Zip validation is getting error.");
        }


        [Test, Order(3)]
        public void ZipValidationErrorTest()
        {
            var result = _services.ZipValidation(".xyz");
            Assert.That(result, Is.False, "Zip validation is getting error.");
        }


        [Test, Order(4)]
        public void EncryptAesManagedTest()
        {
            var secret = ConfigurationManager.AppSettings["SecretKey"];
            var vector = ConfigurationManager.AppSettings["vector"];
            var tree = "{\"test.txt\":\"2019-04-13T16:31:38Z\"}";
            var result = _services.EncryptAesManaged(tree, secret, vector);
            Assert.That(result, Is.EqualTo("hqfoLjEw7HLYnap51O2Lb6tjNI+Rdn1UXo5rihOy/MHn+myU4YqEKonVRaR8FItX"), "Encryption is failing.");
        }

        [Test, Order(5)]
        public void FileProcessTest()
        {
            var filePath = Path.Combine(System.AppContext.BaseDirectory, "Resource//test.zip");
            var savePath = Path.Combine(System.AppContext.BaseDirectory, "Uploads//test.zip");
            var path = Path.Combine(System.AppContext.BaseDirectory, "Tree");
            UTF8Encoding enc = new UTF8Encoding();

            Mock<HttpPostedFileBase> postedFile = new Mock<HttpPostedFileBase>();
            postedFile
                .Setup(f => f.FileName)
                .Returns(filePath);

            postedFile
                .Setup(f => f.InputStream)
                .Returns(new MemoryStream(enc.GetBytes(filePath)));

            postedFile
                .Setup(f => f.ContentLength)
                .Returns(10);

            postedFile
                .Setup(f => f.SaveAs(Path.Combine(filePath, "test.zip")));

            var result = _services.FileProcess(postedFile.Object);
            Assert.That(result, Is.EqualTo(path), "Path is wrong.");
        }

    }
}
