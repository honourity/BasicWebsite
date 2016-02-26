using System.Threading.Tasks;

namespace Data.MongoDB
{
    public interface IMongoDBServer
    {
        Task WriteDocument(string collectionName, dynamic document);
    }
}
