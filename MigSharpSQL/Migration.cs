using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public string UpScriptFullPath
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string DownScriptFullPath
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="upScriptFullPath"></param>
        /// <param name="downScriptFullPath"></param>
        public Migration(string upScriptFullPath, string downScriptFullPath)
        {
            if (upScriptFullPath == null)
            {
                throw new ArgumentNullException("upScriptFullPath");
            }

            UpScriptFullPath = UpScriptFullPath;
            DownScriptFullPath = DownScriptFullPath;
        }
    }
}
