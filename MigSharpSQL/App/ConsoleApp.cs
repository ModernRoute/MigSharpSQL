using MigSharpSQL.Resources;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigSharpSQL.App
{
    public class ConsoleApp
    {
        private const int _SuccessExitCode = 0x0;
        private const int _InvalidArgumentsExitCode = 0x1;
        private const int _FailedMigrationExitCode = 0x10;

        private const string _DirectoryKeyCmdLineOptionName = "DirectoryKey";
        private const string _ConnectionStringNameKeyCmdLineOptionName = "ConnectionStringNameKey";
        private const string _ProcessorKeyCmdLineOptionName = "ProcessorKey";
        
        private static Logger _Logger = LogManager.GetCurrentClassLogger();

        public static void EntryPoint(string configDirectoryKey, 
            string configConnectionStringNameKey, 
            string configProcessorKey, string[] args)
        {
            if (configDirectoryKey == null)
            {
                throw new ArgumentNullException("configDirectoryKey");
            }

            if (configConnectionStringNameKey == null)
            {
                throw new ArgumentNullException("configConnectionStringNameKey");
            }

            if (configProcessorKey == null)
            {
                throw new ArgumentNullException("configProcessorKey");
            }

            CommandLineOptions options;
            if (!ParseArgs(args, out options))
            {
                Environment.ExitCode = _InvalidArgumentsExitCode;
                return;
            }

            options.Options[_DirectoryKeyCmdLineOptionName] = configDirectoryKey;
            options.Options[_ConnectionStringNameKeyCmdLineOptionName] = configConnectionStringNameKey;
            options.Options[_ProcessorKeyCmdLineOptionName] = configProcessorKey;

            switch (options.Command)
            {
                case "state":
                    Environment.ExitCode = GetState(options);
                    break;
                case "migrate":
                    Environment.ExitCode = Migrate(options);
                    break;
                case "help":
                case "--help":
                    Help();
                    break;
            }
        }

        private static void Help()
        {
            Console.WriteLine(Strings.Usage, Migrator.InitialState, Migrator.LastState);
        }

        private static int GetState(CommandLineOptions options)
        {
            try
            {
                Migrator migrator = MigratorFromConfig(options);

                int substate;
                string state = migrator.GetCurrentState(out substate);

                _Logger.Info(LogStrings.CurrentState, state);

                return _SuccessExitCode;
            }
            catch (Exception ex)
            {
                _Logger.Error(LogStrings.CannotFetchCurrentState, ex);
                return _FailedMigrationExitCode;
            }
        }

        private static Migrator MigratorFromConfig(CommandLineOptions options)
        {
            const string appSettingsSection = "configuration/appSettings";
            const string connStringsSection = "configuration/system.data/connectionStrings";

            string connStringNameKey = options.GetOption(_ConnectionStringNameKeyCmdLineOptionName);

            string connStringName = ConfigurationManager.AppSettings[connStringNameKey];

            if (connStringName == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                    Strings.KeyIsAbsentInAppConfig, connStringNameKey, appSettingsSection));
            }

            ConnectionStringSettings connStringSettings = ConfigurationManager.ConnectionStrings[connStringName];

            if (connStringSettings == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                    Strings.ConnStringIsAbsentInAppConfig, connStringName, connStringsSection));
            }

            string migrationProcessorNameKey = options.GetOption(_ProcessorKeyCmdLineOptionName);

            string migrationProcessorName = ConfigurationManager.AppSettings[migrationProcessorNameKey];

            if (migrationProcessorName == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                     Strings.KeyIsAbsentInAppConfig, migrationProcessorNameKey, appSettingsSection));
            }

            string directoryNameKey = options.GetOption(_DirectoryKeyCmdLineOptionName);

            string directoryName = ConfigurationManager.AppSettings[directoryNameKey];

            if (directoryName == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                    Strings.KeyIsAbsentInAppConfig, directoryNameKey, appSettingsSection));
            }

            return new Migrator(connStringSettings.ConnectionString,
                                connStringSettings.ProviderName,
                                migrationProcessorName,
                                directoryName);
        }

        private static int Migrate(CommandLineOptions options)
        {
            string wantedState = options.GetOption("to", Migrator.LastState);

            try
            {
                Migrator migrator = MigratorFromConfig(options);

                migrator.MigrateTo(wantedState.ToLower());

                return _SuccessExitCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine(Strings.CannotMigrateToStateSinceError, wantedState, ex);
                return _FailedMigrationExitCode;
            }
        }

        private static bool ParseArgs(string[] args, out CommandLineOptions options)
        {
            string key = null;
            options = null;

            foreach (string arg in args)
            {
                if (options == null)
                {
                    options = new CommandLineOptions(arg);
                    continue;
                }

                if (key == null)
                {
                    if (!arg.StartsWith("--"))
                    {
                        continue;
                    }

                    key = arg.Substring(2, arg.Length - 2);

                    continue;
                }

                if (!options.Options.ContainsKey(key))
                {
                    options.Options.Add(key, arg);
                }

                key = null;
            }

            if (options == null)
            {
                options = new CommandLineOptions("help");
            }

            if (key != null)
            {
                options.Options.Add(key, "");
            }

            switch (options.Command)
            {
                case "help":
                case "--help":
                case "state":
                case "migrate":
                    return true;
                default:
                    Console.WriteLine("migrate: {0}",
                        string.Format(Strings.NotMigrateCommand, options.Command, "migrate help"));
                    return false;
            }
        }
    }

}
