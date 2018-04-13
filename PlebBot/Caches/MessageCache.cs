using System;
using System.Collections.Concurrent;
using Discord;

namespace PlebBot.Caches
{
    // Based on https://github.com/RogueException/Discord.Net/blob/dev/src/Discord.Net.WebSocket/Entities/Messages/MessageCache.cs
    internal class MessageCache
    {
        private const int ProcessorCountRefreshIntervalMs = 30000;
        private static volatile int _sProcessorCount;
        private static volatile int _sLastProcessorCountRefreshTicks;

        private readonly ConcurrentDictionary<ulong, IMessage> messages;
        private readonly ConcurrentQueue<ulong> orderedMessages;
        private readonly int size;

        public MessageCache()
        {
            size = 1000;
            messages = new ConcurrentDictionary<ulong, IMessage>(DefaultConcurrencyLevel, (int)(size * 1.05));
            orderedMessages = new ConcurrentQueue<ulong>();
        }

        //Based on https://github.com/dotnet/corefx/blob/d0dc5fc099946adc1035b34a8b1f6042eddb0c75/src/System.Threading.Tasks.Parallel/src/SystemThreading/PlatformHelper.cs
        private int DefaultConcurrencyLevel
        {
            get
            {
                int now = Environment.TickCount;
                if (_sProcessorCount == 0 || (now - _sLastProcessorCountRefreshTicks) >= ProcessorCountRefreshIntervalMs)
                {
                    _sProcessorCount = Environment.ProcessorCount;
                    _sLastProcessorCountRefreshTicks = now;
                }

                return _sProcessorCount;
            }
        }

        public void Add(ulong id, IMessage message)
        {
            if (message == null || !messages.TryAdd(id, message)) return;

            orderedMessages.Enqueue(id);
            while (orderedMessages.Count > size && orderedMessages.TryDequeue(out ulong msgId))
            {
                messages.TryRemove(msgId, out var _);
            }
        }

        public IMessage Get(ulong id)
        {
            return messages.ContainsKey(id) ? messages[id] : null;
        }

        public IMessage Remove(ulong id)
        {
            messages.TryRemove(id, out var msg);
            return msg;
        }
    }
}
