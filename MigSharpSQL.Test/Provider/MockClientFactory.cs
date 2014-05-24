using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigSharpSQL.Test.Provider
{
    public sealed class MockClientFactory : System.Data.Common.DbProviderFactory
    {
        public static readonly MockClientFactory Instance = new MockClientFactory();

        private MockClientFactory()
        {

        }

        public override DbConnection CreateConnection()
        {
            return new MockDbConnection();
        }
    }
}
