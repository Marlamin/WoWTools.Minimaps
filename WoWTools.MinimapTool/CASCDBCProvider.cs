using WoWTools.MinimapTool;

namespace DBCD.Providers
{
    class CASCDBCProvider : IDBCProvider
    {
        public Stream StreamForTableName(string tableName, string build)
        {
            uint fileDataID = GetFDIDForDB2(tableName);

            if(!TACTProcessor.TACTRepo.RootFile.ContainsFileId(fileDataID))
                fileDataID = GetFDIDForDBC(tableName);

            var stream = TACTProcessor.TACTRepo.RootFile.OpenFile(fileDataID, TACTProcessor.TACTRepo);
            if (stream == null)
            {
                throw new Exception("Unable to open file with fileDataID " + fileDataID);
            }

            return stream;
        }

        private static uint GetFDIDForDB2(string tableName)
        {
            switch (tableName)
            {
                case "Map":
                    return 1349477;
                default:
                    throw new Exception("Don't know FileDataID for DB2 " + tableName + ", add to switch please or implement listfile.csv reading. <3");
            }
        }

        private static uint GetFDIDForDBC(string tableName)
        {
            switch (tableName)
            {
                case "Map":
                    return 841636;
                default:
                    throw new Exception("Don't know FileDataID for DBC " + tableName + ", add to switch please or implement listfile.csv reading. <3");
            }
        }
    }
}
