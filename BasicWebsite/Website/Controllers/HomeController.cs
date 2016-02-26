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

        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            base.OnActionExecuted(filterContext);

            _logRepository.Log(this);
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
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