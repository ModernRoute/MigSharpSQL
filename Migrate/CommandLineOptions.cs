using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace Migrate.Util
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
                throw new ArgumentNullException("command");
            }

            Command = command;
            Options = new Dictionary<string, string>();
        }

        public string GetOption(string key, string defaultValue)
        {
            if (!Options.ContainsKey(key))
            {
                return defaultValue;
            }

            return Options[key];
        }
    }
}
