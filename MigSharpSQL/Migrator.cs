using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigSharpSQL
{
    /// <summary>
    /// 
    /// </summary>
    public class Migrator
    {
        /// <summary>
        /// 
        /// </summary>
        private IDbProvider Provider
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string ProviderName
        {
            get
            {
                return Provider.Name;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private SortedDictionary<string,Migration> Migrations
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        private System.Data.Common.DbProviderFactory ProviderFactory
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string ConnectionString
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string MigrationsDirectory
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="providerName"></param>
        /// <param name="connectionString"></param>
        /// <param name="migrationsDirectory"></param>
        /// <exception cref="System.NotSupportedException"></exception>
        public Migrator(string providerName, string connectionString, string migrationsDirectory)
        {
            if (providerName == null)
            {
                throw new ArgumentNullException("provider");
            }

            if (connectionString == null)
            {
                throw new ArgumentNullException("connectionString");
            }

            if (migrationsDirectory == null)
            {
                throw new ArgumentNullException("migrationsDir");
            }

            Provider = DbProviderFactory.GetProvider(providerName);
            ProviderFactory = DbProviderFactories.GetFactory(ProviderName);
            ConnectionString = connectionString;
            MigrationsDirectory = migrationsDirectory;

            LoadMigrations(migrationsDirectory);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="migrationsDirectory"></param>
        private void LoadMigrations(string migrationsDirectory)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        public void MigrateTo(string state)
        {
            int indexState;
            string[] keys = Migrations.Keys.ToArray();

            indexState = GetStateIndex(state, keys, "The state `{0}` does not exist");

            using (DbConnection connection = OpenConnection())
            {
                int indexCurrentState = GetStateIndex(GetCurrentState(connection), keys, "Database state `{0}` is unknown");

                // ok, everything alright, let's migrate

                int diff = indexState - indexCurrentState;

                // We need to up 
                if (diff > 0)
                {
                    Up(connection, keys, indexCurrentState + 1, indexState);
                }

                // We need to down
                else if (diff < 0)
                {
                    Down(connection, keys, indexCurrentState, indexState + 1);
                }

                // Otherwise the current state is already approached
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="keys"></param>
        /// <param name="indexCurrentState"></param>
        /// <param name="p"></param>
        private void Down(DbConnection connection, string[] keys, int first, int last)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="keys"></param>
        /// <param name="p"></param>
        /// <param name="indexState"></param>
        private void Up(DbConnection connection, string[] keys, int first, int last)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        /// <param name="keys"></param>
        /// <param name="errorMsgTemplate"></param>
        /// <returns></returns>
        private static int GetStateIndex(string state, string[] keys, string errorMsgTemplate)
        {
            int indexState;

            if (state == null)
            {
                indexState = -1;
            }
            else
            {
                indexState = Array.BinarySearch<string>(keys, state);

                if (indexState < 0)
                {
                    throw new InvalidOperationException(string.Format(errorMsgTemplate, state));
                }
            }

            return indexState;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private DbConnection OpenConnection()
        {
            DbConnection connection = ProviderFactory.CreateConnection();
            connection.ConnectionString = ConnectionString;
            connection.Open();

            return connection;
        }

        /// <summary>
        /// 
        /// </summary>
        public void MigrateToLast()
        {
            string[] keys = Migrations.Keys.ToArray();

            if (keys.Length <= 0)
            {
                MigrateTo(null);
            }
            else
            {
                MigrateTo(keys[keys.Length - 1]);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private string GetCurrentState(DbConnection connection)
        {
            return Provider.GetState(connection);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetCurrentState()
        {
            using (DbConnection connection = OpenConnection())
            {
                return GetCurrentState(connection);
            }
        }
    }
}
