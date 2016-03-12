using MigSharpSQL.Test.Helpers;
using MigSharpSQL.Test.Provider;
using System.Collections.Generic;
using System.Data;

namespace MigSharpSQL.Test
{
    class MockProcessor : IDbMigrationStateProcessor
    {
        public const string ProcessorName = "Mock";

        public MockProcessor(bool supportsTransactions)
        {
            SupportsTransactions = supportsTransactions;
        }

        public string GetStateObsolete(IDbConnection connection, out int substate)
        {
            substate = 0;
            return null;
        }

        public void SetState(IDbConnection connection, IDbTransaction transaction, string state, int substate, bool isUp)
        {
            MockDatabase mockDatabase = GetMockDatabase(connection, transaction);

            mockDatabase.ToNewState(state, substate, isUp);
        }

        private static MockDatabase GetMockDatabase(IDbConnection connection, IDbTransaction transaction)
        {
            MockDbConnection conn;

            if (transaction != null)
            {
                MockDbTransaction tran = transaction as MockDbTransaction;
                conn = tran.Connection as MockDbConnection;
            }
            else
            {
                conn = connection as MockDbConnection;
            }

            return conn.MockDatabase;
        }

        public IEnumerable<MigrationHistoryItem> EnumerateHistory(IDbConnection connection)
        {
            return GetMockDatabase(connection, null).GetHistory();
        }

        public bool CheckDeprecated(IDbConnection connection, IDbTransaction transaction)
        {
            return false;
        }

        public void AddHistory(IDbConnection connection, IDbTransaction transaction, IEnumerable<SimpleMigrationHistoryItem> migrations)
        {
            GetMockDatabase(connection, transaction).AddHistory(migrations);
        }

        public string GetState(IDbConnection connection, IDbTransaction transaction, out int substate)
        {
            return GetMockDatabase(connection, transaction).GetState(out substate);
        }

        public bool SupportsTransactions
        {
            get;
            private set;
        }

        public string Name
        {
            get { return ProcessorName; }
        }
    }
}
