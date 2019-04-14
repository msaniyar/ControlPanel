using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using RestSharp;



namespace ControlPanel.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(HttpPostedFileBase postedFile, string username, string password)
        {
            if (postedFile == null) return View();
            var fileExtension = Path.GetExtension(postedFile.FileName);

            if (!Validation(username, password))
            {
                ViewBag.Message = "Password or username is wrong.";
                return View();
            }

            if (!ZipValidation(fileExtension))
            {
                ViewBag.Message = "Please provide a zip file.";
                return View();
            }

            var path = Server.MapPath("~/Uploads/");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            postedFile.SaveAs(path + Path.GetFileName(postedFile.FileName));

            var newFileName = Path.Combine(path, postedFile.FileName);

            var listEntries = new List<string>();
            using (var archive = ZipFile.OpenRead(newFileName))
            {
                foreach (var entry in archive.Entries)
                {
                    listEntries.Add(entry.ToString());
                }
            }

            ViewBag.Message = "File read and sent to the database. Tree is listed below";

            foreach (var list in listEntries)
            {
                ViewBag.Message += list + "\n";
            }


            ViewBag.Message += SendRequest(username, password);



            return View();
        }


        private static bool Validation(string username, string password)
        {

            var storedPassword = ConfigurationManager.AppSettings["Password"];
            var storedUserName = ConfigurationManager.AppSettings["UserName"];

            if (storedUserName != username || storedPassword != password)
            {
                return false;
            }

            return true;
        }

        private static bool ZipValidation(string fileExtension)
        {

            if (fileExtension != ".zip")
            {
                return false;
            }

            return true;
        }


        private static object SendRequest(string username, string password)
        {
            var request = new RestRequest(Method.POST);
            var endpoint = ConfigurationManager.AppSettings["RemoteURL"];

            request.AddJsonBody(new {password = password, username = username});
            request.AddHeader("Content-Type", "application/json, application/xml, text/json, text/x-json, text/javascript, text/xml");
            request.AddHeader("Accept", "application/json, application/xml, text/json, text/x-json, text/javascript, text/xml");

            var client = new RestClient(endpoint) { Timeout = 30000 };

            var response = client.Execute(request);
            var message = JsonConvert.DeserializeObject(response.Content);
            return message;

        }

    }
}