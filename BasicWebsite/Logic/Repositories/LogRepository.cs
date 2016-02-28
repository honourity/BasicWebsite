using Data.Interfaces;
using Logic.Interfaces;

namespace Logic.Repositories
{
    public class LogRepository : ILogRepository
    {
        private const string REPOSITORY_COLLECTION_NAME = "Website";

        private INoSQLDataProvider _database1;
        private INoSQLDataProvider _database2;

        public LogRepository()
        {
            //use INoSQLDataProvider database for dependency injection
            _database1 = new Data.Servers.MongoDBServer();
            _database2 = new Data.Servers.DocumentDBServer();
        }

        public void Log(System.Web.Mvc.ActionExecutingContext sender)
        {
            var log = NewDynamicLog();

            log.Page = sender.HttpContext.Request.Url.AbsoluteUri;
            log.HttpMethod = sender.HttpContext.Request.HttpMethod;
            log.HttpDirection = "Request";
            log.ViewModel = sender.Controller.ViewData.Model;

            _database1.WriteDocument(REPOSITORY_COLLECTION_NAME, log);
            _database2.WriteDocument(REPOSITORY_COLLECTION_NAME, log);
        }

        public void Log(System.Web.Mvc.ActionExecutedContext sender)
        {
            var log = NewDynamicLog();

            log.Page = sender.HttpContext.Request.Url.AbsoluteUri;
            log.HttpMethod = sender.HttpContext.Request.HttpMethod;
            log.HttpDirection = "Response";
            log.ViewModel = sender.Controller.ViewData.Model;

            _database1.WriteDocument(REPOSITORY_COLLECTION_NAME, log);
            _database2.WriteDocument(REPOSITORY_COLLECTION_NAME, log);
        }

        //public void Log(dynamic sender)
        //{
        //    var log = NewDynamicLog();

        //    log.SenderData = sender;

        //    _database.WriteDocument(repositoryCollection, log);
        //}
    
        private dynamic NewDynamicLog()
        {
            dynamic log = new System.Dynamic.ExpandoObject();

            return log;
        }
    }
}
