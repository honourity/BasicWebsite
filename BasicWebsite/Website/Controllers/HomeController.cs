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

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            //only log this in the case of a POST, otherwise the data will be null and its pointless
            if (HttpContext.Request.RequestType == "POST") _logRepository.Log(filterContext);            
        }

        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            base.OnActionExecuted(filterContext);

            //use this to get the model data filterContext.Controller.ViewData.Model;
            _logRepository.Log(filterContext);
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