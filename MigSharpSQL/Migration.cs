using System;

namespace MigSharpSQL
{
    /// <summary>
    /// 
    /// </summary>
    internal class Migration
    {
        /// <summary>
        /// 
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string UpScriptFullPath
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string DownScriptFullPath
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
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
