using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace PlebBot.Data.Models
{
    public class User
    {
        public int Id { get; set; }

        public string DiscordId { get; set; }

        public string LastFm { get; set; }

        public string ColourRole { get; set; }
    }
}