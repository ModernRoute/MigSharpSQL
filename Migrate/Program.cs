using System;
using System.Configuration;
using System.Collections.Generic;
using Migrate.Util;
using MigSharpSQL;
using NLog.Config;
using NLog.Targets;
using NLog;
using Migrate.Resources;

namespace Migrate
{
    internal class Program
    {
        private const int SuccessExitCode = 0x0;
        private const int InvalidArgumentsExitCode = 0x1;
        private const int FailedMigrationExitCode = 0x10;

        private static Logger logger = LogManager.GetCurrentClassLogger();

        private static void SetupLogger()
        {
            LoggingConfiguration config = new LoggingConfiguration();

            ColoredConsoleTarget consoleTarget = new ColoredConsoleTarget();
            config.AddTarget("console", consoleTarget);

            consoleTarget.Layout = "${message}";

            LoggingRule rule = new LoggingRule("*", LogLevel.Trace, consoleTarget);
            config.LoggingRules.Add(rule);

            LogManager.Configuration = config;
        }

        private static void Main(string[] args)
        {
            SetupLogger();

            CommandLineOptions options;
            if (!ParseArgs(args, out options))
            {
                Environment.ExitCode = InvalidArgumentsExitCode;
                return;
            }

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
                Migrator migrator = MigratorFromConfig();

                int substate;
                string state = migrator.GetCurrentState(out substate);

                logger.Info(LogStrings.CurrentState, state);

                return SuccessExitCode;
            }
            catch (Exception ex)
            {
                logger.Error(LogStrings.CannotFetchCurrentState, ex);
                return FailedMigrationExitCode;
            }
        }

        private static Migrator MigratorFromConfig()
        {
            return new Migrator(
                ConfigurationManager.AppSettings["provider"], 
                ConfigurationManager.AppSettings["connection-string"], 
                ConfigurationManager.AppSettings["directory"]
                );            
        }

        private static int Migrate(CommandLineOptions options)
        {
            string wantedState = options.GetOption("to",Migrator.LastState);

            try
            {
                Migrator migrator = MigratorFromConfig();

                migrator.MigrateTo(wantedState.ToLower());
                
                return SuccessExitCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine(Strings.CannotMigrateToStateSinceError, wantedState, ex);
                return FailedMigrationExitCode;
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
