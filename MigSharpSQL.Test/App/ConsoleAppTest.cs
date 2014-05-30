using MigSharpSQL.App;
using NUnit.Framework;
using System;
using System.IO;

namespace MigSharpSQL.Test.App
{
    class ConsoleAppTest
    {
        private string[] _Args = new string[] 
        {
            "migrate",
            "last"
        };

        private const string _ValidConnectionStringKey = "ConnectionString";
        private const string _InvalidConnectionStringKey = "__ConnectionString__";
        private const string _ValidDirectoryKey = "MigrationsDirectory";
        private const string _InvalidDirectoryKey = "__MigrationsDirectory__";
        private const string _ValidProcessorKey = "MigrationProcessor";
        private const string _InvalidProcessorKey = "__MigrationProcessor__";

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void EntryPoint_ConfigDirectoryKeyNull_Fail()
        {
            ConsoleApp.EntryPoint(null, _ValidConnectionStringKey, _ValidProcessorKey, _Args);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void EntryPoint_ConfigConnectionStringNameKeyNull_Fail()
        {
            ConsoleApp.EntryPoint(_ValidDirectoryKey, null, _ValidProcessorKey, _Args);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void EntryPoint_ConfigProcessorKeyNull_Fail()
        {
            ConsoleApp.EntryPoint(_ValidDirectoryKey, _ValidConnectionStringKey, null, _Args);
        }

        [Test]
        public void EntryPoint_ConfigConnectionStringNameKeyIsAbsent_Fail()
        {
            int exitCode = ConsoleApp.EntryPoint(_ValidDirectoryKey, _InvalidConnectionStringKey, _ValidProcessorKey, _Args);

            Assert.AreEqual(ConsoleApp.FailedMigrationExitCode, exitCode);
        }

        [Test]
        public void EntryPoint_DirectoryKeyIsAbsent_Fail()
        {
            int exitCode = ConsoleApp.EntryPoint(_InvalidDirectoryKey, _ValidConnectionStringKey, _ValidProcessorKey, _Args);

            Assert.AreEqual(ConsoleApp.FailedMigrationExitCode, exitCode);
        }

        [Test]
        public void EntryPoint_ProcessorKeyIsAbsent_Fail()
        {
            int exitCode = ConsoleApp.EntryPoint(_ValidDirectoryKey, _ValidConnectionStringKey, _InvalidProcessorKey, _Args);

            Assert.AreEqual(ConsoleApp.FailedMigrationExitCode, exitCode);
        }
    }
}
