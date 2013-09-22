using MigSharpSQL.Providers;
using System;
using System.Collections.Generic;

namespace MigSharpSQL
{
    /// <summary>
    /// 
    /// </summary>
    static internal class DbProviderFactory
    {
        /// <summary>
        /// Supported providers
        /// </summary>
        private static Dictionary<string, IDbProvider> providers;

        /// <summary>
        /// 
        /// </summary>
        static DbProviderFactory()
        {
            IDbProvider[] providersArray = new IDbProvider[] 
            {
                new MySqlProvider()

                // Put additional providers here
            };

            providers = new Dictionary<string,IDbProvider>();

            foreach (IDbProvider provider in providersArray)
            {
                providers.Add(provider.Name, provider);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="providerName"></param>
        /// <returns></returns>
        public static IDbProvider GetProvider(string providerName)
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
}
