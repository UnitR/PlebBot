using System;
using System.Threading.Tasks;
using Discord.Commands;
using PlebBot.Services.Chart;

namespace PlebBot.TypeReaders
{
    public class ChartSizeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            ChartSize size;
            switch (input)
            {
                case "3x3":
                case "3":
                    size = ChartSize.Small;
                    break;
                case "4x4":
                case "4":
                    size = ChartSize.Medium;
                    break;
                case "5x5":
                case "5":
                    size = ChartSize.Large;
                    break;
                default:
                    return Task.FromResult(TypeReaderResult.FromError(
                        CommandError.ParseFailed,
                        "Check the given chart size. Must be one of the following: 3x3 (small), 4x4 (medium), 5x5 (large)."));
            }
            return Task.FromResult(TypeReaderResult.FromSuccess(size));
        }
    }
}