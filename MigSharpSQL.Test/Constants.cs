namespace MigSharpSQL.Test
{
    static class Constants
    {
        public const string MockConnectionString = "nevermind";

        public const string MigrationDir = "Migrations";

        public const string MigOk5 = "5_ok";
        public const string MigOk0 = "0_ok";
        public const string MigBadBothDirection5 = "5_bad_both_direction";
        public const string MigNoDownScript4 = "4_no_down_script";
        public const string MigNoUpScript4 = "4_no_up_script";
    }
}
