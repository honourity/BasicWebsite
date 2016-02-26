using System;
using System.Web.Mvc;
using BusinessLogic.Models;

namespace BasicWebsite.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {            
            return View();
        }

        public ActionResult About()
        {
            BusinessLogic.DocumentDBLogger logger = new BusinessLogic.DocumentDBLogger();
            WebsiteLog log = new WebsiteLog();
            log.Exception = new Exception("exception message!", new Exception("inner exception message!"));
            log.Page = HttpContext.Request.Url.AbsoluteUri;
            logger.Log(log);

            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}