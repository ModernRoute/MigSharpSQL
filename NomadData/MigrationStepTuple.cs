using System;

namespace ModernRoute.NomadData
{
    public class MigrationStepTuple
    {
        public string Up
        {
            get;
            private set;
        }

        public string Down
        {
            get;
            private set;
        }

        public MigrationStepTuple(string up, string down)
        {
            if (up == null)
            {
                throw new ArgumentNullException(nameof(up));
            }

            if (down == null)
            {
                throw new ArgumentNullException(nameof(down));
            }

            Up = up;
            Down = down;
        }
    }
}
