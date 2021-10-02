using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Drawing;
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

        [BsonRepresentation(BsonType.DateTime)]
        public DateTime Time { get; set; }

        public string Username { get; set; }

        public string World { get; set; }

        public UserCoordinates() { }

        public UserCoordinates(Dynmap.API.Player player)
        {
            this.X = (int)player.X;
            this.Z = (int)player.Z;
            this.Time = DateTime.UtcNow;
            this.Username = player.Account;
            this.World = player.World;
        }

        public static implicit operator Point(UserCoordinates c) => new Point(c.X, c.Z);
    }
}
