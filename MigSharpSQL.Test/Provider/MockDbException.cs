using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MigSharpSQL.Test.Provider
{
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
