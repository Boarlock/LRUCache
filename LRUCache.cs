using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace LRU
{
    public readonly record struct CacheEntry<TKey, TValue>(TKey Key, TValue Value);

    public sealed class Cache<TKey, TValue> : IEnumerable<CacheEntry<TKey, TValue>>
        where TKey : notnull
    {
        private readonly object syncRoot = new();
        private int capacity;

        private readonly LinkedList<CacheEntry<TKey, TValue>> priorities = new();
        private readonly Dictionary<TKey, LinkedListNode<CacheEntry<TKey, TValue>>> values = new();

        public int Capacity
        {
            get
            {
                lock (syncRoot)
                    return capacity;
            }

            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);

                lock (syncRoot)
                {
                    UpdateCapacity(value);
                    capacity = value;
                }
            }
        }

        public int Count
        {
            get
            {
                lock (syncRoot)
                    return values.Count;
            }
        }

        public Cache(int capacity)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(capacity);
            this.capacity = capacity;
        }

        // Add a new item or update an existing item.
        public void Add(TKey item, TValue value)
        {
            lock (syncRoot)
            {
                // Zero capacity
                if (capacity == 0)
                    return;

                // Existing entry
                if (values.TryGetValue(item, out LinkedListNode<CacheEntry<TKey, TValue>>? node))
                {
                    node.Value = new CacheEntry<TKey, TValue>(item, value);

                    UpdatePriority(node);
                    return;
                }

                // Cache is full
                if (values.Count >= capacity)
                {
                    LinkedListNode<CacheEntry<TKey, TValue>> oldNode = priorities.First!;

                    values.Remove(oldNode.Value.Key);
                    oldNode.Value = new CacheEntry<TKey, TValue>(item, value);
                    values.Add(item, oldNode);

                    UpdatePriority(oldNode);
                    return;
                }

                // Cache has available capacity.
                CacheEntry<TKey, TValue> newEntry = new(item, value);

                LinkedListNode<CacheEntry<TKey, TValue>> newNode = priorities.AddLast(newEntry);
                values.Add(item, newNode);
            }
        }

        public void Remove(TKey item)
        {
            lock (syncRoot)
            {
                if (!values.TryGetValue(item, out LinkedListNode<CacheEntry<TKey, TValue>>? node))
                    return;

                values.Remove(node.Value.Key);
                priorities.Remove(node);
            }
        }

        // Counts as access and updates priority
        public bool TryGetValue(TKey item, [MaybeNullWhen(false)] out TValue itemValue)
        {
            lock (syncRoot)
            {
                itemValue = default;

                if (!values.TryGetValue(item, out LinkedListNode<CacheEntry<TKey, TValue>>? node))
                    return false;

                itemValue = node.Value.Value;

                UpdatePriority(node);
                return true;
            }
        }

        // Retrieves an item without updating its priority.
        [return: MaybeNull]
        public TValue Peek(TKey item)
        {
            lock (syncRoot)
            {
                if (!values.TryGetValue(item, out LinkedListNode<CacheEntry<TKey, TValue>>? node))
                    return default;

                return node.Value.Value;
            }
        }

        public void Clear()
        {
            lock (syncRoot)
            {
                priorities.Clear();
                values.Clear();
            }
        }

        public bool ContainsKey(TKey item)
        {
            lock (syncRoot)
                return values.ContainsKey(item);
        }

        public bool ContainsValue(TValue value)
        {
            lock (syncRoot)
            {
                foreach (CacheEntry<TKey, TValue> entry in priorities)
                {
                    if (EqualityComparer<TValue>.Default.Equals(entry.Value, value))
                        return true;
                }

                return false;
            }
        }

        // Doesn't directly free memory, simply changes the capacity to the current count so capacity cannot excced that
        public void TrimExcess()
        {
            lock (syncRoot)
            {
                capacity = values.Count;
            }
        }

        public int EnsureCapacity(int capacity)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(capacity);

            lock (syncRoot)
            {
                if (this.capacity < capacity)
                    this.capacity = capacity;

                return this.capacity;
            }
        }

        // Returns a snapshot of the cache in eviction order.
        public IEnumerator<CacheEntry<TKey, TValue>> GetEnumerator()
        {
            CacheEntry<TKey, TValue>[] snapshot;

            lock (syncRoot)
            {
                snapshot = new CacheEntry<TKey, TValue>[priorities.Count];
                priorities.CopyTo(snapshot, 0);
            }

            return ((IEnumerable<CacheEntry<TKey, TValue>>)snapshot).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void UpdatePriority(LinkedListNode<CacheEntry<TKey, TValue>> oldNode)
        {
            // Get the entry for that node
            priorities.Remove(oldNode);
            priorities.AddLast(oldNode);
        }

        private void UpdateCapacity(int newCapacity)
        {
            while (values.Count > newCapacity)
            {
                LinkedListNode<CacheEntry<TKey, TValue>> oldNode = priorities.First!;

                values.Remove(oldNode.Value.Key);
                priorities.Remove(oldNode);
            }
        }
    }
}