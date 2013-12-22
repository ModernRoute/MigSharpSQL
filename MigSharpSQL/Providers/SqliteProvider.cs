using MigSharpSQL.Resources;
using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

namespace MigSharpSQL.Providers
{
    /// <summary>
    /// Sqlite migration and database connection provider.
    /// </summary>
    internal class SqliteProvider : IDbProvider
    {
        /// <summary>
        /// Gets true. The Sqlite server does support transactions.
        /// </summary>
        public bool SupportsTransactions
        {
            get { return true; }
        }


        /// <summary>
        /// Gets 'Sqlite' string as provider name.
        /// </summary>
        public string Name
        {
            get { return "Sqlite"; }
        }

        /// <summary>
        /// Gets Sqlite database migration state.
        /// </summary>
        /// <param name="connection">Opened connection to Sqlite database server.</param>
        /// <param name="substate">When this method returns it contains the substate value.</param>
        /// <returns>Current database state. Null means initial state (empty database). 
        /// Substate value in that case is nevermind.</returns>
        /// <exception cref="System.Data.Common.DbException">When error occurs 
        /// during Sqlite database communication.</exception>
        public string GetState(IDbConnection connection, out int substate)
        {
            using (IDbCommand command = connection.CreateCommand())
            {
                command.CommandText = viewExistsQuery;

                bool viewExists = false;

                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        viewExists = reader.GetInt32(0) != 0;
                    }
                }

                if (!viewExists)
                {
                    substate = 0;
                    return null;
                }
            }

            using (IDbCommand command = connection.CreateCommand())
            {
                command.CommandText = getStateQuery;

                using (IDataReader reader = command.ExecuteReader())
                {
                    string state = null;
                    substate = 0;

                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                        {
                            state = reader.GetString(0);
                        }

                        substate = reader.GetInt32(1);
                    }

                    return state;
                }
            }
        }

        /// <summary>
        /// Sets Sqlite database migration state.
        /// </summary>
        /// <param name="connection">Opened connection to Sqlite database.</param>        
        /// <param name="transaction">Started transaction for <paramref name="connection"/>.</param>
        /// <param name="state">New state value.</param>
        /// <param name="substate">New substate value.</param>
        /// <exception cref="System.Data.Common.DbException">When error occurs 
        /// during Sqlite database communication.</exception>
        public void SetState(IDbConnection connection, IDbTransaction transaction, string state, int substate)
        {
            using (IDbCommand command = connection.CreateCommand())
            {
                command.Connection = connection;
                command.Transaction = transaction;

                command.CommandText = dropViewQuery;

                command.Prepare();

                command.ExecuteNonQuery();
            }

            using (IDbCommand command = connection.CreateCommand())
            {
                command.Connection = connection;
                command.Transaction = transaction;

                command.CommandText = CreateSetStateQuery(setStateQueryTemplate, state, substate);

                command.Prepare();

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Create 'set state' query from template.
        /// </summary>
        /// <param name="setStateQueryTemplate">Template of the 'set state' query.</param>
        /// <param name="state">State value.</param>
        /// <param name="substate">Substate value.</param>
        /// <returns>Created query.</returns>
        private string CreateSetStateQuery(string setStateQueryTemplate, string state, int substate)
        {
            return 
               string.Format(
                CultureInfo.InvariantCulture,
                setStateQueryTemplate,state == null ? "NULL" : "'" + SqliteEscape(state) + "'", 
                substate);
        }

        /// <summary>
        /// Escapes string to use it in Sqlite query.
        /// </summary>
        /// <param name="str">String to escape.</param>
        /// <returns>Escaped string.</returns>
        private string SqliteEscape(string str)
        {
            return str.Replace("\'","\'\'");
        }

        /// <summary>
        /// Creates and opens connection to Sqlite database.
        /// </summary>
        /// <param name="connectionString">Sqlite connection string to connect to.</param>
        /// <returns>Sqlite database connection.</returns>
        /// <exception cref="ProviderException">If Sqlite database connection class cannot be instantiated.</exception>
        public IDbConnection CreateConnection(string connectionString)
        {
            try
            {
                return (IDbConnection)Activator.CreateInstance(sqliteConnectionType, connectionString);
            }
            catch (Exception ex)
            {
                if (ex is ArgumentException || ex is NotSupportedException ||
                    ex is TargetInvocationException || ex is MethodAccessException ||
                    ex is MemberAccessException || ex is InvalidComObjectException ||
                    ex is MissingMethodException || ex is COMException || 
                    ex is TypeLoadException)
                {
                    throw new ProviderException(Strings.CannotInstanceDatabaseConnectionClass, ex);
                }

                throw;
            }
        }

        /// <summary>
        /// Loads System.Data.SQLite.dll assembly and stores the System.Data.SQLite.SQLiteConnection type.
        /// </summary>
        /// <exception cref="ProviderException">When assembly and type of Sqlite database 
        /// connection cannot be taken.</exception>
        public void Load()
        {
            try
            {
                Assembly assembly = Assembly.LoadFrom(sqliteAssemblyName);
                sqliteConnectionType = assembly.GetType(sqliteConnectionTypeName);
            }
            catch (Exception ex)
            {
                if (ex is FileNotFoundException || ex is FileLoadException ||
                    ex is BadImageFormatException || ex is SecurityException ||
                    ex is PathTooLongException)
                {
                    throw new ProviderException(
                        string.Format(Strings.CannotLoadAssemblyAndType, 
                        sqliteAssemblyName, sqliteConnectionTypeName), ex);
                }

                throw;
            }
        }

        private const string sqliteAssemblyName = "System.Data.SQLite.dll";
        private const string sqliteConnectionTypeName = "System.Data.SQLite.SQLiteConnection";

        private const string viewExistsQuery = "SELECT COUNT(*) FROM sqlite_master " + 
            "WHERE type = 'view' AND name = '__MigrationState'";
        private const string getStateQuery = "SELECT `state`,`substate` FROM `__MigrationState`";
        private const string dropViewQuery = "DROP VIEW IF EXISTS `__MigrationState`";
        private const string setStateQueryTemplate = "CREATE VIEW `__MigrationState` " +
            "AS SELECT {0} AS `state`, {1} AS `substate`";
        private const string stateParamName = "@state";
        private const string substateParamName = "@substate";

        private Type sqliteConnectionType;
    }
}
