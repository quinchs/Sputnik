using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik.DataModels
{
    [BsonIgnoreExtraElements]
    public class CoordAlerts
    {
        public ulong Owner { get; set; }
        public int X { get; set; }
        public int Z { get; set; }
        public int Radius { get; set; }
        public string Name { get; set; }
        public string World { get; set; }
    }
}
