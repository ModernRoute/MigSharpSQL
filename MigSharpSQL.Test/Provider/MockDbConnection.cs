﻿using System;
using System.Data;

namespace MigSharpSQL.Test.Provider
{
    class MockDbConnection : IDbConnection
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

        public MockDbConnection(string connectionString)
        {
            if (connectionString == null)
            {
                throw new ArgumentNullException("connectionString");
            }
        }

        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            throw new NotSupportedException();
        }

        public IDbTransaction BeginTransaction()
        {
            CheckOpened();

            return new MockDbTransaction(this);
        }

        public void ChangeDatabase(string databaseName)
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

        public void Close()
        {
            opened = false;
        }

        public string ConnectionString
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public int ConnectionTimeout
        {
            get { throw new NotSupportedException(); }
        }

        public IDbCommand CreateCommand()
        {
            return new MockDbCommand(this);
        }

        public string Database
        {
            get { return dbName; }
        }

        public void Open()
        {
            opened = true;
        }

        public ConnectionState State
        {
            get { return opened ? ConnectionState.Open : ConnectionState.Closed; }
        }

        public void Dispose()
        {
            Close();
        }
    }
}