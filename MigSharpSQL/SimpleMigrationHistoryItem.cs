namespace MigSharpSQL
{
    public class SimpleMigrationHistoryItem
    {
        public string State
        {
            get;
            private set;
        }

        public int Substate
        {
            get;
            private set;
        }

        public SimpleMigrationHistoryItem(string state, int substate)
        {
            State = state;
            Substate = substate;
        }
    }
}
