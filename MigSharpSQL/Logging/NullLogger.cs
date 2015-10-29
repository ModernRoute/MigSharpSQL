using System;

namespace MigSharpSQL.Logging
{
    public class NullLogger : ILogger
    {
        public void Debug(string message, params object[] args) { }
        public void Error(string message, Exception ex) { }
        public void Info(string message) { }
        public void Info(string message, params object[] args) { }
    }
}
