using MigSharpSQL.Test.Provider;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MigSharpSQL.Test
{
    [TestFixture]
    class MigratorTest
    {
        private const string mockProviderName = "Mock";
        private const string mockConnectionString = "nevermind";

        private const string migrationDir = "Migrations";

        private const string mig_ok_5 = "5_ok";
        private const string mig_no_down_script_4 = "4_no_down_script";
        private const string mig_no_up_script_4 = "4_no_up_script";

        public MigratorTest()
        {
            DbProviderFactory.Register(new MockProvider(true));
        }

        [Test]
        public void LoadingMigrations_BrokenMigration_NotExist()
        {
            string migDir = Path.Combine(Directory.GetCurrentDirectory(), migrationDir, mig_ok_5);

            Migrator mig = new Migrator(mockProviderName, mockConnectionString, migDir);

            string[] migrationNames = mig.GetMigrationNames();

            Assert.AreEqual(5, migrationNames.Length);
            Assert.AreEqual(migrationNames[0], "2013-10-12_10-09");
            Assert.AreEqual(migrationNames[1], "2013-10-12_10-10");
            Assert.AreEqual(migrationNames[2], "2014-10-11_00-05");
            Assert.AreEqual(migrationNames[3], "2014-10-11_00-08");
            Assert.AreEqual(migrationNames[4], "2014-10-16_13-45");
        }

        [Test]
        [ExpectedException(typeof(InvalidDataException))]
        public void LoadingMigrations_BrokenMigration_ExistNoUp()
        {
            string migDir = Path.Combine(Directory.GetCurrentDirectory(), migrationDir, mig_no_up_script_4);

            Migrator mig = new Migrator(mockProviderName, mockConnectionString, migDir);
        }

        [Test]
        [ExpectedException(typeof(InvalidDataException))]
        public void LoadingMigrations_BrokenMigration_ExistNoDown()
        {
            string migDir = Path.Combine(Directory.GetCurrentDirectory(), migrationDir, mig_no_down_script_4);

            Migrator mig = new Migrator(mockProviderName, mockConnectionString, migDir);
        }

        [Test]
        public void GettingCurrentState_NotNull_OK()
        {
            Assert.Fail("Not implemented");
        }

        [Test]
        public void GettingCurrentState_Null_OK()
        {
            Assert.Fail("Not implemented");
        }

        [Test]
        public void SupportsTransactionsUsing_UpThroughFailMigration_CorrectState()
        {
            Assert.Fail("Not implemented");
        }

        [Test]
        public void SupportsTransactionsUsing_DownThroughFailMigration_CorrectState()
        {
            Assert.Fail("Not implemented");
        }

        [Test]
        public void MigrationToInitialState_InitialState_NoActionRequired()
        {
            Assert.Fail("Not implemented");
        }

        [Test]
        public void MigrationToInitialState_MiddleState_DowngradeToInitial()
        {
            Assert.Fail("Not implemented");
        }

        [Test]
        public void MigrationToInitialState_MiddleStateAfterError_DowngradeToInitial()
        {
            Assert.Fail("Not implemented");
        }

        [Test]
        public void MigrationFromInitialState_InitialState_UpgradeToMiddle()
        {
            Assert.Fail("Not implemented");
        }

        [Test]
        public void MigrationToMiddleState_MiddleState_Upgrade()
        {
            Assert.Fail("Not implemented");
        }

        [Test]
        public void MigrationToMiddleState_MiddleStateAfterError_Upgrade()
        {
            Assert.Fail("Not implemented");
        }

        [Test]
        public void MigrationToMiddleState_MiddleState_Downgrade()
        {
            Assert.Fail("Not implemented");
        }

        [Test]
        public void MigrationToMiddleState_MiddleStateAfterError_Downgrade()
        {
            Assert.Fail("Not implemented");
        }

        [Test]
        public void MigrationToLastState_MiddleState_OK()
        {
            Assert.Fail("Not implemented");
        }

        [Test]
        public void MigrationToMiddleStateAfterError_Up_OK()
        {
            Assert.Fail("Not implemented");
        }

        [Test]
        public void MigrationToMiddleStateAfterError_Down_OK()
        {
            Assert.Fail("Not implemented");
        }
    }
}
