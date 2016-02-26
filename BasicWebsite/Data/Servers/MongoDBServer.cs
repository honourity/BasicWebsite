using System.Threading.Tasks;
using MongoDB.Driver;
using Data.Interfaces;

namespace Data.Servers
{
    public class MongoDBServer : IMongoDBServer
    {
        private const string ConnectionString = "mongodb://jtcgreyfox:apoi1237@ds062818.mlab.com:62818/webtest?w=0";
        private const string DatabaseName = "webtest";

        private MongoClient _client;

        public MongoDBServer()
        {
            this._client = new MongoClient(ConnectionString);
        }

        public async Task WriteDocument(string collectionName, dynamic document)
        {
            var database = this._client.GetDatabase(DatabaseName);
            var collection = database.GetCollection<dynamic>(collectionName);
            await collection.InsertOneAsync(document);
        }
    }
}
