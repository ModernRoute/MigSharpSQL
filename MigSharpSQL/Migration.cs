using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MigSharpSQL
{
    public class Migration
    {
        public string Name
        {
            get;
            private set;
        }

        public IReadOnlyList<MigrationStepTuple> Steps
        {
            get;
            private set;
        }

        public Migration(string name, IEnumerable<MigrationStepTuple> steps)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (steps == null)
            {
                throw new ArgumentNullException(nameof(steps));
            }

            if (steps.Any(step => step == null))
            {
                throw new ArgumentException("", nameof(steps)); // TODO: message
            }

            Name = name;
            Steps = new ReadOnlyCollection<MigrationStepTuple>(steps.ToList());
        }
    }
}
