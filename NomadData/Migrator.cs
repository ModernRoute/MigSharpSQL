using ModernRoute.NomadData.Exceptions;
using ModernRoute.NomadData.Logging;
using ModernRoute.NomadData.Resources;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text.RegularExpressions;

namespace ModernRoute.NomadData
{
    public class Migrator
    {
        public const string InitialState = "initial";
        public const string LastState = "last";

        public string ConnectionString
        {
            get;
            private set;
        }
        
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
            _Migrations = LoadMigrations(migrationsDirectory);
        }

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
                UpgradeMigrationInfo(connection);

                _Logger.Info(LogStrings.FiguringOutCurrentDbState);

                int currentSubstate;
                string currentState = GetCurrentState(connection, null, out currentSubstate);

                _Logger.Info(LogStrings.DbStateSubstateInfo, 
                    GetHumanStateName(currentState), currentSubstate);

                int indexCurrentState = GetStateIndex(currentState, keys, Strings.UnknownDatabaseState);

                int diff = indexState - indexCurrentState;

                if (diff >= 0)
                {
                    Up(connection, keys, indexCurrentState, indexState, currentSubstate);
                }
                else
                {
                    Down(connection, keys, indexCurrentState, indexState, currentSubstate);
                }
            }

            _Logger.Info(LogStrings.MigrationCompletedSuccefully);
        }

        private void UpgradeMigrationInfo(IDbConnection connection)
        {
            if (_Processor.SupportsTransactions)
            {
                using (IDbTransaction transaction = connection.BeginTransaction(IsolationLevel.Snapshot))
                {
                    UpgradeMigrationInfo(connection, transaction);
                    transaction.Commit();
                }
            }
            else
            {
                UpgradeMigrationInfo(connection, null);
            }
        }

        private void UpgradeMigrationInfo(IDbConnection connection, IDbTransaction transaction)
        {
            if (!_Processor.CheckDeprecated(connection, transaction))
            {
                return;
            }

            int substate;
            string state = _Processor.GetStateObsolete(connection, out substate);

            if (!_Migrations.ContainsKey(state))
            {
                throw new InvalidDataException(""); // TODO: message
            }

            CheckSubstateValid(state, substate, _Migrations[state].Steps.Count);
                
            IEnumerable<SimpleMigrationHistoryItem> appliedMigrations = 
                _Migrations.SelectMany(MigrationToSimpleMigrationHistoryItem).TakeWhile(m => m.State != state || m.Substate != substate);
                
            _Processor.AddHistory(connection, transaction, appliedMigrations);
        }

        public string GetCurrentState(out int substate)
        {
            using (IDbConnection connection = OpenConnection())
            {
                return GetHumanStateName(GetCurrentState(connection, null, out substate));
            }
        }

        public string[] GetMigrationNames()
        {
            return _Migrations.Keys.ToArray();
        }

        private const string _UpSuffix = "up";
        private const string _DownSuffix = "down";
        
        private static ILogger _Logger = LoggerFactory.GetCurrentClassLogger();

        private static void CheckSubstateValid(string state, int substate, int maxCount)
        {
            if (substate >= maxCount || substate < 0)
            {
                throw new MigrationException(string.Format(
                    Strings.SubstateIsNotApplicable,
                    maxCount, state, substate));
            }
        }

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

        private readonly DbProviderFactory _Provider;
        
        private readonly IDbMigrationStateProcessor _Processor;
        
        private readonly SortedDictionary<string, Migration> _Migrations;

        private static readonly Regex _ScriptFilenamePattern = 
            new Regex(@"(\d{4}-\d{2}-\d{2}_\d{2}-\d{2})_(" + _UpSuffix + "|" + _DownSuffix + @")\.sql",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static SortedDictionary<string, Migration> LoadMigrations(string migrationsDirectory)
        {
            try
            {
                IDictionary<string, string[]> upScripts = new Dictionary<string, string[]>();
                IDictionary<string, string[]> downScripts = new Dictionary<string, string[]>();
                
                DirectoryInfo dirInfo = new DirectoryInfo(migrationsDirectory);

                foreach (FileInfo fileInfo in dirInfo.EnumerateFiles())
                {
                    Match match = _ScriptFilenamePattern.Match(fileInfo.Name);

                    if (match.Success)
                    {
                        string migrationName = match.Groups[1].Value;

                        string script = LoadScript(fileInfo.FullName);
                        string[] steps = script.Split(new string[] { "--//--\r\n", "--//--\n" }, StringSplitOptions.None);

                        if (string.Equals(_UpSuffix, match.Groups[2].Value, StringComparison.OrdinalIgnoreCase))
                        {
                            upScripts[migrationName] = steps;
                        }
                        else
                        {
                            downScripts[migrationName] = steps;
                        }
                    }
                }

                SortedDictionary<string, Migration> migrations = new SortedDictionary<string, Migration>();

                foreach (string migration in upScripts.Keys.Concat(downScripts.Keys).Distinct())
                {
                    bool upScriptIsAbsent = !upScripts.ContainsKey(migration);
                    bool downScriptIsAbsent = !downScripts.ContainsKey(migration);

                    if (upScriptIsAbsent || downScriptIsAbsent)
                    {
                        throw new FileNotFoundException(
                            string.Format(
                                Strings.MigrationScriptIsAbsent,
                                GetMigrationScriptName(migration, upScriptIsAbsent)
                            )
                        );
                    }

                    string[] upSteps = upScripts[migration];
                    string[] downSteps = downScripts[migration];

                    migrations.Add(migration, new Migration(migration, GetSteps(upSteps, downSteps)));
                }

                return migrations;
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
        }

        private static IEnumerable<MigrationStepTuple> GetSteps(string[] upSteps, string[] downSteps)
        {
            if (upSteps.Length != downSteps.Length)
            {
                throw new InvalidDataException(""); // TODO: message
            }

            for (int i = 0; i < upSteps.Length; i++)
            {
                yield return new MigrationStepTuple(upSteps[i], downSteps[i]);
            }
        }

        private static string GetMigrationScriptName(string migrationName, bool isUp)
        {
            return string.Format(CultureInfo.InvariantCulture,
                "{0}_{1}.sql", migrationName, isUp ? _UpSuffix : _DownSuffix);
        }

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

        private void Down(IDbConnection connection, string[] migrationNames, int first, int last, int currentSubstate)
        {
            string newState = migrationNames[first];

            IReadOnlyList<MigrationStepTuple> steps = _Migrations[newState].Steps;

            string previousState = newState;
            int previousSubstate = currentSubstate;

            CheckSubstateValid(newState, currentSubstate, steps.Count);

            string lastState = last >= 0 ? migrationNames[last] : null;

            _Logger.Info(LogStrings.PerformingDowngrade, newState, GetHumanStateName(lastState));

            int newSubstate;

            for (int j = currentSubstate; j < steps.Count - 1; j++)
            {
                newSubstate = j + 1;

                DoStep(connection, steps[j].Down, newState, newSubstate, previousState, previousSubstate, false);

                previousState = newState;
                previousSubstate = newSubstate;
            }

            newState = first >= 0 ? migrationNames[first] : null;
            newSubstate = 0;

            DoStep(connection, steps[steps.Count - 1].Down, newState, newSubstate, previousState, previousSubstate, false);

            previousState = newState;
            previousSubstate = newSubstate;

            for (int i = first - 1; i > last; i--)
            {
                newState = migrationNames[i];

                steps = _Migrations[newState].Steps;
                                
                for (int j = 0; j < steps.Count - 1; j++)
                {
                    newSubstate = j + 1;

                    DoStep(connection, steps[j].Down, newState, newSubstate, previousState, previousSubstate, false);

                    previousState = newState;
                    previousSubstate = newSubstate;
                }

                newState = i > 0 ? migrationNames[i - 1] : null;
                newSubstate = 0;

                DoStep(connection, steps[steps.Count - 1].Down, newState, newSubstate, previousState, previousSubstate, false);

                previousState = newState;
                previousSubstate = newSubstate;
            }
        }

        private void Up(IDbConnection connection, string[] migrationNames, int first, int last, int currentSubstate)
        {
            if (first == last && currentSubstate == 0)
            {
                _Logger.Info(LogStrings.NoActionForTheSameState);
                return;
            }

            string newState = first >= 0 ? migrationNames[first] : null;

            IReadOnlyList<MigrationStepTuple> steps;

            string previousState = newState;
            int previousSubstate = currentSubstate;

            _Logger.Info(LogStrings.PerformingUpgrade, GetHumanStateName(newState),
                migrationNames[last]);

            if (first >= 0)
            {
                steps = _Migrations[newState].Steps;

                CheckSubstateValid(newState, currentSubstate, steps.Count);

                for (int j = steps.Count - currentSubstate; j < steps.Count; j++)
                {
                    int substate = steps.Count - 1 - j;

                    DoStep(connection, steps[j].Up, newState, substate, previousState, previousSubstate, true);

                    previousState = newState;
                    previousSubstate = substate;
                }
            }

            for (int i = first + 1; i <= last; i++)
            {
                newState = migrationNames[i];

                steps = _Migrations[newState].Steps;

                for (int j = 0; j < steps.Count; j++)
                {
                    int substate = steps.Count - 1 - j;

                    DoStep(connection, steps[j].Up, newState, substate, previousState, previousSubstate, true);

                    previousState = newState;
                    previousSubstate = substate;
                }
            }
        }

        private void DoStep(IDbConnection connection, string sql, string newState, int newSubstate, string currentState, int currentSubstate, bool isUp)
        {
            _Logger.Debug(LogStrings.MovingDbState, newState, newSubstate);

            if (_Processor.SupportsTransactions)
            {
                using (IDbTransaction transaction = connection.BeginTransaction(IsolationLevel.Snapshot))
                {
                    try
                    {
                        RunStep(connection, transaction, sql, newState, newSubstate, currentState, currentSubstate, isUp);

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
                RunStep(connection, null, sql, newState, newSubstate, currentState, currentSubstate, isUp);
            }
        }

        private void RunStep(IDbConnection connection, IDbTransaction transaction, 
            string sql, string newState, int newSubstate, string currentState, int currentSubstate, bool isUp)
        {
            int actualSubstate;
            string actualState = _Processor.GetState(connection, transaction, out actualSubstate);

            if (actualState != currentState || actualSubstate != currentSubstate)
            {
                throw new InvalidOperationException(""); // TODO: message
            }

            using (IDbCommand command = connection.CreateCommand())
            {
                command.Connection = connection;
                command.Transaction = transaction;

                command.CommandText = sql;
                command.ExecuteNonQuery();
            }

            _Processor.SetState(connection, transaction, newState, newSubstate, isUp);
        }

        private static string LoadScript(string scriptFullPath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(scriptFullPath);

                _Logger.Debug(LogStrings.LoadingScript, fileInfo.Name);

                return fileInfo.OpenText().ReadToEnd();
            }
            catch (Exception ex) when
                (ex is SecurityException || ex is UnauthorizedAccessException ||
                        ex is PathTooLongException || ex is DirectoryNotFoundException ||
                        ex is FileNotFoundException || ex is OutOfMemoryException ||
                        ex is IOException)
            {
                throw new MigrationException(
                    string.Format(Strings.ScriptCannotBeLoaded, scriptFullPath), ex);
            }
        }

        private IDbConnection OpenConnection()
        {
            IDbConnection connection = _Provider.CreateConnection();
            connection.ConnectionString = ConnectionString;
            connection.Open();

            return connection;
        }

        private string GetCurrentState(IDbConnection connection, IDbTransaction transaction, out int substate)
        {
            return _Processor.GetState(connection, transaction, out substate);
        }

        private void CheckHistory(IDbConnection connection)
        {
            Stack<MigrationHistoryItem> appliedMigrations = new Stack<MigrationHistoryItem>();

            foreach (MigrationHistoryItem item in _Processor.EnumerateHistory(connection))
            {
                MigrationHistoryItem lastItem = appliedMigrations.Count == 0 ? null : appliedMigrations.Peek();

                if (item.IsUp)
                {
                    if (lastItem != null)
                    {
                        if (item.State == lastItem.State)
                        {
                            if (item.Substate + 1 != lastItem.Substate)
                            {
                                throw new InvalidDataException(""); // TODO: message
                            }
                        }
                        else
                        {
                            if (string.Compare(item.State, lastItem.State) <= 0)
                            {
                                throw new InvalidDataException(""); // TODO: message
                            }

                            if (lastItem.Substate != 0)
                            {
                                throw new InvalidDataException(""); // TODO: message
                            }
                        }
                    }

                    appliedMigrations.Push(item);
                }
                else
                {
                    if (lastItem == null)
                    {
                        throw new InvalidDataException(""); // TODO: message
                    }

                    if (lastItem.State != lastItem.State || lastItem.Substate != lastItem.Substate)
                    {
                        throw new InvalidDataException(""); // TODO: message
                    }

                    appliedMigrations.Pop();
                }
            }

            CheckHistory(_Migrations.SelectMany(MigrationToMigrationHistoryItem), appliedMigrations);
        }

        private void CheckHistory(IEnumerable<MigrationHistoryItem> migrations, IEnumerable<MigrationHistoryItem> migrationHistory)
        {
            using (IEnumerator<MigrationHistoryItem> enumerator1 = migrations.GetEnumerator())
            {
                using (IEnumerator<MigrationHistoryItem> enumerator2 = migrationHistory.GetEnumerator())
                {
                    bool hasNext1 = enumerator1.MoveNext();
                    bool hasNext2 = enumerator2.MoveNext();

                    while (hasNext1 && hasNext2)
                    {
                        if (enumerator1.Current.State != enumerator2.Current.State ||
                            enumerator1.Current.Substate != enumerator2.Current.Substate)
                        {
                            throw new InvalidDataException(""); // TODO: message
                        }

                        hasNext1 = enumerator1.MoveNext();
                        hasNext2 = enumerator2.MoveNext();
                    }
                    
                    if (hasNext2)
                    {
                        throw new InvalidDataException(""); // TODO: message
                    }
                }
            }
        }

        private IEnumerable<MigrationHistoryItem> MigrationToMigrationHistoryItem(KeyValuePair<string, Migration> migration)
        {
            for (int i = migration.Value.Steps.Count - 1; i >= 0; i--)
            {
                yield return new MigrationHistoryItem(DateTime.UtcNow, migration.Value.Name, i, true);
            }
        }

        private IEnumerable<SimpleMigrationHistoryItem> MigrationToSimpleMigrationHistoryItem(KeyValuePair<string, Migration> migration)
        {
            for (int i = migration.Value.Steps.Count - 1; i >= 0; i--)
            {
                yield return new SimpleMigrationHistoryItem(migration.Value.Name, i);
            }
        }

        private string GetHumanStateName(string state)
        {
            return state == null ? InitialState : state;
        }
    }
}
