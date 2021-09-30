using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik.DataModels
{

    [BsonIgnoreExtraElements]
    public class UserCoordinates
    {
        public int X { get; set; }
        public int Z { get; set; }

        public DateTime Time { get; set; }

        public string Username { get; set; }

        public string World { get; set; }

        public UserCoordinates() { }

        public UserCoordinates(Dynmap.API.Player player)
        {
            this.X = player.X;
            this.Z = player.Y;
            this.Time = DateTime.UtcNow;
            this.Username = player.Account;
            this.World = player.World;
        }
    }
}
