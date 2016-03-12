using System;
using System.Data;
using System.Data.Common;

namespace MigSharpSQL.Test.Provider
{
    class MockDbTransaction : DbTransaction
    {
        private bool _Disposed;
        private MockDbConnection _Connection;

        public MockDbTransaction(MockDbConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            _Connection = connection;
            connection.MockDatabase.BeginTransaction();
        }

        public override void Commit()
        {
            _Connection.MockDatabase.Commit();
        }

        protected override DbConnection DbConnection
        {
            get { return _Connection; }
        }

        public override IsolationLevel IsolationLevel
        {
            get { throw new NotSupportedException(); }
        }

        public override void Rollback()
        {
            _Connection.MockDatabase.Rollback();
        }

        protected override void Dispose(bool disposing)
        {
            if (_Disposed)
            {
                return;
            }

            if (disposing)
            {
                _Connection.MockDatabase.EndTransaction();
                _Disposed = true;
            }            

            base.Dispose(disposing);
        }
    }
}
