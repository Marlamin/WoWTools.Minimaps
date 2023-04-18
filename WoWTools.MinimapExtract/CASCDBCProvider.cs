using System;
using System.IO;
using WoWTools.MinimapExtract;

namespace DBCD.Providers
{
    class CASCDBCProvider : IDBCProvider
    {
        public Stream StreamForTableName(string tableName, string build)
        {
            int fileDataID = 0;

            switch (tableName)
            {
                case "Map":
                    fileDataID = 1349477;
                    break;
                default:
                    throw new Exception("Don't know FileDataID for DBC " + tableName + ", add to switch please or implement listfile.csv reading. <3");
            }

            var stream = Program.cascHandler.OpenFile(fileDataID);
            if (stream == null)
            {
                throw new Exception("Unable to open file with fileDataID " + fileDataID);
            }

            return stream;
        }
    }
}
