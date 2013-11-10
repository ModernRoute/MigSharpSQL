using System.Data;

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
        bool SupportsTransactions { get; }

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
        string GetState(IDbConnection connection, out int substate);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>        
        /// <param name="transaction"></param>
        /// <param name="state"></param>
        void SetState(IDbConnection connection, IDbTransaction transaction, string state, int substate);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        IDbConnection CreateConnection(string connectionString);

        /// <summary>
        /// 
        /// </summary>
        void Load();
    }
}
