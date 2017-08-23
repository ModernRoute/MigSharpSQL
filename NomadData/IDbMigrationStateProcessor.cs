using System.Collections.Generic;
using System.Data;

namespace ModernRoute.NomadData
{
    public interface IDbMigrationStateProcessor
    {
        bool SupportsTransactions { get; }

        string Name { get; }

        string GetStateObsolete(IDbConnection connection, out int substate);

        void SetState(IDbConnection connection, IDbTransaction transaction, string state, int substate, bool isUp);

        IEnumerable<MigrationHistoryItem> EnumerateHistory(IDbConnection connection);

        bool CheckDeprecated(IDbConnection connection, IDbTransaction transaction);

        void AddHistory(IDbConnection connection, IDbTransaction transaction, IEnumerable<SimpleMigrationHistoryItem> migrations);

        string GetState(IDbConnection connection, IDbTransaction transaction, out int substate);
    }
}
