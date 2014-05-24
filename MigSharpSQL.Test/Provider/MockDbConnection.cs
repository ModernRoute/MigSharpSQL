using System;
using System.Data;
using System.Data.Common;

namespace MigSharpSQL.Test.Provider
{
    class MockDbConnection : DbConnection
    {
        private const string dbName = "DATABASE";

        private bool opened = false;

        #region Migration metadata

        public static string MigrationStateStatic
        {
            get;
            set;
        }

        public string MigrationState
        {
            get
            {
                return MigrationStateStatic;
            }
            set
            {
                MigrationStateStatic = value;
            }
        }

        public static int MigrationSubstateStatic
        {
            get;
            set;
        }

        public int MigrationSubstate
        {
            get
            {
                return MigrationSubstateStatic;
            }
            set
            {
                MigrationSubstateStatic = value;
            }
        }
        #endregion

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            CheckOpened();

            return new MockDbTransaction(this);
        }

        public override void ChangeDatabase(string databaseName)
        {
            throw new NotSupportedException();
        }

        public void CheckOpened()
        {
            if (!opened)
            {
                throw new MockDbException("Connection to database is not opened");
            }
        }

        public override void Close()
        {
            opened = false;
        }

        public override string ConnectionString
        {
            get;
            set;
        }

        protected override DbCommand CreateDbCommand()
        {
            return new MockDbCommand(this);
        }

        public override string DataSource
        {
            get { throw new NotImplementedException(); }
        }

        public override string Database
        {
            get { return dbName; }
        }

        public override void Open()
        {
            opened = true;
        }

        public override string ServerVersion
        {
            get { throw new NotImplementedException(); }
        }

        public override ConnectionState State
        {
            get { return opened ? ConnectionState.Open : ConnectionState.Closed; }
        }
    }
}
