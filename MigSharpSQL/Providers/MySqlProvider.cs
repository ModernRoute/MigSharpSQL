using MigSharpSQL.Resources;
using System;
using System.Data;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

namespace MigSharpSQL.Providers
{
    /// <summary>
    /// MySql migration and database connection provider.
    /// </summary>
    internal class MySqlProvider : IDbProvider
    {
        /// <summary>
        /// Gets true. The MySql server does support transactions.
        /// </summary>
        public bool SupportsTransactions
        {
            get { return true; }
        }


        /// <summary>
        /// Gets 'MySql' string as provider name.
        /// </summary>
        public string Name
        {
            get { return "MySql"; }
        }

        /// <summary>
        /// Gets MySql database migration state.
        /// </summary>
        /// <param name="connection">Opened connection to MySql database server.</param>
        /// <param name="substate">When this method returns it contains the substate value.</param>
        /// <returns>Current database state. Null means initial state (empty database). 
        /// Substate value in that case is nevermind.</returns>
        /// <exception cref="System.Data.Common.DbException">When error occurs 
        /// during MySql database communication.</exception>
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
        /// Sets MySql database migration state.
        /// </summary>
        /// <param name="connection">Opened connection to MySql database server.</param>        
        /// <param name="transaction">Started transaction for <paramref name="connection"/>.</param>
        /// <param name="state">New state value.</param>
        /// <param name="substate">New substate value.</param>
        /// <exception cref="System.Data.Common.DbException">When error occurs 
        /// during MySql database communication.</exception>
        public void SetState(IDbConnection connection, IDbTransaction transaction, string state, int substate)
        {
            using (IDbCommand command = connection.CreateCommand())
            {
                command.Connection = connection;
                command.Transaction = transaction;

                command.CommandText = setStateQuery;

                IDbDataParameter parameter = command.CreateParameter();
                parameter.ParameterName = stateParamName;
                parameter.DbType = System.Data.DbType.String;

                if (state == null)
                {
                    parameter.Value = DBNull.Value;
                }
                else
                {
                    parameter.Value = state;
                }

                command.Parameters.Add(parameter);

                parameter = command.CreateParameter();
                parameter.ParameterName = substateParamName;
                parameter.DbType = System.Data.DbType.Int32;
                parameter.Value = substate;

                command.Parameters.Add(parameter);

                command.Prepare();

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Creates and opens connection to MySql database server.
        /// </summary>
        /// <param name="connectionString">MySql connection string to connect to.</param>
        /// <returns>MySql database connection.</returns>
        /// <exception cref="ProviderException">If MySql database connection class cannot be instantiated.</exception>
        public IDbConnection CreateConnection(string connectionString)
        {
            try
            {
                return (IDbConnection)Activator.CreateInstance(mySqlConnectionType, connectionString);
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
        /// Loads MySql.Data.dll assembly and stores the MySql.Data.MySqlClient.MySqlConnection type.
        /// </summary>
        /// <exception cref="ProviderException">When assembly and type of MySql database 
        /// connection cannot be taken.</exception>
        public void Load()
        {
            try
            {
                Assembly assembly = Assembly.LoadFrom(mySqlAssemblyName);
                mySqlConnectionType = assembly.GetType(mySqlConnectionTypeName);
            }
            catch (Exception ex)
            {
                if (ex is FileNotFoundException || ex is FileLoadException ||
                    ex is BadImageFormatException || ex is SecurityException ||
                    ex is PathTooLongException)
                {
                    throw new ProviderException(
                        string.Format(Strings.CannotLoadAssemblyAndType, 
                        mySqlAssemblyName, mySqlConnectionTypeName), ex);
                }

                throw;
            }
        }

        private const string mySqlAssemblyName = "MySql.Data.dll";
        private const string mySqlConnectionTypeName = "MySql.Data.MySqlClient.MySqlConnection";

        private const string viewExistsQuery = "SELECT COUNT(*) FROM information_schema.VIEWS " + 
            "AS info WHERE info.TABLE_SCHEMA = DATABASE() AND info.TABLE_NAME = '__MigrationState'";
        private const string getStateQuery = "SELECT `state`,`substate` FROM `__MigrationState`";        
        private const string setStateQuery = "CREATE OR REPLACE VIEW `__MigrationState` " +
            "AS SELECT @state AS `state`, @substate AS `substate`";
        private const string stateParamName = "@state";
        private const string substateParamName = "@substate";

        private Type mySqlConnectionType;
    }
}
