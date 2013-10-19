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
        private const string mig_bad_both_direction_5 = "5_bad_both_direction";
        private const string mig_no_down_script_4 = "4_no_down_script";
        private const string mig_no_up_script_4 = "4_no_up_script";

        public MigratorTest()
        {
            DbProviderFactory.Register(new MockProvider(true));
        }

        private static Migrator CreateMigrator(string migrationBulk)
        {
            string migDir = Path.Combine(Directory.GetCurrentDirectory(), migrationDir, migrationBulk);

            Migrator mig = new Migrator(mockProviderName, mockConnectionString, migDir);
            return mig;
        }

        private static void CheckState(Migrator mig, string expectedMigrationState, int expectedMigrationSubstate)
        {
            int migrationSubstate;
            string migrationState = mig.GetCurrentState(out migrationSubstate);

            Assert.AreEqual(expectedMigrationState == null ? "initial" : MockDbConnection.MigrationStateStatic, migrationState);
            Assert.AreEqual(expectedMigrationSubstate, migrationSubstate);
        }

        [Test]
        public void LoadingMigrations_BrokenMigration_NotExist()
        {
            Migrator mig = CreateMigrator(mig_ok_5);

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
            Migrator mig = CreateMigrator(mig_no_up_script_4);
        }

        [Test]
        [ExpectedException(typeof(InvalidDataException))]
        public void LoadingMigrations_BrokenMigration_ExistNoDown()
        {
            Migrator mig = CreateMigrator(mig_no_down_script_4);
        }

        [Test]
        public void GettingCurrentState_NotNull_OK()
        {
            const string state = "2013-10-12_10-09";
            const int substate = 3;

            MockDbConnection.MigrationStateStatic = state;
            MockDbConnection.MigrationSubstateStatic = substate;

            Migrator mig = CreateMigrator(mig_ok_5);

            CheckState(mig, state, substate);
        }

        [Test]
        public void GettingCurrentState_Null_OK()
        {
            const string state = null;
            const int substate = 0;

            MockDbConnection.MigrationStateStatic = state;
            MockDbConnection.MigrationSubstateStatic = substate;

            Migrator mig = CreateMigrator(mig_ok_5);

            CheckState(mig, state, substate);
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

        private static void DoAndVerifyMigration_Success(string currentState, int currentSubstate, string wantedState, int wantedSubstate, string migrationBulk)
        {
            MockDbConnection.MigrationStateStatic = currentState;
            MockDbConnection.MigrationSubstateStatic = currentSubstate;

            Migrator mig = CreateMigrator(migrationBulk);

            mig.MigrateTo(wantedState);

            CheckState(mig, wantedState, wantedSubstate);
        }

        [Test]
        public void MigrationToInitialState_InitialState_NoActionRequired()
        {
            DoAndVerifyMigration_Success(null, 0, null, 0, mig_ok_5);
        }

        [Test]
        public void MigrationToInitialState_MiddleState_DowngradeToInitial()
        {
            DoAndVerifyMigration_Success("2014-10-11_00-08", 0, null, 0, mig_ok_5);
        }

        [Test]
        public void MigrationToInitialState_MiddleStateAfterError_DowngradeToInitial()
        {
            DoAndVerifyMigration_Success("2014-10-11_00-08", 3, null, 0, mig_ok_5);
        }

        [Test]
        public void MigrationFromInitialState_InitialState_UpgradeToMiddle()
        {
            DoAndVerifyMigration_Success(null, 0, "2014-10-16_13-45", 0, mig_ok_5);
        }

        [Test]
        public void MigrationToMiddleState_MiddleState_Upgrade()
        {
            DoAndVerifyMigration_Success("2013-10-12_10-10", 0, "2014-10-11_00-08", 0, mig_ok_5);
        }

        [Test]
        public void MigrationToMiddleState_MiddleStateAfterError_Upgrade()
        {
            DoAndVerifyMigration_Success("2013-10-12_10-10", 2, "2014-10-11_00-08", 0, mig_ok_5);
        }

        [Test]
        public void MigrationToMiddleState_MiddleState_Downgrade()
        {
            DoAndVerifyMigration_Success("2014-10-11_00-08", 0, "2013-10-12_10-10", 0, mig_ok_5);
        }

        [Test]
        public void MigrationToMiddleState_MiddleStateAfterError_Downgrade()
        {
            DoAndVerifyMigration_Success("2014-10-11_00-08", 4, "2013-10-12_10-10", 0, mig_ok_5);
        }

        [Test]
        public void MigrationToLastState_MiddleState_OK()
        {
            MockDbConnection.MigrationStateStatic = "2013-10-12_10-09";
            MockDbConnection.MigrationSubstateStatic = 0;

            Migrator mig = CreateMigrator(mig_ok_5);

            mig.MigrateToLast();

            CheckState(mig, "2014-10-16_13-45", 0);
        }

        private static void DoAndVerifyMigration_Fail(string currentState, int currentSubstate, string wantedState, string newState, int newSubstate)
        {
            MockDbConnection.MigrationStateStatic = currentState;
            MockDbConnection.MigrationSubstateStatic = currentSubstate;

            Migrator mig = CreateMigrator(mig_bad_both_direction_5);

            try
            {
                mig.MigrateTo(wantedState);

                Assert.Fail("MockDbException was not thrown");
            }
            catch (MockDbException)
            {
                // ok
            }

            CheckState(mig, newState, newSubstate);
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
    }
}
