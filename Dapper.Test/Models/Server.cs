using System.Collections.Generic;

namespace Dapper.Test.Models
{
    public class Server
    {
        public Server()
        {
            this.Roles = new List<Role>();
        }

        public int Id { get; set; }

        public string DiscordId { get; set; }

        public string Prefix { get; set; }

        public ICollection<Role> Roles { get; set; }
    }
}