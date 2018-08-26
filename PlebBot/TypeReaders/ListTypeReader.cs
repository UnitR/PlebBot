using System;
using System.Threading.Tasks;
using Discord.Commands;
using PlebBot.Services.Chart;

namespace PlebBot.TypeReaders
{
    public class ListTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            ListType type;
            switch (input.ToLowerInvariant())
            {
                case "albums":
                case "album":
                    type = ListType.Albums;
                    break;
                case "tracks":
                case "track":
                    type = ListType.Tracks;
                    break;
                case "artists":
                case "artist":
                    type = ListType.Artists;
                    break;
                default:
                    return Task.FromResult(TypeReaderResult.FromError(
                        CommandError.ParseFailed, 
                        "Incorrect chart type. Must be one of the following: albums, tracks or artists"));
            }
            return Task.FromResult(TypeReaderResult.FromSuccess(type));
        }
    }
}