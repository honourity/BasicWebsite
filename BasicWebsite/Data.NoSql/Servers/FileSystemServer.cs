using System;
using System.IO;
using System.Threading.Tasks;
using Data.NoSql.Interfaces;

namespace Data.NoSql.Servers
{
    public class FileSystemServer : INoSQLDataProvider
    {
        private const string DRIVE_LETTER_TO_USE = "C";
        private const string ROOT_DIRECTORY_NAME = "Logs";

        public System.Collections.Generic.IEnumerable<dynamic> QueryDocuments(string query)
        {
            throw new NotImplementedException();
        }

        public Task WriteDocument(string collectionName, dynamic document)
        {
            Task task = new Task(() => WriteDocumentTask(collectionName, document));
            task.Start();
            return task;
        }

        private void WriteDocumentTask(string collectionName, dynamic document)
        {
            //System.Web.HttpContext.Current.Server.MapPath(".").Split(':')[0]; this ends up as drive letter of website.
            // But cant always assume HttpContext.Current exists
            FileInfo file = new FileInfo(string.Format("{0}:\\{1}\\{2}\\{3}.json", DRIVE_LETTER_TO_USE, ROOT_DIRECTORY_NAME, collectionName.ToString(), Guid.NewGuid().ToString()));
            file.Directory.Create();
            File.WriteAllText(file.FullName, ((object)document).ToJSON());
        }
    }
}
