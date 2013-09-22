using NLog;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MigSharpSQL
{
    /// <summary>
    /// 
    /// </summary>
    public class Migrator
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        // yyyy-MM-dd_HH-mm_up.sql
        private readonly Regex scriptFilenamePattern = new Regex(@"(\d{4}-\d{2}-\d{2}_\d{2}-\d{2})_(up|down)\.sql",RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
                throw new ArgumentNullException("migrationsDirectory");
            }

            Provider = DbProviderFactory.GetProvider(providerName);
            ProviderFactory = DbProviderFactories.GetFactory(ProviderName);
            ConnectionString = connectionString;
            MigrationsDirectory = migrationsDirectory;
            Migrations = new SortedDictionary<string, Migration>();

            LoadMigrations();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="migrationsDirectory"></param>
        private void LoadMigrations()
        {
            DirectoryInfo dirInfo = new DirectoryInfo(MigrationsDirectory);

            Migrations.Clear();

            foreach (FileInfo fileInfo in dirInfo.EnumerateFiles())
            {
                Match match = scriptFilenamePattern.Match(fileInfo.Name);

                if (match.Success)
                {
                    string migrationName = match.Groups[1].Value;

                    Migration migration = GetMigration(migrationName);

                    if (string.Equals("up",match.Groups[2].Value,StringComparison.OrdinalIgnoreCase))
                    {
                        migration.UpScriptFullPath = fileInfo.FullName;
                    }
                    else
                    {
                        migration.DownScriptFullPath = fileInfo.FullName;
                    }
                }
            }

            foreach (Migration migration in Migrations.Values)
            {
                if (migration.UpScriptFullPath == null || migration.DownScriptFullPath == null)
                {
                    throw new InvalidDataException(
                        string.Format(
                            "Migration script `{0}_{1}.sql` is absent.",
                            migration.Name,
                            migration.UpScriptFullPath == null ? "up": "down")
                        );
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="migrationName"></param>
        /// <returns></returns>
        private Migration GetMigration(string migrationName)
        {
            Migration migration;

            if (!Migrations.ContainsKey(migrationName))
            {
                migration = new Migration(migrationName);
                Migrations.Add(migration.Name, migration);
            }
            else
            {
                migration = Migrations[migrationName];
            }

            return migration;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        public void MigrateTo(string state)
        {
            logger.Info("Migration started");

            int indexState;
            string[] keys = Migrations.Keys.ToArray();

            indexState = GetStateIndex(state, keys, "The state `{0}` does not exist");

            using (DbConnection connection = OpenConnection())
            {
                logger.Info("Figuring out the current database state");

                string currentState = GetCurrentState(connection);

                logger.Info("The current database state: {0}", currentState == null ? "initial" : currentState); // TODO: initial

                int indexCurrentState = GetStateIndex(currentState, keys, "The database state `{0}` is unknown");

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
                else
                {
                    logger.Info("The database is already at specified state. No action required");
                }
            }

            logger.Info("Migration completed successfully");
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
            logger.Info("Performing the downgrading scripts {0}...{1} has been started", keys[first], keys[last]);

            if (Provider.SupportsTransactions)
            {
                for (int i = first; i >= last; i--)
                {
                    using (DbTransaction transaction = connection.BeginTransaction())
                    {
                        RunScript(transaction, connection, Migrations[keys[i]].DownScriptFullPath);
                        Provider.SetState(connection, transaction, keys[i]);

                        transaction.Commit();
                    }
                }
            }
            else
            {
                for (int i = first; i >= last; i--)
                {
                    RunScript(null, connection, Migrations[keys[i]].DownScriptFullPath);

                    Provider.SetState(connection, null, keys[i]);
                }
            }
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
            logger.Info("Performing the upgrading scripts {0}...{1} has been started", keys[first], keys[last]);

            if (Provider.SupportsTransactions)
            {
                for (int i = first; i <= last; i++)
                {
                    using (DbTransaction transaction = connection.BeginTransaction())
                    {
                        RunScript(transaction, connection, Migrations[keys[i]].UpScriptFullPath);
                        Provider.SetState(connection, transaction, keys[i]);

                        transaction.Commit();
                    }
                }
            }
            else
            {
                for (int i = first; i <= last; i++)
                {
                    RunScript(null, connection, Migrations[keys[i]].UpScriptFullPath);

                    Provider.SetState(connection, null, keys[i]);
                }
            }
        }

        private void RunScript(DbTransaction transaction, DbConnection connection, string scriptFullPath)
        {
            FileInfo fileInfo = new FileInfo(scriptFullPath);

            logger.Info("Running script: {0}", fileInfo.Name);

            string script = fileInfo.OpenText().ReadToEnd();

            using (DbCommand command = connection.CreateCommand())
            {
                command.Connection = connection;
                command.Transaction = transaction;

                command.CommandText = script;
                command.ExecuteNonQuery();
            }
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
