using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TACT.Net;

namespace DBCD.Providers
{
    class TACTDBCProvider : IDBCProvider
    {
        private TACTRepo tactRepo;

        public TACTDBCProvider(TACTRepo tactRepo) 
        {
            this.tactRepo = tactRepo;
        }

        public Stream StreamForTableName(string tableName, string build)
        {
            uint fileDataID = 0;

            // Replace this for Listfile.csv load at one point or something? Would be nice if DBCD supported loading things by FileDataID at one point but not sure how that'd work definition wise...
            switch (tableName)
            {
                case "Map":
                    fileDataID = 1349477;
                    break;
                default:
                    throw new Exception("Don't know FileDataID for DBC " + tableName + ", add to switch please or implement listfile.csv reading. <3");
            }

            var stream = tactRepo.RootFile.OpenFile(fileDataID, tactRepo);
            if(stream == null)
            {
                throw new Exception("Unable to open file with fileDataID " + fileDataID);
            }

            return stream;
        }
    }
}
