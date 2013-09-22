using System.Data.Common;

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
        /// <param name="transaction"></param>
        /// <returns></returns>
        string GetState(DbConnection connection);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>        
        /// <param name="transaction"></param>
        /// <param name="state"></param>
        void SetState(DbConnection connection, DbTransaction transaction, string state);

        /// <summary>
        /// 
        /// </summary>
        bool SupportsTransactions { get; }
    }
}
