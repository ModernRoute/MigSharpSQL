using System;
using System.Data;

namespace MigSharpSQL.Test.Provider
{
    class MockDbTransaction : IDbTransaction
    {
        private MockDbConnection connection;

        public string MigrationState
        {
            get;
            set;
        }

        public int MigrationSubstate
        {
            get;
            set;
        }

        public MockDbTransaction(MockDbConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            this.connection = connection;
            MigrationState = connection.MigrationState;
            MigrationSubstate = connection.MigrationSubstate;
        }

        public void Commit()
        {
            connection.MigrationState = MigrationState;
            connection.MigrationSubstate = MigrationSubstate;
        }

        public IDbConnection Connection
        {
            get { return connection; }
        }

        public IsolationLevel IsolationLevel
        {
            get { throw new NotSupportedException(); }
        }

        public void Rollback()
        {
            
        }

        public void Dispose()
        {
           
        }
    }
}
