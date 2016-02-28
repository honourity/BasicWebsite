using System.Web.Mvc;
using Logic.Interfaces;
using BasicWebsite.Models;

namespace BasicWebsite.Controllers
{
    public class HomeController : Controller
    {
        private ILogRepository _logRepository;

        public HomeController(ILogRepository logRepository)
        {
            this._logRepository = logRepository;
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            if (HttpContext.Request.RequestType == "POST") _logRepository.Log(filterContext);            
        }

        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            base.OnActionExecuted(filterContext);
            _logRepository.Log(filterContext);
        }

        public ActionResult Index()
        {
            HomeModel model = new HomeModel();

            return View(model);
        }

        public ActionResult About()
        {
            HomeModel model = new HomeModel();

            return View(model);
        }

        public ActionResult Contact()
        {
            HomeModel model = new HomeModel();

            return View(model);
        }
    }
}