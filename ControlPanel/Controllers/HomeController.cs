using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO.Compression;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using System.Xml.Schema;
using Newtonsoft.Json;
using RestSharp;
using Newtonsoft.Json.Linq;
using ControlPanel.JsonTreeCreate;
using ControlPanel.Services;

namespace ControlPanel.Controllers
{
    public class HomeController : Controller
    {

        private readonly IServices _service;

        public HomeController()
        {
        }

        public HomeController(IServices service)
        {
            _service = service;
        }

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
            var fileName = Path.GetFileNameWithoutExtension(postedFile.FileName);

            if (!_service.CredentialsValidation(username, password))
            {
                ViewBag.Message = "Password or username is wrong.";
                return View();
            }

            if (_service.ZipValidation(fileExtension))
            {
                ViewBag.Message = "Please provide a zip file.";
                return View();
            }

           var treePath = _service.FileProcess(postedFile);
           var directoryInfo = new DirectoryInfo(treePath);
           var treeResult = directoryInfo.ToJson(f => f.LastWriteTimeUtc);


            if (!_service.SendRequest(username, password, treeResult))
            {
                ViewBag.Message = "En error occured. Please try again.";
            }
            else
            {
                ViewBag.Message = "File read and sent to the database. Tree is listed:    ";
                ViewBag.Message += treeResult;
            }


            return View();
        }
    }
}