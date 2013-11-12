using MigSharpSQL.Exceptions;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Security;
using System.Text.RegularExpressions;

namespace MigSharpSQL
{
    /// <summary>
    /// Database migrator. All migration logic is here.
    /// </summary>
    public class Migrator
    {
        /// <summary>
        /// Gets database connection string.
        /// </summary>
        public string ConnectionString
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets database migrations directory.
        /// </summary>
        public string MigrationsDirectory
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Migrator"/> class.
        /// </summary>
        /// <param name="providerName">Unique database provider name.</param>
        /// <param name="connectionString">Database connection string.</param>
        /// <param name="migrationsDirectory">Database migrations directory.</param>
        /// <exception cref="ArgumentNullException">If either <paramref name="providerName"/>, 
        /// <paramref name="connectionString"/> or <paramref name="migrationsDirectory"/> is null.</exception> 
        /// <exception cref="NotSupportedException">When provider with specified name in
        /// <paramref name="providerName"/> param is not supported.</exception> 
        /// <exception cref="ProviderException">When error occurs, during loading provider.</exception>
        /// <exception cref="MigrationException">When migrations cannot be loaded.</exception>
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

            provider = DbProviderFactory.GetProvider(providerName);
            provider.Load();
            ConnectionString = connectionString;
            MigrationsDirectory = migrationsDirectory;
            migrations = new SortedDictionary<string, Migration>();

            LoadMigrations();
        }

        /// <summary>
        /// Moves the database state.
        /// </summary>
        /// <param name="state">The new desired database state.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="state"/>
        /// is null.</exception>
        /// <exception cref="MigrationException">If either database state or 
        /// <paramref name="state"/> parameter contains invalid value. It means state doesn't exist.</exception>
        /// <exception cref="ProviderException">When error occurs, during creating connection.</exception>
        /// <exception cref="System.Data.Common.DbException">When error occurs 
        /// during database communication.</exception>
        public void MigrateTo(string state)
        {
            if (state == null)
            {
                throw new ArgumentNullException("state");
            }

            state = ParseState(state);

            logger.Info("Migration started");

            int indexState;
            string[] keys = GetMigrationNames();

            indexState = GetStateIndex(state, keys, "The state {0} does not exist");

            using (IDbConnection connection = OpenConnection())
            {
                logger.Info("Figuring out the current database state");

                int currentSubstate;
                string currentState = GetCurrentState(connection, out currentSubstate);

                logger.Info("The current database state is {0}. The substate is {1}", 
                    GetHumanStateName(currentState), currentSubstate);

                int indexCurrentState = GetStateIndex(currentState, keys, "Unknown database state {0}");

                // ok, everything alright, let's migrate

                int diff = indexState - indexCurrentState;

                // We need to up (current state was not applied correctly before)
                if (diff >= 0)
                {
                    Up(connection, keys, indexCurrentState, indexState, currentSubstate);
                }
                // We need to down
                else
                {
                    Down(connection, keys, indexCurrentState, indexState + 1, currentSubstate);
                }
            }

            logger.Info("Migration completed successfully");
        }

        /// <summary>
        /// Gets current database migration state and substate
        /// </summary>
        /// <param name="substate">When this method returns it contains the substate value.</param>
        /// <returns>Current database state. Null means initial state (empty database). 
        /// Substate value in that case is nevermind.</returns>
        /// <exception cref="ProviderException">When error occurs, during creating connection.</exception>
        /// <exception cref="System.Data.Common.DbException">When error occurs 
        /// during database communication.</exception>
        public string GetCurrentState(out int substate)
        {
            using (IDbConnection connection = OpenConnection())
            {
                return GetHumanStateName(GetCurrentState(connection, out substate));
            }
        }

        /// <summary>
        /// Gets migration names.
        /// </summary>
        /// <returns>Migrations names.</returns>
        public string[] GetMigrationNames()
        {
            return migrations.Keys.ToArray();
        }

        private const string initialState = "initial";
        private const string lastState = "last";

        private const string upSuffix = "up";
        private const string downSuffix = "down";
        
