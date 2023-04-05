using Aas = AasCore.Aas3_0; // renamed

// ReSharper disable RedundantUsingDirective
using System.Collections.Generic;  // can't alias

namespace Registering
{
    public class TypedRegistry<T> where T : Aas.IIdentifiable
    {
        private readonly List<T> _items = new();
        private readonly Dictionary<string, T> _map = new();

        public void Add(T instance)
        {
            if (TryGet(instance.Id) != null)
            {
                throw new System.ArgumentException(
                    "An instance with " +
                    $"the same ID already contained: {instance.Id}"
                );
            }

            _items.Add(instance);
            _map[instance.Id] = instance;
        }

        public T MustGet(string identifier)
        {
            _map.TryGetValue(identifier, out T? result);
            if (result == null)
            {
                throw new KeyNotFoundException(identifier);
            }

            return result;
        }

        public T? TryGet(string identifier)
        {
            _map.TryGetValue(identifier, out T? result);
            return result;
        }

        public IReadOnlyCollection<T> Items => _items;
    }
}