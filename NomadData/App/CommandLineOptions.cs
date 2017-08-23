using System;
using System.Collections.Generic;

namespace ModernRoute.NomadData.App
{
    internal class CommandLineOptions
    {
        public string Command
        {
            get;
            private set;
        }

        public Dictionary<string, string> Options
        {
            get;
            private set;
        }

        public CommandLineOptions(string command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            Command = command;
            Options = new Dictionary<string, string>();
        }

        public string GetOption(string key, string defaultValue = null)
        {
            if (!Options.ContainsKey(key))
            {
                return defaultValue;
            }

            return Options[key];
        }
    }
}
