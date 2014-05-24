using MigSharpSQL.Exceptions;
using MigSharpSQL.Test.Provider;
using NUnit.Framework;
using System;
using System.IO;

namespace MigSharpSQL.Test
{
    [TestFixture]
    class MigratorWithTransactionTest : MigratorTest
    {
        public MigratorWithTransactionTest()
            : base()
        {
            DbMigrationStateProcessorFactory.Register(new MockProcessor(true));
        }      

        [Test]
        public void LoadingMigrations_BrokenMigration_NotExist()
        {
            Migrator mig = CreateMigrator(Constants.MigOk5);

            string[] migrationNames = mig.GetMigrationNames();

            Assert.AreEqual(5, migrationNames.Length);
            Assert.AreEqual(migrationNames[0], "2013-10-12_10-09");
            Assert.AreEqual(migrationNames[1], "2013-10-12_10-10");
            Assert.AreEqual(migrationNames[2], "2014-10-11_00-05");
            Assert.AreEqual(migrationNames[3], "2014-10-11_00-08");
            Assert.AreEqual(migrationNames[4], "2014-10-16_13-45");
        }

        [Test]
        [ExpectedException(typeof(FileNotFoundException))]
        public void LoadingMigrations_BrokenMigration_ExistNoUp()
        {
            Migrator mig = CreateMigrator(Constants.MigNoUpScript4);
        }

        [Test]
        [ExpectedException(typeof(FileNotFoundException))]
        public void LoadingMigrations_BrokenMigration_ExistNoDown()
        {
            Migrator mig = CreateMigrator(Constants.MigNoDownScript4);
        }

        [Test]
        public void GettingCurrentState_NotNull_OK()
        {
            const string state = "2013-10-12_10-09";
            const int substate = 3;

            MockDbConnection.MigrationStateStatic = state;
            MockDbConnection.MigrationSubstateStatic = substate;

            Migrator mig = CreateMigrator(Constants.MigOk5);

            CheckState(mig, state, substate);
        }

        [Test]
        public void GettingCurrentState_Null_OK()
        {
            const string state = initialState;
            const int substate = 0;

            MockDbConnection.MigrationStateStatic = null;
            MockDbConnection.MigrationSubstateStatic = substate;

            Migrator mig = CreateMigrator(Constants.MigOk5);

            CheckState(mig, state, substate);
        }

        [Test]
        public void MigrationToInitialState_InitialState_NoActionRequired()
        {
            DoAndVerifyMigration_Success(null, 0, initialState, 0, Constants.MigOk5);
        }

        [Test]
        public void MigrationToInitialState_MiddleState_DowngradeToInitial()
        {
            DoAndVerifyMigration_Success("2014-10-11_00-08", 0, initialState, 0, Constants.MigOk5);
        }

        [Test]
        public void MigrationToInitialState_MiddleStateAfterError_DowngradeToInitial()
        {
            DoAndVerifyMigration_Success("2014-10-11_00-08", 3, initialState, 0, Constants.MigOk5);
        }

        [Test]
        public void MigrationFromInitialState_InitialState_UpgradeToMiddle()
        {
            DoAndVerifyMigration_Success(null, 0, "2014-10-16_13-45", 0, Constants.MigOk5);
        }

        [Test]
        public void MigrationToMiddleState_MiddleState_Upgrade()
        {
            DoAndVerifyMigration_Success("2013-10-12_10-10", 0, "2014-10-11_00-08", 0, Constants.MigOk5);
        }

        [Test]
        public void MigrationToMiddleState_MiddleStateAfterError_Upgrade()
        {
            DoAndVerifyMigration_Success("2013-10-12_10-10", 2, "2014-10-11_00-08", 0, Constants.MigOk5);
        }

        [Test]
        public void MigrationToMiddleState_MiddleState_Downgrade()
        {
            DoAndVerifyMigration_Success("2014-10-11_00-08", 0, "2013-10-12_10-10", 0, Constants.MigOk5);
        }

        [Test]
        public void MigrationToMiddleState_MiddleStateAfterError_Downgrade()
        {
            DoAndVerifyMigration_Success("2014-10-11_00-08", 4, "2013-10-12_10-10", 0, Constants.MigOk5);
        }

        [Test]
        public void MigrationToLastState_MiddleState_OK()
        {
            MockDbConnection.MigrationStateStatic = "2013-10-12_10-09";
            MockDbConnection.MigrationSubstateStatic = 0;

            Migrator mig = CreateMigrator(Constants.MigOk5);

            mig.MigrateTo(lastState);

            CheckState(mig, "2014-10-16_13-45", 0);
        }

        [Test]
        public void MigrationToMiddleStateAfterError_Up_OK()
        {
            DoAndVerifyMigration_Fail("2013-10-12_10-10", 0, "2014-10-11_00-08", "2014-10-11_00-05", 5);
        }

        [Test]
        public void MigrationToMiddleStateAfterError_Down_OK()
        {
            DoAndVerifyMigration_Fail("2014-10-16_13-45", 0, "2013-10-12_10-10", "2014-10-11_00-08", 1);
        }

        [Test]
        public void MigrationToLastState_EmptyMigrationListInitialState_OK()
        {
            MockDbConnection.MigrationStateStatic = null;
            MockDbConnection.MigrationSubstateStatic = 0;

            Migrator mig = CreateMigrator(Constants.MigOk0);

            mig.MigrateTo(lastState);

            CheckState(mig, initialState, 0);
        }

        [Test]
        [ExpectedException(typeof(MigrationException))]
        public void MigrationUp_InvalidSubstate_Fail()
        {
            DoAndVerifyMigration_Success("2013-10-12_10-10", 6, "2014-10-11_00-08", 0, Constants.MigOk5);
        }

        [Test]
        [ExpectedException(typeof(MigrationException))]
        public void MigrationDown_InvalidSubstate_Fail()
        {
            DoAndVerifyMigration_Success("2014-10-11_00-08", 9, "2013-10-12_10-10", 0, Constants.MigOk5);
        }

        [Test]
        public void MigrationDown_FromInvalidSubstateToCurrentState_Fail()
        {
            DoAndVerifyMigration_Fail("2014-10-11_00-08", 0, "2013-10-12_10-10", "2014-10-11_00-08", 1);
        }

        private const string initialState = "initial";
        private const string lastState = "last";
    }
}
