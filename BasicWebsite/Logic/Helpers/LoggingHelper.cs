using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Data.NoSql.Interfaces;

namespace Logic.Helpers
{
    //this was created after LogRepository but is more dynamic and hacky
    //should try merge them to an optimum middleground
    public class LoggingHelper
    {
        //choose which dataprovider to use here
        private INoSQLDataProvider _dataProvider = new Data.NoSql.Servers.DocumentDBServer();

        public LoggingHelper(INoSQLDataProvider dataProvider)
        {
            this._dataProvider = dataProvider;
        }

        /// <summary>
        /// Logs an object to a NoSQL data repository
        /// </summary>
        /// <param name="data">anything you with to log. Will be converted ToJSON() and stored with some additional data wrapped around it</param>
        public async Task<string> Log(object data)
        {
            var log = WrapLogData(data);
            await _dataProvider.WriteDocument("Logs", log);
            return log["Code"].ToString();
        }

        private Dictionary<string, object> WrapLogData(object data)
        {
            var logContainer = new Dictionary<string, object>
            {
                ["Data"] = data,
                ["Code"] = Guid.NewGuid().ToString("N")
            };

            if (System.Web.HttpContext.Current != null)
            {
                logContainer["Url"] = System.Web.HttpContext.Current.Request.Url.PathAndQuery;
            }

            return logContainer;
        }
    }
}
