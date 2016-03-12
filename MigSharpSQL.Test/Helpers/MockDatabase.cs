using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MigSharpSQL.Test.Helpers
{
    class MockDatabase
    {
        private static ConcurrentDictionary<string, MockDatabase> _Instances = new ConcurrentDictionary<string, MockDatabase>();

        private IList<MigrationHistoryItem> _History;
        private IList<MigrationHistoryItem> _HistoryInTransaction;

        public string Name
        {
            get;
            private set;
        }

        private MockDatabase(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            _History = new List<MigrationHistoryItem>();
        }

        public void BeginTransaction()
        {
            Monitor.Enter(_History);
            _HistoryInTransaction = new List<MigrationHistoryItem>();
        }

        public void Commit()
        {
            CheckTransaction();

            lock (_History)
            {
                foreach (MigrationHistoryItem item in _HistoryInTransaction)
                {
                    _History.Add(item);
                }

                _HistoryInTransaction.Clear();
            }
        }

        internal void SetHistory(string currentState, int currentSubstate, SortedDictionary<string, Migration> migrations)
        {
            if (migrations == null)
            {
                throw new ArgumentNullException(nameof(migrations));
            }

            lock (_History)
            {
                if (_HistoryInTransaction != null)
                {
                    throw new InvalidOperationException("Unable to initialize history in transaction.");
                }

                if (currentState != null)
                {
                    if (!migrations.ContainsKey(currentState))
                    {
                        throw new InvalidOperationException("The current state is not a valid.");
                    }

                    if (currentSubstate > migrations[currentState].Steps.Count - 1)
                    {
                        throw new InvalidOperationException("The current substate is not valid.");
                    }
                }

                _History.Clear();

                if (currentState == null)
                {
                    return;
                }

                DateTime dateTime = DateTime.UtcNow.AddSeconds(-1);

                foreach (KeyValuePair<string, Migration> state in migrations)
                {
                    dateTime = dateTime.AddSeconds(state.Value.Steps.Count);
                }

                foreach (KeyValuePair<string, Migration> state in migrations)
                {
                    int substate;
                    bool last;
                    
                    if (state.Key == currentState)
                    {
                        substate = currentSubstate;
                        last = true;
                    }
                    else
                    {
                        substate = 0;
                        last = false;
                    }
                    
                    for (int i = state.Value.Steps.Count - 1; i >= currentSubstate; i--)
                    {
                        _History.Add(new MigrationHistoryItem(dateTime, state.Key, i, true));
                    }

                    dateTime = dateTime.AddSeconds(1);

                    if (last)
                    {
                        break;
                    }
                }
            }
        }

        public void Rollback()
        {
            CheckTransaction();

            lock (_History)
            {
                _HistoryInTransaction.Clear();
            }
        }

        private void CheckTransaction()
        {
            if (_HistoryInTransaction == null)
            {
                throw new InvalidOperationException("Not in transaction state.");
            }
        }

        public void EndTransaction()
        {
            _HistoryInTransaction = null;
            Monitor.Exit(_History);
        }

        public void AddHistory(IEnumerable<SimpleMigrationHistoryItem> history)
        {
            if (history == null)
            {
                throw new ArgumentNullException(nameof(history));
            }

            lock (_History)
            {
                IList<MigrationHistoryItem> list = GetHistoryList();

                foreach (MigrationHistoryItem item in history.Select(m => new MigrationHistoryItem(DateTime.UtcNow, m.State, m.Substate, true)))
                {
                    list.Add(item);
                }
            }
        }

        private IList<MigrationHistoryItem> GetHistoryList()
        {
            IList<MigrationHistoryItem> list;

            if (_HistoryInTransaction != null)
            {
                list = _HistoryInTransaction;
            }
            else
            {
                list = _History;
            }

            return list;
        }

        public string GetState(out int substate)
        {
            MigrationHistoryItem item;

            if (_HistoryInTransaction != null && _HistoryInTransaction.Count > 0)
            {
                item = _HistoryInTransaction[_HistoryInTransaction.Count - 1];
                substate = item.Substate;

                return item.State;
            }

            if (_History.Count <= 0)
            {
                substate = 0;

                return null;
            }

            item = _History[_History.Count - 1];
            substate = item.Substate;

            return item.State;
        }

        public void ToNewState(string state, int substate, bool isUp)
        {
            lock (_History)
            {
                MigrationHistoryItem item = new MigrationHistoryItem(DateTime.UtcNow, state, substate, isUp);

                GetHistoryList().Add(item);
            }
        }

        public IEnumerable<MigrationHistoryItem> GetHistory()
        {
            lock (_History)
            {
                foreach (MigrationHistoryItem item in _History)
                {
                    yield return item;
                }
            }
        }

        public static MockDatabase GetInstance(string name)
        {
            return _Instances.GetOrAdd(name, s => new MockDatabase(s));
        }
    }
}
