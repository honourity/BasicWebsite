using System;
using System.Threading.Tasks;
using Data.Interfaces;

using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;
using System.Linq;

namespace Data.Servers
{
    public class DocumentDBServer : INoSQLDataProvider
    {
        private const string END_POINT_URL = "https://webtest.documents.azure.com:443/";
        private const string AUTHORIZATION_KEY = "onr8XqNFhXr8PgPxw2MUBp3FlJMuEA7JEOGds1lt6OE9UAbcXjnHDABjuJzKlMWyRVqYmKA2sJXrVAA2eFQ6iQ==";
        private const string DATABASE_NAME = "Logs";

        private DocumentClient _client;
        private Database _database;

        public DocumentDBServer()
        {
            _client = new DocumentClient(new Uri(END_POINT_URL), AUTHORIZATION_KEY);

            _database = InitializeDatabase(_client).Result;
        }

        private async Task<Database> InitializeDatabase(DocumentClient client)
        {
            Database database = client.CreateDatabaseQuery().Where(db => db.Id == DATABASE_NAME).AsEnumerable().FirstOrDefault();

            if (database == null)
            {
                database = await client.CreateDatabaseAsync(
                    new Database
                    {
                        Id = DATABASE_NAME
                    });
            }

            return database;
        }

        private async Task<DocumentCollection> InitializeCollection(DocumentClient client, Database database, string collectionName)
        {
            DocumentCollection collection = client.CreateDocumentCollectionQuery("dbs/" + database.Id).Where(c => c.Id == collectionName).AsEnumerable().FirstOrDefault();

            if (collection == null)
            {
                collection = await client.CreateDocumentCollectionAsync("dbs/" + database.Id,
                    new DocumentCollection
                    {
                        Id = collectionName
                    });
            }

            return collection;
        }

        public async Task WriteDocument(string collectionName, dynamic document)
        {
            DocumentCollection collection = InitializeCollection(_client, _database, collectionName).Result;

            await _client.CreateDocumentAsync("dbs/" + _database.Id + "/colls/" + collection.Id, document);
        }
    }
}
