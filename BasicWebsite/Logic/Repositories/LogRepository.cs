using System;
using Data.Interfaces;
using Logic.Interfaces;

namespace Logic.Repositories
{
    public class LogRepository : ILogRepository
    {
        private const string repositoryCollection = "Logs";

        private IMongoDBServer _database;

        public LogRepository(IMongoDBServer database)
        {
            this._database = database;
        }

        public void Log(System.Web.Mvc.Controller sender)
        {
            var log = NewDynamicLog();

            log.Page = sender.HttpContext.Request.Url.AbsoluteUri;
            log.HttpMethod = sender.HttpContext.Request.HttpMethod;
            log.ViewBag = (dynamic)sender.ViewBag;

            _database.WriteDocument(repositoryCollection, log);
        }

        public void Log(dynamic sender)
        {
            var log = NewDynamicLog();

            log.SenderData = sender;

            _database.WriteDocument(repositoryCollection, log);
        }

        private dynamic NewDynamicLog()
        {
            dynamic log = new System.Dynamic.ExpandoObject();

            //populating always-present fields
            log._id = System.Guid.NewGuid();
            log._timestamp = System.DateTime.Now;

            return log;
        }
    }
}
