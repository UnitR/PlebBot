using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace PlebBot.Caches.CommandCache
{
    public class CommandCacheService : ICommandCache<ulong, ConcurrentBag<ulong>>, IDisposable
    {
        private const int Unlimited = -1;

        private readonly ConcurrentDictionary<ulong, ConcurrentBag<ulong>> cache
            = new ConcurrentDictionary<ulong, ConcurrentBag<ulong>>();
        private readonly int max;
        private Timer autoClear;
        private int count;

        public CommandCacheService(DiscordSocketClient client, int capacity = 200)
        {
            // Make sure the max capacity is within an acceptable range, use it if it is.
            if (capacity < 1 && capacity != Unlimited)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity can not be lower than 1 unless capacity is CommandCacheService.UNLIMITED.");
            }
            max = capacity;

            // Create a timer that will clear out cached messages older than 2 hours every 30 minutes.
            autoClear = new Timer(OnTimerFired, null, 1800000, 1800000);

            client.MessageDeleted += OnMessageDeleted;
        }

        public IEnumerable<ulong> Keys => cache.Keys;

        public IEnumerable<ConcurrentBag<ulong>> Values => cache.Values;

        public int Count => count;

        public void Add(ulong key, ConcurrentBag<ulong> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values), "The supplied collection can not be null.");
            }

            if (max != Unlimited && count >= max)
            {
                var removeCount = count - max + 1;
                // The left 42 bits represent the timestamp.
                var orderedKeys = cache.Keys.OrderBy(k => k >> 22).ToList();
                // Remove items until we're under the maximum.
                var successfulRemovals = 0;
                foreach (var orderedKey in orderedKeys)
                {
                    if (successfulRemovals >= removeCount) break;

                    var success = Remove(orderedKey);
                    if (success) successfulRemovals++;
                }

                // Reset _count to cache.Count.
                UpdateCount();
            }

            // TryAdd will return false if the key already exists, in which case we don't want to increment the count.
            if (cache.TryAdd(key, values))
            {
                Interlocked.Increment(ref count);
            }
            else
            {
                cache[key].AddMany(values);
            }
        }

        public void Add(KeyValuePair<ulong, ConcurrentBag<ulong>> pair) => Add(pair.Key, pair.Value);

        public void Add(ulong key, ulong value)
        {
            if (!TryGetValue(key, out ConcurrentBag<ulong> bag))
            {
                Add(key, new ConcurrentBag<ulong>() { value });
            }
            else
            {
                bag.Add(value);
            }
        }

        public void Add(ulong key, params ulong[] values) => Add(key, new ConcurrentBag<ulong>(values));

        public void Add(IUserMessage command, IUserMessage response) => Add(command.Id, response.Id);

        public void Clear()
        {
            cache.Clear();
            Interlocked.Exchange(ref count, 0);
        }

        public bool ContainsKey(ulong key) => cache.ContainsKey(key);

        public IEnumerator<KeyValuePair<ulong, ConcurrentBag<ulong>>> GetEnumerator() => cache.GetEnumerator();

        public bool Remove(ulong key)
        {
            var success = cache.TryRemove(key, out ConcurrentBag<ulong> _);
            if (success) Interlocked.Decrement(ref count);
            return success;
        }

        public bool TryGetValue(ulong key, out ConcurrentBag<ulong> value) => cache.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing || autoClear == null) return;

            autoClear.Dispose();
            autoClear = null;
        }

        private void OnTimerFired(object state)
        {
            // Get all messages where the timestamp is older than 2 hours, then convert it to a list. The result of where merely contains references to the original
            // collection, so iterating and removing will throw an exception. Converting it to a list first avoids this.
            var purge = cache.Where(p =>
            {
                // The timestamp of a message can be calculated by getting the leftmost 42 bits of the ID, then
                // adding January 1, 2015 as a Unix timestamp.
                var timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)((p.Key >> 22) + 1420070400000UL));
                var difference = DateTimeOffset.UtcNow - timestamp;

                return difference.TotalHours >= 2.0;
            }).ToList();
            var unused = purge.Where(p => Remove(p.Key));
            UpdateCount();
        }

        private async Task OnMessageDeleted(Cacheable<IMessage, ulong> cacheable, ISocketMessageChannel channel)
        {
            if (TryGetValue(cacheable.Id, out ConcurrentBag<ulong> messages))
            {
                foreach (var messageId in messages)
                {
                    var message = await channel.GetMessageAsync(messageId);
                    if (message != null)
                    {
                        await message.DeleteAsync();
                    }
                }
                Remove(cacheable.Id);
            }
        }

        private void UpdateCount() => Interlocked.Exchange(ref count, cache.Count);
    }
}
