using System;

namespace ModernRoute.NomadData
{
    public class MigrationHistoryItem
    {
        public DateTime When
        {
            get;
            private set;
        }
 
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

        public bool IsUp
        {
            get;
            private set;
        }

        public MigrationHistoryItem(DateTime when, string state, int substate, bool isUp)
        {
            When = when;
            State = state;
            Substate = substate;
            IsUp = isUp;
        }
    }
}
