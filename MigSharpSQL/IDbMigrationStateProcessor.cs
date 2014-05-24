using System.Data;

namespace MigSharpSQL
{
    /// <summary>
    /// Migration state processor.
    /// </summary>
    public interface IDbMigrationStateProcessor
    {
        /// <summary>
        /// Gets whether transactions are supported or not.
        /// </summary>
        bool SupportsTransactions { get; }

        /// <summary>
        /// Gets unique processor name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets database migration state.
        /// </summary>
        /// <param name="connection">Opened connection to database.</param>
        /// <param name="substate">When this method returns it contains the substate value.</param>
        /// <returns>Current database state. Null means initial state (empty database). 
        /// Substate value in that case is nevermind.</returns>
        /// <exception cref="System.Data.Common.DbException">When error occurs 
        /// during database communication.</exception>
        string GetState(IDbConnection connection, out int substate);

        /// <summary>
        /// Sets database migration state.
        /// </summary>
        /// <param name="connection">Opened connection to database.</param>        
        /// <param name="transaction">Started transaction for <paramref name="connection"/>.</param>
        /// <param name="state">New state value.</param>
        /// <param name="substate">New substate value.</param>
        /// <exception cref="System.Data.Common.DbException">When error occurs 
        /// during database communication.</exception>        
        void SetState(IDbConnection connection, IDbTransaction transaction, string state, int substate);
    }
}
