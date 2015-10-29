using System.Data;
using System.Globalization;

namespace MigSharpSQL.Processors
{
    /// <summary>
    /// Sqlite migration state processor.
    /// </summary>
    internal class SqliteMigrationProcessor : IDbMigrationStateProcessor
    {
        public bool SupportsTransactions
        {
            get { return true; }
        }

        public string Name
        {
            get { return "Sqlite"; }
        }

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
             
        private const string viewExistsQuery = "SELECT COUNT(*) FROM sqlite_master " + 
            "WHERE type = 'view' AND name = '__MigrationState'";
        private const string getStateQuery = "SELECT `state`,`substate` FROM `__MigrationState`";
        private const string dropViewQuery = "DROP VIEW IF EXISTS `__MigrationState`";
        private const string setStateQueryTemplate = "CREATE VIEW `__MigrationState` " +
            "AS SELECT {0} AS `state`, {1} AS `substate`";
        private const string stateParamName = "@state";
        private const string substateParamName = "@substate";
    }
}
