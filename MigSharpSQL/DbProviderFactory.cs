using MigSharpSQL.Providers;
using NLog;
using System;
using System.Collections.Generic;

namespace MigSharpSQL
{
    /// <summary>
    /// Database providers factory.
    /// </summary>
    static public class DbProviderFactory
    {
        /// <summary>
        /// Registers database provider.
        /// </summary>
        /// <param name="provider">Provider to register.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="provider" /> 
        /// is null.</exception>
        /// <exception cref="ArgumentException">If <paramref name="provider" />
        /// is null.</exception>
        public static void Register(IDbProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("provider");
            }

            if (provider.Name == null)
            {
                throw new ArgumentException("Provider name cannot be null");
            }

            if (providers.ContainsKey(provider.Name))
            {
                providers.Remove(provider.Name);
            }

            providers.Add(provider.Name, provider);
        }

        /// <summary>
        /// Gets provider by its name.
        /// </summary>
        /// <param name="providerName">Unique provider name.</param>
        /// <returns><see cref="IDbProvider"/> implementation associated with <paramref name="providerName"/>.</returns>
        /// <exception cref="NotSupportedException">Provider with specified name does not exist.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="providerName" /> is null.</exception>
        public static IDbProvider GetProvider(string providerName)
        {
            if (providerName == null)
            {
                throw new ArgumentNullException("providerName");
            }

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

        /// <summary>
        /// Logger.
        /// </summary>
        private static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Supported providers.
        /// </summary>
        private static Dictionary<string, IDbProvider> providers;

        /// <summary>
        /// Initializes <see cref="DbProviderFactory"/> class. Populates <see cref="providers"/> field.
        /// </summary>
        static DbProviderFactory()
        {
            providers = new Dictionary<string, IDbProvider>();

            Register(new MySqlProvider());
        }
    }
}
