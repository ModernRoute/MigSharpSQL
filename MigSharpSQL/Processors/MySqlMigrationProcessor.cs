using System;
using System.Data;

namespace MigSharpSQL.Processors
{
    /// <summary>
    /// MySql migration state processor.
    /// </summary>
    internal class MySqlMigrationProcessor : IDbMigrationStateProcessor
    {
        public bool SupportsTransactions
        {
            get { return true; }
        }

        public string Name
        {
            get { return "MySql"; }
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
        
        private const string viewExistsQuery = "SELECT COUNT(*) FROM information_schema.VIEWS " + 
            "AS info WHERE info.TABLE_SCHEMA = DATABASE() AND info.TABLE_NAME = '__MigrationState'";
        private const string getStateQuery = "SELECT `state`,`substate` FROM `__MigrationState`";
        private const string setStateQuery = "CREATE OR REPLACE VIEW `__MigrationState` " +
            "AS SELECT @state AS `state`, @substate AS `substate`";
        private const string stateParamName = "@state";
        private const string substateParamName = "@substate";
    }
}
