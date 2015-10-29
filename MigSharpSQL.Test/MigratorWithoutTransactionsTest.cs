using NUnit.Framework;

namespace MigSharpSQL.Test
{
    [TestFixture]
    class MigratorWithoutTransactionsTest : MigratorTest
    {
        public MigratorWithoutTransactionsTest()
            : base()
        {
            DbMigrationStateProcessorFactory.Register(new MockProcessor(false));
        }

        [Test]
        public void SupportsTransactionsUsing_UpThroughFailMigration_CorrectState()
        {
            DoAndVerifyMigration_Fail("2013-10-12_10-10", 0, "2014-10-11_00-08", "2014-10-11_00-05", 5);
        }

        [Test]
        public void SupportsTransactionsUsing_DownThroughFailMigration_CorrectState()
        {
            DoAndVerifyMigration_Fail("2014-10-16_13-45", 0, "2013-10-12_10-10", "2014-10-11_00-08", 1);
        }
    }
}
