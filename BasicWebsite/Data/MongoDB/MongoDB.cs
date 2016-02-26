using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Data.MongoDB
{
    public class MongoDBServer : IMongoDBServer
    {
        private const string DatabaseName = "webtest";

        private MongoClient _client;

        public MongoDBServer()
        {
            MongoClientSettings settings = new MongoClientSettings();

            //dont wait for result from server. (since we are only using this for logging, we want it fast!)
            settings.WriteConcern = new WriteConcern(0);

            settings.Server = new MongoServerAddress("mongodb://webtest:apoi1237@ds062818.mlab.com:62818/webtest");

            this._client = new MongoClient(settings);
        }

        public async Task WriteDocument(string collectionName, dynamic document)
        {
            var database = this._client.GetDatabase(DatabaseName);
            var collection = database.GetCollection<BsonDocument>(collectionName);

            //if inserted object does not contain _id field, it will auto generate its own upon insert
            await collection.InsertOneAsync(document);
        }
    }
}
