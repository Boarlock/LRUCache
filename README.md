# LRU Cache - Public API

**Namespace:** `LRU` <br>
**Class:** `Cache<TKey, TValue>` <br>
**Entry Type:** `CacheEntry<TKey, TValue>` <br>

### Constructor
`Cache(int capacity)` - Initializes a new cache with the specified maximum capacity. Zero capacity is permitted. <br>

### Properties
`Capacity` - **int32** - Gets or sets the maximum number of entries. Reducing capacity evicts the least recently used entries. <br>
`Count` - **int32** - Gets the current number of entries in the cache. <br>

### Methods
`Add(TKey item, TValue value)` - **void** - Adds an entry or updates an existing entry. Updates its priority to most recently used. Evicts the least recently used entry when full. <br>
`Remove(TKey item)` - **void** - Removes an entry by key. Does nothing if the key doesn't exist. <br>
`TryGetValue(Tkey item, out TValue value)` - **bool** - Attempts to retrieve an entry by key. Updates its priority when successful. <br>
`Peek(TKey item)` - **TValue** - Retrieves an entry without updating its priority. Returns default if the key doesn't exist. <br>
`ContainsKey(TKey item)` - **bool** - Determines whether the cache contains the specified key. <br>
`ContainsValue(TValue value)` - **bool** - Determines weather the cache contains the specified value. <br>
`Clear()` - **void** - Removes all entries without changing capacity. <br>
`TrimExcess()` - **void** - Reduces capacity to the current number of entries. <br>
`EnsureCapacity(int capacity)` - **int32** - Increases capacity if necessary. Returns the resulting capacity. <br>

### Enumeration
`GetEnumerator()` - **IEnumerator<CacheEntry<TKey, TValue>>** - Returns an enumerator over a snapshot of the cache. <br>
`IEnumerable.GetEnumerator()` - **IEnumerator** - Non-generic enumeration implementation. <br>
**Enumeration order is Least Recently Used -> Most Recently Used** <br>

### CacheEntry
Key - The entry's key, reference and value types are accepted. <br>
Value - The entry's store value, null is perfectly valid. <br>

**Example Usage:** <br>
```
using LRU;

Cache<int, string> cache = new(3);

cache.Add(1, "Dog");
cache.Add(2, "Cat");
cache.Add(3, "Fish");

// Access updates priority
if (cache.TryGetValue(1, out string? value))
  Console.WriteLine(value);

// Enumerate from least recently used to most recently used
foreach (CacheEntry<int, string> entry in cache)
{
  Console.WriteLine($"Entry: {entry.Key} | Value: {entry.Value}");
}

// Increase capacity
cache.EnsureCapacity(10);

// Remove an entry
cache.Remove(2);

// Reduce capacity to current count
cache.TrimExcess();
```

### Some Extra Information <br>
**Enumeration Returns a Snapshot** -- Changes made after obtaining the enumerator aren't reflected in that enumeration. <br>
**Thread Safety** -- Applies to cache operations, not stored objects. Mutable reference-type values aren't automatically thread safe. <br>
**TrimExcess()** -- Unlike the one used on the generic `Dictionary<TKey, TValue>`, no memory is freed in this operation. The cache simply looks at its current Count and reduces capacity to match it. <br>
**EnsureCapacity()** -- This is the publically facing API for increasing cache size, you cannot reduce through this method. To reduce you need to use the .Capacity property and set it manually. <br>
**Peek()** -- This returns `default` on a miss, which is the same as a value that's simply stored as a default. If you need to know if the value exists I recommend using `TryGetValue()`. <br>

### Time Complexity
**Constructor** - O(1) - Capacity is a number nothing is pre-allocated. <br>
**Count** - O(1) <br>
**Capacity(get)** - O(1) <br>
**Capacity(set)** - O(1) growing, O(k) shrinking - k = items evicted `(Count - newCapacity)`. Each eviction is O(1), so shrinking to 0 is O(n). <br>
**Add(existing key)** - O(1) - Dictionary lookup, replace the node's value, move the node. <br>
**Add(new key, has room)** - O(1) amortized - One new node plus a dictionary insert. The dictionary occasionally resizes and rehashes, so a single insert can be O(n), but that averages out. <br>
**Add(new key, full)** - O(1) - Evict the first node, reuse it, move it. A full cache holds steady at its size, so it shouldn't trigger resizes. <br>
**TryGetValue** - O(1) - Lookup, then a node remove and readd. <br>
**Peek** - O(1) - Lookup only. <br> 
**ContainsKey** - O(1) <br>
**Remove** - O(1) - Lookup, then Remove(node) which is the pointer overload. <br>
**Clear** - O(n) - LinkedList.Clear walks the nodes to detach them, and Dictionary.Clear clears its internal arrays. <br>
**ContainsValue** - O(n) - Scans the list, since values aren't indexed. <br>
**TrimExcess** - O(1) <br>
**EnsureCapacity** - O(1) <br>
**GetEnumerator** - O(n) to retrieve, O(1) per step - The snapshot copies the whole list under the lock before returning. <br>



