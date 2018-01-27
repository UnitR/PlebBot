using System;
using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace PlebBot.Data
{
    public class User
    {
        public int UserId { get; set; }

        public string DiscordId { get; set; }

        public string LastFm { get; set; }
    }
}
