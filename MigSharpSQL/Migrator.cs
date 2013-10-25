using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
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
        private SortedDictionary<string,Migration> Migrations
        {
            get;
            set;
        }

        public string[] GetMigrationNames()
        {
            return Migrations.Keys.ToArray();
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
            Provider.Load();
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
                    throw new FileNotFoundException(
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

            using (IDbConnection connection = OpenConnection())
            {
                logger.Info("Figuring out the current database state");

                int currentSubstate;
                string currentState = GetCurrentState(connection, out currentSubstate);

                logger.Info("The current database state is `{0}`. The substate is {1}", GetHumanStateName(currentState), currentSubstate);

                int indexCurrentState = GetStateIndex(currentState, keys, "Unknown database state `{0}`");

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
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="keys"></param>
        /// <param name="first"></param>
        /// <param name="last"></param>
        /// <param name="currentSubstate"></param>
        private void Down(IDbConnection connection, string[] keys, int first, int last, int currentSubstate)
        {
            string[] steps = LoadScript(Migrations[keys[first]].DownScriptFullPath);

            CheckSubstateValid(keys, first, currentSubstate, steps);

            logger.Info("Performing the downgrading scripts {0}...{1} has been started", keys[first], keys[last]);

            for (int j = currentSubstate; j < steps.Length - 1; j++)
            {
                DoStep(connection, steps[j], keys[first], j + 1);
            }

            string newState = first > 0 ? keys[first - 1] : null;

            DoStep(connection, steps[steps.Length - 1], newState, 0);

            for (int i = first - 1; i >= last; i--)
            {
                steps = LoadScript(Migrations[keys[i]].DownScriptFullPath);
                                
                for (int j = 0; j < steps.Length - 1; j++)
                {
                    DoStep(connection, steps[j], keys[i], j + 1);
                }

                newState = i > 0 ? keys[i - 1] : null;

                DoStep(connection, steps[steps.Length - 1], newState, 0);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="first"></param>
        /// <param name="currentSubstate"></param>
        /// <param name="steps"></param>
        private static void CheckSubstateValid(string[] keys, int first, int currentSubstate, string[] steps)
        {
            if (currentSubstate >= steps.Length || currentSubstate < 0)
            {
                throw new InvalidOperationException(string.Format(
                    "There are only {0} substate(s) available for state `{1}`, but database stays in {2} substate",
                    steps.Length, keys[first], currentSubstate));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="keys"></param>
        /// <param name="first"></param>
        /// <param name="last"></param>
        /// <param name="currentSubstate"></param>
        private void Up(IDbConnection connection, string[] keys, int first, int last, int currentSubstate)
        {
            if (first == last && currentSubstate == 0)
            {
                logger.Info("The database is already at specified state. No action required");
                return;
            }

            string[] steps;

            if (first >= 0)
            {
                logger.Info("Performing the upgrading scripts {0}...{1} has been started", keys[first], keys[last]);

                steps = LoadScript(Migrations[keys[first]].UpScriptFullPath);

                CheckSubstateValid(keys, first, currentSubstate, steps);

                for (int j = steps.Length - currentSubstate; j < steps.Length; j++)
                {
                    DoStep(connection, steps[j], keys[first], steps.Length - 1 - j);
                }
            }
            else
            {
                logger.Info("Performing the upgrading scripts {0}...{1} has been started", keys[first + 1], keys[last]);
            }

            for (int i = first + 1; i <= last; i++)
            {
                steps = LoadScript(Migrations[keys[i]].UpScriptFullPath);

                for (int j = 0; j < steps.Length; j++)
                {
                    DoStep(connection, steps[j], keys[i], steps.Length - 1 - j);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="stepBody"></param>
        /// <param name="newState"></param>
        /// <param name="substateNum"></param>
        private void DoStep(IDbConnection connection, string stepBody, string newState, int substateNum)
        {
            logger.Debug("Move database to state `{0}`, substate `{1}`", newState, substateNum);

            if (Provider.SupportsTransactions)
            {
                using (IDbTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        RunStep(connection, transaction, stepBody, newState, substateNum);

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
                RunStep(connection, null, stepBody, newState, substateNum);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <param name="stepBody"></param>
        /// <param name="newState"></param>
        /// <param name="substateNum"></param>
        private void RunStep(IDbConnection connection, IDbTransaction transaction, string stepBody, string newState, int substateNum)
        {
            using (IDbCommand command = connection.CreateCommand())
            {
                command.Connection = connection;
                command.Transaction = transaction;

                command.CommandText = stepBody;
                command.ExecuteNonQuery();
            }

            Provider.SetState(connection, transaction, newState, substateNum);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scriptFullPath"></param>
        /// <returns></returns>
        private string[] LoadScript(string scriptFullPath)
        {
            FileInfo fileInfo = new FileInfo(scriptFullPath);

            logger.Debug("Loading script: {0}", fileInfo.Name);
            
            string script = fileInfo.OpenText().ReadToEnd();

            return script.Split(new string[] {"--//--\r\n","--//--\n"},StringSplitOptions.None);
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
        private IDbConnection OpenConnection()
        {
            IDbConnection connection = Provider.CreateConnection(ConnectionString);
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
        private string GetCurrentState(IDbConnection connection, out int substate)
        {
            return Provider.GetState(connection, out substate);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string GetCurrentState(out int substate)
        {
            using (IDbConnection connection = OpenConnection())
            {
                return GetHumanStateName(GetCurrentState(connection, out substate));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private string GetHumanStateName(string state)
        {
            return state == null ? "initial" : state;
        }
    }
}
