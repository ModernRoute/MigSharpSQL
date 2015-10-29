using System;

namespace MigSharpSQL.Logging
{
    public interface ILogger
    {
        void Info(string message);
        void Info(string message, params object[] args);
        void Debug(string message, params object[] args);
        void Error(string message, Exception ex);
    }
}
