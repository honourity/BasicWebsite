using System.Threading.Tasks;

namespace Data.Interfaces
{
    public interface INoSQLDataProvider
    {
        Task WriteDocument(string collectionName, dynamic document);
    }
}
