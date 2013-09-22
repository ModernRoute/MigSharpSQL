using System;
using System.Data.Common;

namespace MigSharpSQL.Providers
{
    internal class MySqlProvider : IDbProvider
    {
        public string Name
        {
            get
            {
                return "MySql.Data.MySqlClient";
            }
        }

        public string GetState(DbConnection connection)
        {
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM information_schema.VIEWS as info WHERE info.TABLE_SCHEMA = DATABASE() AND info.TABLE_NAME = '__MigrationState'";

                bool procedureExists = false;

                using (DbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        procedureExists = reader.GetInt32(0) != 0;
                    }
                }

                if (!procedureExists)
                {
                    return null;
                }
            }

            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT `state` FROM `__MigrationState`";

                using (DbDataReader reader = command.ExecuteReader())
                {
                    string state = null;

                    while (reader.Read())
                    {
                        state = reader.GetString(0);
                    }

                    return state;
                }
            }
        }

        public void SetState(DbConnection connection, DbTransaction transaction, string state)
        {
            using (DbCommand command = connection.CreateCommand())
            {   
                command.Connection = connection;
                command.Transaction = transaction;

                command.CommandText = "CREATE OR REPLACE VIEW `__MigrationState` AS SELECT @p AS `state`";

                DbParameter parameter = command.CreateParameter();
                parameter.ParameterName = "@p";
                parameter.DbType = System.Data.DbType.String;
                parameter.Value = state;

                command.Parameters.Add(parameter);

                command.Prepare();

                command.ExecuteNonQuery();
            }
        }

        public bool SupportsTransactions
        {
            get { return true; }
        }
    }
}
