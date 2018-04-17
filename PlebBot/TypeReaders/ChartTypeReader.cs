using System;
using System.Threading.Tasks;
using Discord.Commands;
using PlebBot.Services.Chart;

namespace PlebBot.TypeReaders
{
    public class ChartTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> Read(ICommandContext context, string input, IServiceProvider services)
        {
            ChartType type;
            switch (input.ToLowerInvariant())
            {
                case "albums":
                case "album":
                    type = ChartType.Albums;
                    break;
                case "tracks":
                case "track":
                    type = ChartType.Tracks;
                    break;
                case "artists":
                case "artist":
                    type = ChartType.Artists;
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