using System;
using System.Data.Common;

namespace MigSharpSQL.Providers
{
    internal class MySqlProvider : IDbProvider
    {
        public string Name
        {
            get
            {
                return "MySql.Data.MySqlClient";
            }
        }

        public string GetState(DbConnection connection)
        {
            throw new NotImplementedException();
        }

        public void SetState(DbConnection connection, DbTransaction transaction)
        {
            throw new NotImplementedException();
        }

        public bool SupportsTransactions
        {
            get { return true; }
        }
    }
}
