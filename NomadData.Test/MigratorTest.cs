using ModernRoute.NomadData.Exceptions;
using ModernRoute.NomadData.Test.Helpers;
using ModernRoute.NomadData.Test.Provider;
using NUnit.Framework;
using System.IO;
using System.Reflection;

namespace ModernRoute.NomadData.Test
{
    class MigratorTest
    {
        [Test]
        public void UnsupportedProvider_Fail()
        {
            Assert.Throws<MigrationException>(() => new Migrator(string.Empty, "__UnsupportedProvider__", MockProcessor.ProcessorName, Constants.MigOk5));
        }

        [Test]
        public void UnsupportedProcessor_Fail()
        {
            Assert.Throws<MigrationException>(() => new Migrator(string.Empty, Constants.MockProviderName, "__UnsupportedProcessor__", Constants.MigOk5));
        }

        private string GetMigrationDirectory(string migrationBulk)
        {
            return Path.Combine(Assembly.GetExecutingAssembly().GetDirectory(), Constants.MigrationDir, migrationBulk);
        }

        protected Migrator CreateMigrator(string currentState, int currentSubstate, string migrationBulk, string baseMigrationBulk)
        {
            string migDir = GetMigrationDirectory(migrationBulk);
            string baseMigDir = GetMigrationDirectory(baseMigrationBulk);

            Migrator mig = new Migrator(migrationBulk, Constants.MockProviderName, MockProcessor.ProcessorName, migDir);

            MockDatabase database = MockDatabase.GetInstance(migrationBulk);

            database.SetHistory(currentState, currentSubstate, Migrator.LoadMigrations(baseMigDir));

            return mig;
        }

        protected Migrator CreateMigrator(string currentState, int currentSubstate, string migrationBulk)
        {
            return CreateMigrator(currentState, currentSubstate, migrationBulk, migrationBulk);
        }

        protected void CheckState(Migrator mig, string expectedMigrationState, int expectedMigrationSubstate)
        {
            int migrationSubstate;
            string migrationState = mig.GetCurrentState(out migrationSubstate);

            Assert.AreEqual(expectedMigrationState, migrationState);
            Assert.AreEqual(expectedMigrationSubstate, migrationSubstate);
        }

        protected void DoAndVerifyMigration_Success(string migrationBulk, string baseMigrationBulk, string currentState, int currentSubstate,
            string wantedState)
        {
            Migrator mig = CreateMigrator(currentState, currentSubstate, migrationBulk, baseMigrationBulk);

            mig.MigrateTo(wantedState);

            CheckState(mig, wantedState, 0);
        }

        protected void DoAndVerifyMigration_Success(string migrationBulk, string currentState, int currentSubstate, 
            string wantedState)
        {
            DoAndVerifyMigration_Success(migrationBulk, migrationBulk, currentState, currentSubstate, wantedState);
        }

        protected void DoAndVerifyMigration_Fail(string migrationBulk, string currentState, int currentSubstate,
            string wantedState, string actualState, int actualSubstate)
        {
            Migrator mig = CreateMigrator(currentState, currentSubstate, migrationBulk);

            Assert.Throws<MockDbException>(() => mig.MigrateTo(wantedState));

            CheckState(mig, actualState, actualSubstate);
        }
    }
}
