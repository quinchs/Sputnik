using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik.DataModels
{
    [BsonIgnoreExtraElements]
    public class CustomEmote
    {
        public ulong EmoteId { get; set; }
        public int ARGB { get; set; }
    }
}
