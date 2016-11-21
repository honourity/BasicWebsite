using System.Collections.Generic;
using System.Threading.Tasks;

namespace Data.NoSql.Interfaces
{
    public interface INoSQLDataProvider
    {
        Task WriteDocument(string collectionName, dynamic document);
        IEnumerable<dynamic> QueryDocuments(string query);
    }
}