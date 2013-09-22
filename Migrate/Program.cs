using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Migrate.Util;
using MigSharpSQL;
using NLog.Config;
using NLog.Targets;
using NLog;

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
                    Environment.ExitCode = GetState(options.Options);
                    break;
                case "migrate":
                    Environment.ExitCode = Migrate(options.Options);
                    break;
                case "help":
                case "--help":
                    Help();
                    break;
            }
        }

        private static void Help()
        {
            Console.WriteLine("usage: migrate <command> <args>");
            Console.WriteLine("");
            Console.WriteLine("The migrate commands are:");
            Console.WriteLine("    state    Get current database state");
            Console.WriteLine("    migrate  Migrate database to specified state");
            Console.WriteLine("");
            Console.WriteLine("Commands parameters:");
            Console.WriteLine("    --provider <provider>                    Provider name. Required");
            Console.WriteLine("    --connection-string <connection-string>  Connection string. Required");
            Console.WriteLine("    --directory <directory>                  Migrations directory. Optional. The current directory");
            Console.WriteLine("                                             will be used if ommited");
            Console.WriteLine("    --to <state>                             Wanted state. Useful for `migrate` command only. Optional. ");
            Console.WriteLine("                                             Special values: `initial` (before the first migration state), ");
            Console.WriteLine("                                             `last` (the last migration state). Will use `last` if ommited");
        }

        private static int GetState(Dictionary<string, string> dictionary)
        {
            try
            {
                Migrator migrator = new Migrator(dictionary["provider"], dictionary["connection-string"], dictionary["directory"]);

                string state = migrator.GetCurrentState();

                logger.Info("Current state: {0}", state == null ? "initial" : state);

                return SuccessExitCode;
            }
            catch (Exception ex)
            {
                logger.Error("Cannot fetch the current state: {0}", ex);
                return FailedMigrationExitCode;
            }
        }

        private static int Migrate(Dictionary<string, string> dictionary)
        {
            string wantedState = dictionary.ContainsKey("to") ? dictionary["to"] : "last";

            try
            {
                Migrator migrator = new Migrator(dictionary["provider"], dictionary["connection-string"], dictionary["directory"]);

                switch (wantedState.ToLower())
                {
                    case "initial":
                        migrator.MigrateTo(null);
                        break;
                    case "last":
                        migrator.MigrateToLast();
                        break;
                    default:
                        migrator.MigrateTo(wantedState);
                        break;
                } 

                return SuccessExitCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot migrate to state `{0}` since error: {1}", wantedState, ex);
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
                    return true;
                case "state":
                case "migrate":
                    if (!options.Options.ContainsKey("provider"))
                    {
                        Console.WriteLine("migrate {0}: provider name is not specified.", options.Command);
                        return false;
                    }

                    if (!options.Options.ContainsKey("connection-string"))
                    {
                        Console.WriteLine("migrate {0}: connection string is not specified.", options.Command);
                        return false;
                    }

                    if (!options.Options.ContainsKey("directory"))
                    {
                        Console.WriteLine("migrate {0}: directory is not specified.", options.Command);
                        return false;                
                    }

                    return true;
                default:
                    Console.WriteLine("migrate: '{0}' is not a migrate command. See 'migrate help'.",options.Command);
                    return false;
            }
        }
    }
}
