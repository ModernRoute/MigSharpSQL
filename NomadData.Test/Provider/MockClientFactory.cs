using System.Data.Common;

namespace ModernRoute.NomadData.Test.Provider
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
