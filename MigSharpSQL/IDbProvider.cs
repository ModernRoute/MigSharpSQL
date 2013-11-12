using System.Data;

namespace MigSharpSQL
{
    /// <summary>
    /// Database connection and migration metainfo provider.
    /// </summary>
    public interface IDbProvider
    {
        /// <summary>
        /// Gets whether transactions are supported or not.
        /// </summary>
        bool SupportsTransactions { get; }

        /// <summary>
        /// Gets unique provider name.
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

        /// <summary>
        /// Creates and opens connection database.
        /// </summary>
        /// <param name="connectionString">Connection string to connect to.</param>
        /// <returns>Database connection.</returns>
        /// <exception cref="ProviderException">When database 
        /// connection class class cannot be instantiated.</exception>
        IDbConnection CreateConnection(string connectionString);

        /// <summary>
        /// Does provider specific actions such as loading assembly and types.
        /// </summary>
        /// <exception cref="ProviderException">When assembly and type of database 
        /// connection cannot be taken.</exception>
        void Load();
    }
}
