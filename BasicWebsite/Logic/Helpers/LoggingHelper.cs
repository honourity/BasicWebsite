using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.NoSql.Interfaces;
using Shared.Settings;

namespace Shared.Helpers
{
    public static class LoggingHelper
    {
        //choose which dataprovider to use here
        private static INoSQLDataProvider _dataProvider = new Data.NoSql.Servers.DocumentDBServer();

        /// <summary>
        /// Logs an object to a NoSQL data repository
        /// </summary>
        /// <param name="data">anything you with to log. Will be converted ToJSON() and stored with some additional data wrapped around it</param>
        public static async Task Log(object data)
        {
            var environment = ConfigHelper.GetConfigValue<string>("EnvironmentURL");
            await _dataProvider.WriteDocument(environment ?? "NoEnvironment", WrapLogData(environment, data));
        }

        /// <summary>
        /// Fetches NoSQL log data based on input query string
        /// </summary>
        /// <param name="query">an sql query string to fetch a json array of matching log entries</param>
        public static string Query(string query)
        {
            StringBuilder result = new StringBuilder();
            result.Append("{\"Logs\":[");

            var documents = _dataProvider.QueryDocuments(query);
            foreach (dynamic document in documents)
            {
                result.AppendLine(((object)document).ToJSON());
                result.Append(",");
            }

            if (documents.Any()) result.Remove(result.Length - 1, 1);

            result.Append("]}");

            return result.ToString();
        }

        private static object WrapLogData(string environment, object data)
        {
            var logContainer = new Dictionary<string, object>();

            logContainer["Data"] = data;

            if (System.Web.HttpContext.Current != null)
            {
                logContainer["Url"] = System.Web.HttpContext.Current.Request.Url.PathAndQuery;
            }

            logContainer["Environment"] = environment ?? "EnvironmentURL not defined in appsettings for current project";
            logContainer["TimeStamp"] = CustomDateFormat(DateTime.Now);

            return logContainer;
        }

        private static long CustomDateFormat(DateTime date)
        {
            return Convert.ToInt64(date.ToString("yyyyMMddHHmmss"));
        }
    }
}
