using WoWTools.MinimapExtractTACT;

namespace DBCD.Providers
{
    class CASCDBCProvider : IDBCProvider
    {
        public Stream StreamForTableName(string tableName, string build)
        {
            uint fileDataID = 0;

            switch (tableName)
            {
                case "Map":
                    fileDataID = 1349477;
                    break;
                default:
                    throw new Exception("Don't know FileDataID for DBC " + tableName + ", add to switch please or implement listfile.csv reading. <3");
            }

            var stream = Program.tactRepo.RootFile.OpenFile(fileDataID, Program.tactRepo);
            if (stream == null)
            {
                throw new Exception("Unable to open file with fileDataID " + fileDataID);
            }

            return stream;
        }
    }
}
