namespace Logic.Interfaces
{
    public interface ILogRepository
    {
        void Log(System.Web.Mvc.Controller sender);

        void Log(dynamic sender);
    }
}
