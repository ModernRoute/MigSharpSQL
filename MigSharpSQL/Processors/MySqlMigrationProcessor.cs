using System;
using System.Collections.Generic;
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

        public string GetStateObsolete(IDbConnection connection, out int substate)
        {
            using (IDbCommand command = connection.CreateCommand())
            {
                command.CommandText = _ViewExistsQuery;

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
                command.CommandText = _GetStateQuery;

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

        public void SetState(IDbConnection connection, IDbTransaction transaction, string state, int substate, bool isUp)
        {
            using (IDbCommand command = connection.CreateCommand())
            {
                command.Connection = connection;
                command.Transaction = transaction;

                command.CommandText = _SetStateQuery;

                IDbDataParameter parameter = command.CreateParameter();
                parameter.ParameterName = _StateParamName;
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
                parameter.ParameterName = _SubstateParamName;
                parameter.DbType = System.Data.DbType.Int32;
                parameter.Value = substate;

                command.Parameters.Add(parameter);

                command.Prepare();

                command.ExecuteNonQuery();
            }
        }

        public IEnumerable<MigrationHistoryItem> EnumerateHistory(IDbConnection connection)
        {
            throw new NotImplementedException();
        }

        public bool CheckDeprecated(IDbConnection connection, IDbTransaction transaction)
        {
            throw new NotImplementedException();
        }

        public void AddHistory(IDbConnection connection, IDbTransaction transaction, IEnumerable<SimpleMigrationHistoryItem> migrations)
        {
            throw new NotImplementedException();
        }

        public string GetState(IDbConnection connection, IDbTransaction transaction, out int substate)
        {
            throw new NotImplementedException();
        }

        private const string _ViewExistsQuery = "SELECT COUNT(*) FROM information_schema.VIEWS " + 
            "AS info WHERE info.TABLE_SCHEMA = DATABASE() AND info.TABLE_NAME = '__MigrationState'";
        private const string _GetStateQuery = "SELECT `state`,`substate` FROM `__MigrationState`";
        private const string _SetStateQuery = "CREATE OR REPLACE VIEW `__MigrationState` " +
            "AS SELECT @state AS `state`, @substate AS `substate`";
        private const string _StateParamName = "@state";
        private const string _SubstateParamName = "@substate";
    }
}
