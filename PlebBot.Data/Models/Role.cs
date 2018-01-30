using System;
using System.Collections.Generic;
using System.Text;

namespace PlebBot.Data.Models
{
    public class Role
    {
        public int Id { get; set; }
        
        public string DiscordId { get; set; }

        public string Name { get;set; }

        public bool IsColour { get; set; }

        public Server Server { get; set; }
    }
}
