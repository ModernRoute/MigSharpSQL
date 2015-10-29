using MigSharpSQL.Exceptions;
using MigSharpSQL.Logging;
using MigSharpSQL.Resources;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
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
        /// Special initial state value.
        /// </summary>
        public const string InitialState = "initial";

        /// <summary>
        /// Alias to last state value.
        /// </summary>
        public const string LastState = "last";

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
        /// <param name="connectionString">Database connection string.</param>
        /// <param name="providerName">Database provider name.</param>
        /// <param name="migrationProcessorName">Migration processor name.</param>
        /// <param name="migrationsDirectory">Database migrations directory.</param>
        /// <exception cref="ArgumentNullException">If either <paramref name="providerName"/>, 
        /// <paramref name="connectionString"/>, <paramref name="migrationProcessorName"/> or 
        /// <paramref name="migrationsDirectory"/> is null.</exception> 
        /// <exception cref="MigrationException">When migrations cannot be loaded, provider or processor cannot be loaded.</exception>
        /// <exception cref="ArgumentException">When migrations cannot be loaded.</exception>
        public Migrator(string connectionString, string providerName, string migrationProcessorName, string migrationsDirectory)
        {
            if (providerName == null)
            {
                throw new ArgumentNullException(nameof(providerName));
            }

            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            if (migrationsDirectory == null)
            {
                throw new ArgumentNullException(nameof(migrationsDirectory));
            }

            if (migrationProcessorName == null)
            {
                throw new ArgumentNullException(nameof(migrationProcessorName));
            }

            try
            {
                _Provider = DbProviderFactories.GetFactory(providerName);
            }
            catch (ArgumentException ex)
            {
                throw new MigrationException(string.Format(Strings.ProviderCannotBeLoaded, providerName), ex);
            }

            try
            {
                _Processor = DbMigrationStateProcessorFactory.GetProcessor(migrationProcessorName);
            }
            catch (ArgumentException ex)
            {
                throw new MigrationException(string.Format(Strings.ProcessorCannotBeLoaded, migrationProcessorName), ex);
            }

            ConnectionString = connectionString;
            MigrationsDirectory = migrationsDirectory;
            _Migrations = new SortedDictionary<string, Migration>();

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
        /// <exception cref="System.Data.Common.DbException">When error occurs 
        /// during database communication.</exception>
        public void MigrateTo(string state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            state = ParseState(state);

            _Logger.Info(LogStrings.MigrationStarted);

            int indexState;
            string[] keys = GetMigrationNames();

            indexState = GetStateIndex(state, keys, Strings.StateDoesNotExists);

            using (IDbConnection connection = OpenConnection())
            {
                _Logger.Info(LogStrings.FiguringOutCurrentDbState);

                int currentSubstate;
                string currentState = GetCurrentState(connection, out currentSubstate);

                _Logger.Info(LogStrings.DbStateSubstateInfo, 
                    GetHumanStateName(currentState), currentSubstate);

                int indexCurrentState = GetStateIndex(currentState, keys, Strings.UnknownDatabaseState);

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

            _Logger.Info(LogStrings.MigrationCompletedSuccefully);
        }

        /// <summary>
        /// Gets current database migration state and substate
        /// </summary>
        /// <param name="substate">When this method returns it contains the substate value.</param>
        /// <returns>Current database state. Null means initial state (empty database). 
        /// Substate value in that case is nevermind.</returns>
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
            return _Migrations.Keys.ToArray();
        }

        private const string _UpSuffix = "up";
        private const string _DownSuffix = "down";
        
        private static ILogger _Logger = LoggerFactory.GetCurrentClassLogger();

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
                    Strings.SubstateIsNotApplicable,
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
        private readonly DbProviderFactory _Provider;
        
        /// <summary>
        /// Migration processor.
        /// </summary>
        private readonly IDbMigrationStateProcessor _Processor;
        
        /// <summary>
        /// Migration collection.
        /// </summary>
        private readonly SortedDictionary<string, Migration> _Migrations;

        /// <summary>
        /// Migration filename pattern.
        /// </summary>
        private readonly Regex _ScriptFilenamePattern = 
            new Regex(@"(\d{4}-\d{2}-\d{2}_\d{2}-\d{2})_(" + _UpSuffix + "|" + _DownSuffix + @")\.sql",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Scans migration directory for migrations and store them in <see cref="_Migrations"/>.
        /// </summary>
        /// <exception cref="FileNotFoundException">If at least one migration has no either up or down script.</exception>
        /// <exception cref="MigrationException">When migrations cannot be loaded.</exception>
        private void LoadMigrations()
        {
            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(MigrationsDirectory);

                _Migrations.Clear();

                foreach (FileInfo fileInfo in dirInfo.EnumerateFiles())
                {
                    Match match = _ScriptFilenamePattern.Match(fileInfo.Name);

                    if (match.Success)
                    {
                        string migrationName = match.Groups[1].Value;

                        Migration migration = GetMigration(migrationName);

                        if (string.Equals(_UpSuffix, match.Groups[2].Value, StringComparison.OrdinalIgnoreCase))
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
                    throw new MigrationException(Strings.CannotLoadMigrations, ex);
                }

                throw;
            }

            foreach (Migration migration in _Migrations.Values)
            {
                if (migration.UpScriptFullPath == null || migration.DownScriptFullPath == null)
                {
                    throw new FileNotFoundException(
                        string.Format(
                            Strings.MigrationScriptIsAbsent,
                            GetMigrationScriptName(migration.Name, migration.UpScriptFullPath == null)
                        )
                    );
                }
            }
        }

        /// <summary>
        /// Gets migration script name.
        /// </summary>
        /// <param name="migrationName">Migration name.</param>
        /// <param name="isUp">The type of the script.</param>
        /// <returns>Script name.</returns>
        private string GetMigrationScriptName(string migrationName, bool isUp)
        {
            return string.Format(CultureInfo.InvariantCulture,
                "{0}_{1}.sql", migrationName, isUp ? _UpSuffix : _DownSuffix);
        }

        /// <summary>
        /// Gets the migration from <see cref="_Migrations" /> or creates a new one, 
        /// add to <see cref="_Migrations"/> and returns it. 
        /// </summary>
        /// <param name="migrationName">Migration name. Cannot be null.</param>
        /// <returns>Migration object associated with given name.</returns>
        private Migration GetMigration(string migrationName)
        {
            Migration migration;

            if (!_Migrations.ContainsKey(migrationName))
            {
                migration = new Migration(migrationName);
                _Migrations.Add(migration.Name, migration);
            }
            else
            {
                migration = _Migrations[migrationName];
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
            if (state == LastState)
            {
                string[] keys = GetMigrationNames();

                if (keys.Length <= 0)
                {
                    state = InitialState;
                }
                else
                {
                    state = keys[keys.Length - 1];
                }
            }

            if (state == InitialState)
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
            string[] steps = LoadScript(_Migrations[migrationNames[first]].DownScriptFullPath);

            CheckSubstateValid(migrationNames[first], currentSubstate, steps);

            _Logger.Info(LogStrings.PerformingDowngrade, migrationNames[first], 
                migrationNames[last]);

            for (int j = currentSubstate; j < steps.Length - 1; j++)
            {
                DoStep(connection, steps[j], migrationNames[first], j + 1);
            }

            string newState = first > 0 ? migrationNames[first - 1] : null;

            DoStep(connection, steps[steps.Length - 1], newState, 0);

            for (int i = first - 1; i >= last; i--)
            {
                steps = LoadScript(_Migrations[migrationNames[i]].DownScriptFullPath);
                                
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
                _Logger.Info(LogStrings.NoActionForTheSameState);
                return;
            }

            string[] steps;

            if (first >= 0)
            {
                _Logger.Info(LogStrings.PerformingUpgrade, migrationNames[first], 
                    migrationNames[last]);

                steps = LoadScript(_Migrations[migrationNames[first]].UpScriptFullPath);

                CheckSubstateValid(migrationNames[first], currentSubstate, steps);

                for (int j = steps.Length - currentSubstate; j < steps.Length; j++)
                {
                    DoStep(connection, steps[j], migrationNames[first], steps.Length - 1 - j);
                }
            }
            else
            {
                _Logger.Info(LogStrings.PerformingUpgrade, migrationNames[first + 1],
                    migrationNames[last]);
            }

            for (int i = first + 1; i <= last; i++)
            {
                steps = LoadScript(_Migrations[migrationNames[i]].UpScriptFullPath);

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
            _Logger.Debug(LogStrings.MovingDbState, newState, substateNum);

            if (_Processor.SupportsTransactions)
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

            _Processor.SetState(connection, transaction, newState, substateNum);
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

                _Logger.Debug(LogStrings.LoadingScript, fileInfo.Name);

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
                        string.Format(Strings.ScriptCannotBeLoaded, scriptFullPath), ex);
                }

                throw;
            }
        }

        /// <summary>
        /// Opens database connection using <see cref="ConnectionString"/>.
        /// </summary>
        /// <returns>Opened database connection.</returns>
        /// <exception cref="System.Data.Common.DbException">When error occurs 
        /// during database communication.</exception>
        private IDbConnection OpenConnection()
        {
            IDbConnection connection = _Provider.CreateConnection();
            connection.ConnectionString = ConnectionString;
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
            return _Processor.GetState(connection, out substate);
        }

        /// <summary>
        /// Gets human readable database state.
        /// </summary>
        /// <param name="state">Internal state value.</param>
        /// <returns>Human readable state value.</returns>
        private string GetHumanStateName(string state)
        {
            return state == null ? InitialState : state;
        }
    }
}
