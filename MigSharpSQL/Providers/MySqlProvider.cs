using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public void SetState(DbConnection connection)
        {
            throw new NotImplementedException();
        }

        public bool SupportsTransactions
        {
            get { return true; }
        }
    }
}
