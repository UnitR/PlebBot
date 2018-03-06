using Dapper.Contrib.Extensions;

namespace PlebBot.Data.Models
{
    [Table("Users")]
    public class User
    {
        public int Id { get; set; }

        public string DiscordId { get; set; }

        public string LastFm { get; set; }

        public string Rym { get; set; }
    }
}