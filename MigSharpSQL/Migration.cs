using System;

namespace MigSharpSQL
{
    /// <summary>
    /// Represents the one migration.
    /// </summary>
    internal class Migration
    {
        /// <summary>
        /// Gets migration unique name.
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// Full path to up script.
        /// </summary>
        public string UpScriptFullPath
        {
            get;
            set;
        }

        /// <summary>
        /// Full path to down script.
        /// </summary>
        public string DownScriptFullPath
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Migration"/> class.
        /// </summary>
        /// <param name="name">Migration name.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="name"/> is null.</exception>
        public Migration(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            Name = name;
        }
    }
}
