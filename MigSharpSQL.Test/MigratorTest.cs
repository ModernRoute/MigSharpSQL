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
        [SetUp]
        public void SetUp()
        {
            DbProviderFactory.Register(new MockProvider(true));
        }

        //Migrator mig = new Migrator("Mock","nevermind",);

        [Test]
        public void LoadingMigrations_BrokenMigration_NotExist()
        {
            Assert.Fail("Not implemented");
        }

        [Test]
        public void LoadingMigrations_BrokenMigration_Exist()
        {
            Assert.Fail("Not implemented");
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
