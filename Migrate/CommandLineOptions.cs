using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace Migrate.Util
{
    /// <summary>
    /// 
    /// </summary>
    internal class CommandLineOptions
    {
        /// <summary>
        /// 
        /// </summary>
        public string Command
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, string> Options
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        public CommandLineOptions(string command)
        {
            if (command == null)
            {
                throw new ArgumentNullException("command");
            }

            Command = command;
            Options = new Dictionary<string, string>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
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
