using System.Collections.Generic;

namespace PlebBot.Data.Models
{
    public class Server
    {
        public Server()
        {
            this.Roles = new List<Role>();
        }

        public int Id { get; set; }

        public long DiscordId { get; set; }

        public string Prefix { get; set; }

        public ICollection<Role> Roles { get; set; }
    }
}