using Data.MongoDB;
using Logic.Interfaces;
using Logic.Models;

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

        public void Log(Log log)
        {
            _database.WriteDocument(repositoryCollection, log);
        }
    }
}
