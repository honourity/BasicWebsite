using System;
using System.Threading.Tasks;
using Data.NoSql.Interfaces;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System.Linq;
using System.Collections.Generic;

namespace Data.NoSql.Servers
{
    public class DocumentDBServer : INoSQLDataProvider
    {
        private const string END_POINT_URL = "https://ltaweb.documents.azure.com:443/";
        private const string AUTHORIZATION_KEY = "SvjBdyvPT4F10lpS8o75otZQSPaF1nKaEgrPWoprEeRmhRnsmKeimzZHlagPfRmGBpOqqziscX74EQZYJGWB0Q==";
        private const string DATABASE_NAME = "Logs";

        private DocumentClient _client;
        private Database _database;

        public DocumentDBServer()
        {
            _client = new DocumentClient(new Uri(END_POINT_URL), AUTHORIZATION_KEY);

            Task<Database> initDatabase = InitializeDatabase(_client);
            initDatabase.Wait();
            _database = initDatabase.Result;
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
            //init documentDB link
            Task<DocumentCollection> initCollection = InitializeCollection(_client, _database, collectionName);
            initCollection.Wait();
            DocumentCollection collection = initCollection.Result;

            await WriteDocumentWithRetries("dbs/" + _database.Id + "/colls/" + collection.Id, document);
        }

        public IEnumerable<dynamic> QueryDocuments(string query)
        {
            var resultsList = new List<dynamic>();

            IEnumerable<DocumentCollection> collections = _client.CreateDocumentCollectionQuery((string)_database.SelfLink).ToList();

            foreach (DocumentCollection collection in collections)
            {
                resultsList.AddRange(_client.CreateDocumentQuery<dynamic>(collection.DocumentsLink, query).AsEnumerable());
            }

            return resultsList;
        }

        private async Task<ResourceResponse<Document>> WriteDocumentWithRetries(string collectionLink, dynamic document)
        {
            TimeSpan sleepTime = TimeSpan.Zero;
            while (true)
            {
                try
                {
                    return await _client.CreateDocumentAsync(collectionLink, document);
                }
                catch (DocumentClientException de)
                {
                    if ((int)de.StatusCode != 429)
                    {
                        throw;
                    }
                    sleepTime = de.RetryAfter;
                }
                catch (AggregateException ae)
                {
                    if (!(ae.InnerException is DocumentClientException))
                    {
                        throw;
                    }

                    DocumentClientException de = (DocumentClientException)ae.InnerException;
                    if ((int)de.StatusCode != 429)
                    {
                        throw;
                    }
                    sleepTime = de.RetryAfter;
                }

                await Task.Delay(sleepTime);
            }
        }
    }
}