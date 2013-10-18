using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigSharpSQL.Test.Provider
{
    class MockDbCommand : IDbCommand
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

        public void Cancel()
        {
            throw new NotSupportedException();            
        }

        public string CommandText
        {
            get;
            set;
        }

        public int CommandTimeout
        {
            get;
            set;
        }

        public CommandType CommandType
        {
            get;
            set;
        }

        public IDbConnection Connection
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

        public IDbDataParameter CreateParameter()
        {
            throw new NotSupportedException();
        }

        public int ExecuteNonQuery()
        {
            connection.CheckOpened();

            if (CommandText != null && CommandText.Trim().ToUpper() == goodQuery)
            {
                return 1;
            }

            throw new MockDbException(string.Format("Expected exception for query `{0}`", CommandText));
        }

        public IDataReader ExecuteReader(CommandBehavior behavior)
        {
            throw new NotSupportedException();
        }

        public IDataReader ExecuteReader()
        {
            throw new NotSupportedException();
        }

        public object ExecuteScalar()
        {
            throw new NotSupportedException();
        }

        public IDataParameterCollection Parameters
        {
            get { throw new NotSupportedException(); }
        }

        public void Prepare()
        {
            throw new NotSupportedException();
        }

        public IDbTransaction Transaction
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

        public UpdateRowSource UpdatedRowSource
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

        public void Dispose()
        {
            
        }
    }
}
