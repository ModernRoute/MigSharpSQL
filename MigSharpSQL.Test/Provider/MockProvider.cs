using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigSharpSQL.Test.Provider
{
    /// <summary>
    /// 
    /// </summary>
    class MockProvider : IDbProvider
    {
        private const string providerName = "Mock";

        public MockProvider(bool supportsTransactions)
        {
            SupportsTransactions = supportsTransactions;
        }

        public string GetState(IDbConnection connection, out int substate)
        {
            MockDbConnection conn = ((MockDbConnection)connection);

            substate = conn.MigrationSubstate;
            return conn.MigrationState;
        }

        public void SetState(IDbConnection connection, IDbTransaction transaction, string state, int substate)
        {
            if (transaction != null)
            {
                MockDbTransaction tran = (MockDbTransaction)transaction;

                tran.MigrationState = state;
                tran.MigrationSubstate = substate;
            }
            else
            {
                MockDbConnection conn = (MockDbConnection)connection;

                conn.MigrationState = state;
                conn.MigrationSubstate = substate;
            }
        }

        public bool SupportsTransactions
        {
            get;
            private set;
        }

        public string Name
        {
            get { return providerName; }
        }

        public IDbConnection CreateConnection(string connectionString)
        {
            return new MockDbConnection(connectionString);
        }

        public void Load()
        {
            
        }
    }
}
