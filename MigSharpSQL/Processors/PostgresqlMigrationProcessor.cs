using System.Data;
using System.Globalization;

namespace MigSharpSQL.Processors
{
    public class PostgreSqlMigrationProcessor : IDbMigrationStateProcessor
    {
        private static string Escape(string value)
        {
            return value?.Replace("\'", "\'\'");
        }

        private static string Nullable(string value)
        {
            if (value == null)
            {
                return "NULL";
            }

            return string.Concat("\'", value, "\'");
        }

        public string Name
        {
            get
            {
                return "PostgreSql";
            }
        }

        public bool SupportsTransactions
        {
            get
            {
                return true;
            }
        }

        public string GetState(IDbConnection connection, out int substate)
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

        public void SetState(IDbConnection connection, IDbTransaction transaction, string state, int substate)
        {
            using (IDbCommand command = connection.CreateCommand())
            {
                command.Connection = connection;
                command.Transaction = transaction;

                command.CommandText = string.Format(CultureInfo.InvariantCulture,
                    _SetStateQuery, Nullable(Escape(state)), substate);

                command.Prepare();

                command.ExecuteNonQuery();
            }
        }

        private const string _ViewExistsQuery = "SELECT COUNT(*) from \"information_schema\".\"views\" " + 
            "WHERE \"table_schema\" = ANY(current_schemas(FALSE)) AND \"table_name\" = '__MigrationState';";
        private const string _GetStateQuery = "SELECT \"state\",\"substate\" FROM \"__MigrationState\";";
        private const string _SetStateQuery = "CREATE OR REPLACE VIEW \"__MigrationState\" AS " + 
            "SELECT CAST({0} AS VARCHAR(16)) AS \"state\", CAST({1} AS INT) AS \"substate\";";
    }
}
