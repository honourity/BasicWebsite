using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;

namespace BasicWebsite.BusinessLogic
{
    public class DocumentDBLogger
    {
        private const string EndpointUrl = "https://webtest.documents.azure.com:443/";
        private const string AuthorizationKey = "onr8XqNFhXr8PgPxw2MUBp3FlJMuEA7JEOGds1lt6OE9UAbcXjnHDABjuJzKlMWyRVqYmKA2sJXrVAA2eFQ6iQ==";

        public void Log(dynamic data)
        {
            WriteToDocumentDB(data);
        }

        private static async Task WriteToDocumentDB(dynamic data)
        {

            // Create a new instance of the DocumentClient
            var client = new DocumentClient(new Uri(EndpointUrl), AuthorizationKey);

            // Check to verify a database with the id=FamilyRegistry does not exist
            Database database = client.CreateDatabaseQuery().Where(db => db.Id == "Logs").AsEnumerable().FirstOrDefault();

            // If the database does not exist, create a new database
            if (database == null)
            {
                database = await client.CreateDatabaseAsync(
                    new Database
                    {
                        Id = "Logs"
                    });
            }

            // Check to verify a document collection with the id=FamilyCollection does not exist
            DocumentCollection documentCollection = client.CreateDocumentCollectionQuery("dbs/" + database.Id).Where(c => c.Id == "Website").AsEnumerable().FirstOrDefault();

            // If the document collection does not exist, create a new collection
            if (documentCollection == null)
            {
                documentCollection = await client.CreateDocumentCollectionAsync("dbs/" + database.Id,
                    new DocumentCollection
                    {
                        Id = "Website"
                    });
            }
            
            // id based routing for the first argument, "dbs/FamilyRegistry/colls/FamilyCollection"
            await client.CreateDocumentAsync("dbs/" + database.Id + "/colls/" + documentCollection.Id, data);
        }
    }
}