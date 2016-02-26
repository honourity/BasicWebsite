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
            ViewBag.Message = "Your application description page.";

            _logRepository.Log(this);

            _logRepository.Log(string.Empty);

            _logRepository.Log(null);

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}