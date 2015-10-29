using System;
using System.Data;
using System.Data.Common;

namespace MigSharpSQL.Test.Provider
{
    class MockDbTransaction : DbTransaction
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
                throw new ArgumentNullException(nameof(connection));
            }

            this.connection = connection;
            MigrationState = connection.MigrationState;
            MigrationSubstate = connection.MigrationSubstate;
        }

        public override void Commit()
        {
            connection.MigrationState = MigrationState;
            connection.MigrationSubstate = MigrationSubstate;
        }

        protected override DbConnection DbConnection
        {
            get { return connection; }
        }

        public override IsolationLevel IsolationLevel
        {
            get { throw new NotSupportedException(); }
        }

        public override void Rollback()
        {
            
        }
    }
}
