using System;
using System.Collections.Concurrent;
using Discord;

namespace PlebBot.Helpers
{
    // Based on https://github.com/RogueException/Discord.Net/blob/dev/src/Discord.Net.WebSocket/Entities/Messages/MessageCache.cs
    // Also based on https://github.com/moiph/ub3r-b0t/blob/master/src/MessageCache.cs
    internal class MessageCache
    {
        private const int PROCESSOR_COUNT_REFRESH_INTERVAL_MS = 30000;
        private static volatile int s_processorCount;
        private static volatile int s_lastProcessorCountRefreshTicks;

        private readonly ConcurrentDictionary<ulong, IMessage> _messages;
        private readonly ConcurrentQueue<ulong> _orderedMessages;
        private readonly int _size;

        public MessageCache()
        {
            _size = 1000;
            _messages = new ConcurrentDictionary<ulong, IMessage>(DefaultConcurrencyLevel, (int)(_size * 1.05));
            _orderedMessages = new ConcurrentQueue<ulong>();
        }

        //Based on https://github.com/dotnet/corefx/blob/d0dc5fc099946adc1035b34a8b1f6042eddb0c75/src/System.Threading.Tasks.Parallel/src/SystemThreading/PlatformHelper.cs
        private int DefaultConcurrencyLevel
        {
            get
            {
                int now = Environment.TickCount;
                if (s_processorCount == 0 || (now - s_lastProcessorCountRefreshTicks) >= PROCESSOR_COUNT_REFRESH_INTERVAL_MS)
                {
                    s_processorCount = Environment.ProcessorCount;
                    s_lastProcessorCountRefreshTicks = now;
                }

                return s_processorCount;
            }
        }

        public void Add(ulong id, IMessage message)
        {
            if (message == null || !_messages.TryAdd(id, message)) return;

            _orderedMessages.Enqueue(id);
            while (_orderedMessages.Count > _size && _orderedMessages.TryDequeue(out ulong msgId))
            {
                _messages.TryRemove(msgId, out var msg);
            }
        }

        public IMessage Get(ulong id)
        {
            return _messages.ContainsKey(id) ? _messages[id] : null;
        }

        public IMessage Remove(ulong id)
        {
            _messages.TryRemove(id, out var msg);
            return msg;
        }
    }
}
