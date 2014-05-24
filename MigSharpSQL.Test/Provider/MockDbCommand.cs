using System;
using System.Data;
using System.Data.Common;

namespace MigSharpSQL.Test.Provider
{
    class MockDbCommand : DbCommand
    {
        private MockDbConnection connection;
        private MockDbTransaction transaction;
        private const string goodQuery = "GOOD";

        public MockDbCommand(MockDbConnection connection)
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            this.connection = connection;
        }


        public override void Cancel()
        {
            throw new NotSupportedException();
        }

        public override string CommandText
        {
            get;
            set;
        }

        public override int CommandTimeout
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

        public override CommandType CommandType
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

        protected override DbParameter CreateDbParameter()
        {
            throw new NotSupportedException();
        }

        protected override DbConnection DbConnection
        {
            get
            {
                return connection;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }

                if (value is MockDbConnection)
                {
                    connection = (MockDbConnection)value;
                }
                else
                {
                    throw new InvalidCastException("Value must be MockDbConnection type");
                }
            }
        }

        protected override DbParameterCollection DbParameterCollection
        {
            get { throw new NotSupportedException(); }
        }

        protected override DbTransaction DbTransaction
        {
            get
            {
                return transaction;
            }
            set
            {
                if (value == null)
                {
                    transaction = null;
                    return;
                }

                if (value is MockDbTransaction)
                {
                    transaction = (MockDbTransaction)value;
                }
                else
                {
                    throw new InvalidCastException("Value must be MockDbTransaction type");
                }
            }
        }

        public override bool DesignTimeVisible
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

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            throw new NotSupportedException();
        }

        public override int ExecuteNonQuery()
        {
            connection.CheckOpened();

            if (CommandText != null && CommandText.Trim().ToUpper() == goodQuery)
            {
                return 1;
            }

            throw new MockDbException(string.Format("Expected exception for query `{0}`", CommandText));
        }

        public override object ExecuteScalar()
        {
            throw new NotSupportedException();
        }

        public override void Prepare()
        {
            throw new NotSupportedException();
        }

        public override UpdateRowSource UpdatedRowSource
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
    }
}
