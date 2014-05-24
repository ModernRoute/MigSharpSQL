using MigSharpSQL.Processors;
using MigSharpSQL.Resources;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MigSharpSQL
{
    /// <summary>
    /// Migration state processor factory.
    /// </summary>
    static public class DbMigrationStateProcessorFactory
    {
        /// <summary>
        /// Registers processor.
        /// </summary>
        /// <param name="processor">Processor to register.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="processor" /> 
        /// is null.</exception>
        /// <exception cref="ArgumentException">If <paramref name="processor" />
        /// is null.</exception>
        public static void Register(IDbMigrationStateProcessor processor)
        {
            if (processor == null)
            {
                throw new ArgumentNullException("processor");
            }

            if (processor.Name == null)
            {
                throw new ArgumentException(Strings.ProcessorNameCannotBeNull);
            }

            if (_Processors.ContainsKey(processor.Name))
            {
                _Processors.Remove(processor.Name);
            }

            _Processors.Add(processor.Name, processor);
        }

        /// <summary>
        /// Gets processor by its name.
        /// </summary>
        /// <param name="processorName">Unique processor name.</param>
        /// <returns><see cref="IDbMigrationStateProcessor"/> implementation associated with <paramref name="processorName"/>.</returns>
        /// <exception cref="ArgumentException">Processor with specified name does not exist.</exception>
        /// <exception cref="ArgumentNullException">If <paramref name="processorName" /> is null.</exception>
        public static IDbMigrationStateProcessor GetProcessor(string processorName)
        {
            if (processorName == null)
            {
                throw new ArgumentNullException("processorName");
            }

            lock (_Processors)
            {
                if (_Processors.ContainsKey(processorName))
                {
                    return _Processors[processorName];
                }

                throw new ArgumentException(Strings.ProcessorIsNotSupported, processorName);
            }
        }

        /// <summary>
        /// Available processors.
        /// </summary>
        public static IReadOnlyDictionary<string, IDbMigrationStateProcessor> Processors
        {
            get;
            private set;
        }

        /// <summary>
        /// Logger.
        /// </summary>
        private static Logger _Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Supported processors.
        /// </summary>
        private static IDictionary<string, IDbMigrationStateProcessor> _Processors;

        /// <summary>
        /// Initializes <see cref="DbMigrationStateProcessorFactory"/> class. Populates <see cref="_Processors"/> field.
        /// </summary>
        static DbMigrationStateProcessorFactory()
        {
            _Processors = new Dictionary<string, IDbMigrationStateProcessor>();

            Processors = new ReadOnlyDictionary<string, IDbMigrationStateProcessor>(_Processors);

            Register(new MySqlMigrationProcessor());
            Register(new SqliteMigrationProcessor());
        }
    }
}