        private static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Checks whether substate is valid. Substate is valid if it belongs to [0;steps.Count].
        /// </summary>
        /// <param name="state">Migration state.</param>
        /// <param name="substate">Migration substate.</param>
        /// <param name="steps">Migration steps.</param>
        /// <exception cref="MigrationException">If substate is invalid.</exception>
        private static void CheckSubstateValid(string state, int substate, string[] steps)
        {
            if (substate >= steps.Length || substate < 0)
            {
                throw new MigrationException(string.Format(
                    "There are only {0} substate(s) available for state {1}, but database stays in {2} substate",
                    steps.Length, state, substate));
            }
        }

        /// <summary>
        /// Returns state index in <paramref name="migrationNames" />.
        /// </summary>
        /// <param name="state">Migration state.</param>
        /// <param name="migrationNames">Ascending sorted migration names.</param>
        /// <param name="errorMsgTemplate">Error message template for exception.</param>
        /// <returns>State index in <paramref name="migrationNames"/> array or -1 if <paramref name="state"/> is null.</returns>
        /// <exception cref="MigrationException">If provided state doesn't exists.</exception>
        private static int GetStateIndex(string state, string[] migrationNames, string errorMsgTemplate)
        {
            int indexState;

            if (state == null)
            {
                indexState = -1;
            }
            else
            {
                indexState = Array.BinarySearch<string>(migrationNames, state);

                if (indexState < 0)
                {
                    throw new MigrationException(string.Format(errorMsgTemplate, state));
                }
            }

            return indexState;
        }

        /// <summary>
        /// Database provider.
        /// </summary>
        private readonly IDbProvider provider;

        /// <summary>
        /// Migration collection.
        /// </summary>
        private readonly SortedDictionary<string, Migration> migrations;

