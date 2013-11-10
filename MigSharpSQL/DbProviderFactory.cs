using MigSharpSQL.Providers;
using NLog;
using System;
using System.Collections.Generic;

namespace MigSharpSQL
{
    /// <summary>
    /// 
    /// </summary>
    static public class DbProviderFactory
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="provider"></param>
        public static void Register(IDbProvider provider)
        {
            if (providers.ContainsKey(provider.Name))
            {
                providers.Remove(provider.Name);
            }

            providers.Add(provider.Name, provider);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="providerName"></param>
        /// <returns></returns>
        public static IDbProvider GetProvider(string providerName)
        {
            lock (providers)
            {
                if (providers.ContainsKey(providerName))
                {
                    return providers[providerName];
                }

                throw new NotSupportedException(
                    string.Format(
                        "Provider {0} is not supported. The following providers are supported: {1}.",
                        providerName,
                        string.Join(", ", providers.Keys)
                        )
                    );
            }
        }

        private static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Supported providers
        /// </summary>
        private static Dictionary<string, IDbProvider> providers;

        /// <summary>
        /// 
        /// </summary>
        static DbProviderFactory()
        {
            providers = new Dictionary<string, IDbProvider>();

            Register(new MySqlProvider());
        }
    }
}
