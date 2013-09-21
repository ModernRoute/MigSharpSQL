using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigSharpSQL
{
    /// <summary>
    /// 
    /// </summary>
    internal interface IDbProvider
    {
        /// <summary>
        /// 
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        string GetState(DbConnection connection);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        void SetState(DbConnection connection);

        /// <summary>
        /// 
        /// </summary>
        bool SupportsTransactions { get; }
    }
}
