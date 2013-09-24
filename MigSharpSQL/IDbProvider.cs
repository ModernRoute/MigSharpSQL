using System.Data;
using System.Data.Common;

namespace MigSharpSQL
{
    /// <summary>
    /// 
    /// </summary>
    public interface IDbProvider
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        string GetState(IDbConnection connection);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>        
        /// <param name="transaction"></param>
        /// <param name="state"></param>
        void SetState(IDbConnection connection, IDbTransaction transaction, string state);

        /// <summary>
        /// 
        /// </summary>
        bool SupportsTransactions { get; }

        /// <summary>
        /// 
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        IDbConnection CreateConnection(string connectionString);

        void Load();
    }
}
