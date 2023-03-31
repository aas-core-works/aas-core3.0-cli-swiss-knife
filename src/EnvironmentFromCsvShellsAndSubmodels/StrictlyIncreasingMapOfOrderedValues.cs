using System.Collections;  // can't alias
using System.Collections.Generic;  // can't alias

namespace EnvironmentFromCsvShellsAndSubmodels
{
    public class StrictlyIncreasingMapOfOrderedValues<TKey, TValue>
        : IEnumerable<KeyValuePair<TKey, IReadOnlyList<TValue>>> where TKey : notnull
    {
        private readonly Dictionary<TKey, List<TValue>> _lists = new();

        private readonly Dictionary<TKey, HashSet<TValue>> _sets = new();

        public void Add(TKey key, TValue value)
        {
            _lists.TryGetValue(key, out var list);
            if (list == null)
            {
                list = new List<TValue>();
                _lists.Add(key, list);
            }

            list.Add(value);

            _sets.TryGetValue(key, out var set);
            if (set == null)
            {
                set = new HashSet<TValue>();
                _sets.Add(key, set);
            }

            set.Add(value);
        }

        public bool TryGetValues(TKey key, out IReadOnlyList<TValue>? values)
        {
            bool has = _lists.TryGetValue(key, out var mutableList);
            values = mutableList;
            return has;
        }

        public IEnumerator<KeyValuePair<TKey, IReadOnlyList<TValue>>> GetEnumerator()
        {
            foreach (var keyValue in _lists)
            {
                yield return new KeyValuePair<TKey, IReadOnlyList<TValue>>(
                    keyValue.Key, keyValue.Value
                );
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}