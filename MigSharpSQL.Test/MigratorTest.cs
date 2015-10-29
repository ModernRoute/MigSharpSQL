﻿using MigSharpSQL.Exceptions;
using MigSharpSQL.Test.Provider;
using NUnit.Framework;
using System.IO;

namespace MigSharpSQL.Test
{
    class MigratorTest
    {
        [Test]
        [ExpectedException(typeof(MigrationException))]
        public void UnsupportedProvider_Fail()
        {
            new Migrator(Constants.MockConnectionString, "__UnsupportedProvider__", MockProcessor.ProcessorName, Constants.MigOk5);
        }

        [Test]
        [ExpectedException(typeof(MigrationException))]
        public void UnsupportedProcessor_Fail()
        {
            new Migrator(Constants.MockConnectionString, Constants.MockProviderName, "__UnsupportedProcessor__", Constants.MigOk5);
        }

        protected Migrator CreateMigrator(string migrationBulk)
        {
            string migDir = Path.Combine(Directory.GetCurrentDirectory(), Constants.MigrationDir, migrationBulk);

            Migrator mig = new Migrator(Constants.MockConnectionString, Constants.MockProviderName, MockProcessor.ProcessorName, migDir);

            return mig;
        }

        protected void CheckState(Migrator mig, string expectedMigrationState, int expectedMigrationSubstate)
        {
            int migrationSubstate;
            string migrationState = mig.GetCurrentState(out migrationSubstate);

            Assert.AreEqual(expectedMigrationState, migrationState);
            Assert.AreEqual(expectedMigrationSubstate, migrationSubstate);
        }

        protected void DoAndVerifyMigration_Success(string currentState, int currentSubstate, string wantedState, 
            int wantedSubstate, string migrationBulk)
        {
            MockDbConnection.MigrationStateStatic = currentState;
            MockDbConnection.MigrationSubstateStatic = currentSubstate;

            Migrator mig = CreateMigrator(migrationBulk);

            mig.MigrateTo(wantedState);

            CheckState(mig, wantedState, wantedSubstate);
        }

        protected void DoAndVerifyMigration_Fail(string currentState, int currentSubstate, string wantedState, 
            string newState, int newSubstate)
        {
            MockDbConnection.MigrationStateStatic = currentState;
            MockDbConnection.MigrationSubstateStatic = currentSubstate;

            Migrator mig = CreateMigrator(Constants.MigBadBothDirection5);

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
    }
}
