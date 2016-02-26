using System.Threading.Tasks;

namespace Data.Interfaces
{
    public interface IMongoDBServer
    {
        Task WriteDocument(string collectionName, dynamic document);
    }
}