        /// <summary>
        /// Migration filename pattern.
        /// </summary>
        private readonly Regex scriptFilenamePattern = 
            new Regex(@"(\d{4}-\d{2}-\d{2}_\d{2}-\d{2})_(" + upSuffix + "|" + downSuffix + @")\.sql",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Scans migration directory for migrations and store them in <see cref="migrations"/>.
        /// </summary>
        /// <exception cref="FileNotFoundException">If at least one migration has no either up or down script.</exception>
        /// <exception cref="MigrationException">When migrations cannot be loaded.</exception>
        private void LoadMigrations()
        {
            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(MigrationsDirectory);

                migrations.Clear();

                foreach (FileInfo fileInfo in dirInfo.EnumerateFiles())
                {
                    Match match = scriptFilenamePattern.Match(fileInfo.Name);

                    if (match.Success)
                    {
                        string migrationName = match.Groups[1].Value;

                        Migration migration = GetMigration(migrationName);

                        if (string.Equals(upSuffix, match.Groups[2].Value, StringComparison.OrdinalIgnoreCase))
                        {
                            migration.UpScriptFullPath = fileInfo.FullName;
                        }
                        else
                        {
                            migration.DownScriptFullPath = fileInfo.FullName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is ArgumentException || ex is SecurityException ||
                    ex is PathTooLongException || ex is DirectoryNotFoundException)
                {
                    throw new MigrationException("Cannot load migrations.", ex);
                }

                throw;
            }

            foreach (Migration migration in migrations.Values)
            {
                if (migration.UpScriptFullPath == null || migration.DownScriptFullPath == null)
                {
                    throw new FileNotFoundException(
                        string.Format(
                            "Migration script {0}_{1}.sql is absent.",
                            migration.Name,
                            migration.UpScriptFullPath == null ? upSuffix: downSuffix)
                        );
                }
            }
        }

        /// <summary>
        /// Gets the migration from <see cref="migrations" /> or creates a new one, 
        /// add to <see cref="migrations"/> and returns it. 
        /// </summary>
        /// <param name="migrationName">Migration name. Cannot be null.</param>
        /// <returns>Migration object associated with given name.</returns>
        private Migration GetMigration(string migrationName)
        {
            Migration migration;

            if (!migrations.ContainsKey(migrationName))
            {
                migration = new Migration(migrationName);
                migrations.Add(migration.Name, migration);
            }
            else
            {
                migration = migrations[migrationName];
            }

            return migration;
        }

        /// <summary>
        /// Gets internal state representation.
        /// </summary>
        /// <param name="state">Human readable state.</param>
        /// <returns>Internal state representation.</returns>
        private string ParseState(string state)
        {
            if (state == lastState)
            {
                string[] keys = GetMigrationNames();

                if (keys.Length <= 0)
                {
                    state = initialState;
                }
                else
                {
                    state = keys[keys.Length - 1];
                }
            }

            if (state == initialState)
            {
                state = null;
            }

            return state;
        }

        /// <summary>
        /// Downgrades database.
        /// </summary>
        /// <param name="connection">Opened database connection.</param>
        /// <param name="migrationNames">Migration names collection.</param>
        /// <param name="first">Index of the first item in <paramref name="migrationNames"/>
        /// to downgrade.</param>
        /// <param name="last">Index of the last item in <paramref name="migrationNames"/>
        /// to downgrade.</param>
        /// <param name="currentSubstate">Current database substate.</param>
        /// <exception cref="System.Data.Common.DbException">When error occurs 
        /// during database communication.</exception>
        /// <exception cref="MigrationException">When the migration script cannot be loaded.</exception>        
        private void Down(IDbConnection connection, string[] migrationNames, int first, int last, int currentSubstate)
        {
            string[] steps = LoadScript(migrations[migrationNames[first]].DownScriptFullPath);

            CheckSubstateValid(migrationNames[first], currentSubstate, steps);

            logger.Info("Performing the downgrading scripts {0}...{1} has been started", migrationNames[first], 
                migrationNames[last]);

            for (int j = currentSubstate; j < steps.Length - 1; j++)
            {
                DoStep(connection, steps[j], migrationNames[first], j + 1);
            }

            string newState = first > 0 ? migrationNames[first - 1] : null;

            DoStep(connection, steps[steps.Length - 1], newState, 0);

            for (int i = first - 1; i >= last; i--)
            {
                steps = LoadScript(migrations[migrationNames[i]].DownScriptFullPath);
                                
                for (int j = 0; j < steps.Length - 1; j++)
                {
                    DoStep(connection, steps[j], migrationNames[i], j + 1);
                }

                newState = i > 0 ? migrationNames[i - 1] : null;

                DoStep(connection, steps[steps.Length - 1], newState, 0);
            }
        }

        /// <summary>
        /// Upgrades database.
        /// </summary>
        /// <param name="connection">Opened database connection.</param>
        /// <param name="migrationNames">Migration names collection.</param>
        /// <param name="first">Index of the first item in <paramref name="migrationNames"/>
        /// to upgrade.</param>
        /// <param name="last">Index of the last item in <paramref name="migrationNames"/>
        /// to upgrade.</param>
        /// <param name="currentSubstate">Current database substate.</param>
        /// <exception cref="System.Data.Common.DbException">When error occurs 
        /// during database communication.</exception>
        /// <exception cref="MigrationException">When the migration script cannot be loaded.</exception>
        private void Up(IDbConnection connection, string[] migrationNames, int first, int last, int currentSubstate)
        {
            if (first == last && currentSubstate == 0)
            {
                logger.Info("The database is already at specified state. No action required");
                return;
            }

            string[] steps;

            if (first >= 0)
            {
                logger.Info("Performing the upgrading scripts {0}...{1} has been started", migrationNames[first], 
                    migrationNames[last]);

                steps = LoadScript(migrations[migrationNames[first]].UpScriptFullPath);

                CheckSubstateValid(migrationNames[first], currentSubstate, steps);

                for (int j = steps.Length - currentSubstate; j < steps.Length; j++)
                {
                    DoStep(connection, steps[j], migrationNames[first], steps.Length - 1 - j);
                }
            }
            else
            {
                logger.Info("Performing the upgrading scripts {0}...{1} has been started", migrationNames[first + 1],
                    migrationNames[last]);
            }

            for (int i = first + 1; i <= last; i++)
            {
                steps = LoadScript(migrations[migrationNames[i]].UpScriptFullPath);

                for (int j = 0; j < steps.Length; j++)
                {
                    DoStep(connection, steps[j], migrationNames[i], steps.Length - 1 - j);
                }
            }
        }

        /// <summary>
        /// Runs migration query inside or outside the transaction. 
        /// </summary>
        /// <param name="connection">Opened database connection.</param>
        /// <param name="sql">Query to run.</param>
        /// <param name="newState">New state value.</param>
        /// <param name="substateNum">New substate value.</param>
        /// <exception cref="System.Data.Common.DbException">When error occurs 
        /// during database communication.</exception>
        private void DoStep(IDbConnection connection, string sql, string newState, int substateNum)
        {
            logger.Debug("Move database to state {0}, substate {1}", newState, substateNum);

            if (provider.SupportsTransactions)
            {
                using (IDbTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        RunStep(connection, transaction, sql, newState, substateNum);

                        transaction.Commit();
                    }
                    catch (DbException)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            else
            {
                RunStep(connection, null, sql, newState, substateNum);
            }
        }

        /// <summary>
        /// Runs migration query.
        /// </summary>
        /// <param name="connection">Opened database connection.</param>
        /// <param name="transaction">Started transaction for <paramref name="connection"/>.</param>
        /// <param name="sql">Query to run.</param>
        /// <param name="newState">New state value.</param>
        /// <param name="substateNum">New substate value.</param>
        /// <exception cref="System.Data.Common.DbException">When error occurs 
        /// during database communication.</exception>
        private void RunStep(IDbConnection connection, IDbTransaction transaction, 
            string sql, string newState, int substateNum)
        {
            using (IDbCommand command = connection.CreateCommand())
            {
                command.Connection = connection;
                command.Transaction = transaction;

                command.CommandText = sql;
                command.ExecuteNonQuery();
            }

            provider.SetState(connection, transaction, newState, substateNum);
        }

        /// <summary>
        /// Loads sql script from file and split it on the steps.
        /// </summary>
        /// <param name="scriptFullPath">Path to script file.</param>
        /// <returns>Splitted sql query.</returns>
        /// <exception cref="MigrationException">When the script cannot be loaded.</exception>
        private string[] LoadScript(string scriptFullPath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(scriptFullPath);

                logger.Debug("Loading script: {0}", fileInfo.Name);

                string script = fileInfo.OpenText().ReadToEnd();

                return script.Split(new string[] { "--//--\r\n", "--//--\n" }, StringSplitOptions.None);
            }
            catch (Exception ex)
            {
                if (ex is SecurityException || ex is UnauthorizedAccessException ||
                    ex is PathTooLongException || ex is DirectoryNotFoundException ||
                    ex is FileNotFoundException || ex is OutOfMemoryException ||
                    ex is IOException)
                {
                    throw new MigrationException(
                        string.Format("Script {0} cannot be loaded.", scriptFullPath), ex);
                }

                throw;
            }
        }

        /// <summary>
        /// Opens database connection using <see cref="ConnectionString"/>.
        /// </summary>
        /// <returns>Opened database connection.</returns>
        /// <exception cref="ProviderException">When error occurs, during creating connection.</exception>
        /// <exception cref="System.Data.Common.DbException">When error occurs 
        /// during database communication.</exception>
        private IDbConnection OpenConnection()
        {
            IDbConnection connection = provider.CreateConnection(ConnectionString);
            connection.Open();

            return connection;
        }

        /// <summary>
        /// Gets database migration state.
        /// </summary>
        /// <param name="connection">Opened connection to database.</param>
        /// <param name="substate">When this method returns it contains the substate value.</param>
        /// <returns>Current database state. Null means initial state (empty database). 
        /// Substate value in that case is nevermind.</returns>
        /// <exception cref="System.Data.Common.DbException">When error occurs 
        /// during database communication.</exception>
        private string GetCurrentState(IDbConnection connection, out int substate)
        {
            return provider.GetState(connection, out substate);
        }

        /// <summary>
        /// Gets human readable database state.
        /// </summary>
        /// <param name="state">Internal state value.</param>
        /// <returns>Human readable state value.</returns>
        private string GetHumanStateName(string state)
        {
            return state == null ? initialState : state;
        }
    }
}
