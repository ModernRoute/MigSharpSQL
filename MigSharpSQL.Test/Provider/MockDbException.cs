using System;
using System.Data.Common;
using System.Runtime.Serialization;

namespace MigSharpSQL.Test.Provider
{
    [Serializable]
    class MockDbException : DbException
    {
        public MockDbException()
            : base()
        {

        }

        public MockDbException(string message)
            : base(message)
        {

        }

        public MockDbException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        public MockDbException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        public MockDbException(string message, int errorCode)
            : base(message, errorCode)
        {

        }
    }
}
