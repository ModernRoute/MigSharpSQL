using MigSharpSQL.Test.Helpers;
using System;
using System.Data;
using System.Data.Common;

namespace MigSharpSQL.Test.Provider
{
    class MockDbConnection : DbConnection
    {
        private const string dbName = "DATABASE";

        private bool opened = false;

        public MockDatabase MockDatabase
        {
            get;
            private set;
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            CheckOpened();

            return new MockDbTransaction(this);
        }

        public override void ChangeDatabase(string databaseName)
        {
            throw new NotSupportedException();
        }

        public void CheckNotOpened()
        {
            if (opened)
            {
                throw new MockDbException("Connection to database is opened.");
            }
        }

        public void CheckOpened()
        {
            if (!opened)
            {
                throw new MockDbException("Connection to database is not opened.");
            }
        }

        public override void Close()
        {
            opened = false;
            MockDatabase = null;
        }

        private string _ConnectionString;
        public override string ConnectionString
        {
            get
            {
                return _ConnectionString;
            }
            set
            {
                CheckNotOpened();

                _ConnectionString = value;
            }
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
            MockDatabase = MockDatabase.GetInstance(_ConnectionString);
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
