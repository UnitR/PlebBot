namespace PlebBot.Data.Models
{
    public class User
    {
        public int Id { get; set; }

        public long DiscordId { get; set; }

        public string LastFm { get; set; }

        public string Rym { get; set; }

        public string City { get; set; }

        public byte[] Chart { get; set; }
    }
}