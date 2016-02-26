using System.Web.Mvc;
using Logic.Interfaces;

namespace BasicWebsite.Controllers
{
    public class HomeController : Controller
    {
        private ILogRepository _logRepository;

        public HomeController(ILogRepository logRepository)
        {
            this._logRepository = logRepository;
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            var log = new Logic.Models.Log();

            log.Exception = new System.Exception("exception message!", new System.Exception("inner exception message!"));
            log.Page = this.HttpContext.Request.Url.AbsoluteUri;

            _logRepository.Log(log);

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