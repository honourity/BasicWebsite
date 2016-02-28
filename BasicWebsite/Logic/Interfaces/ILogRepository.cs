namespace Logic.Interfaces
{
    public interface ILogRepository
    {
        //void Log(System.Web.Mvc.Controller sender);

        void Log(System.Web.Mvc.ActionExecutingContext sender);

        void Log(System.Web.Mvc.ActionExecutedContext sender);

        //void Log(dynamic sender);
    }
}
