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
    public class ActiveAlert
    {
        public ulong MessageId { get; set; }
        public CoordAlerts AlertArea { get; set; }
        public Dictionary<string, AlertUser> Positions { get; set; }
        public DateTime LastUpdateImage { get; set; }
    }

    public class AlertUser
    {
        public DateTime DateEntered { get; set; }
        public DateTime? DateLeft { get; set; }
        public string Username { get; set; }
        public int Color { get; set; }
        public List<UserCoordinates> Positions { get; set; }
    }
}
