using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik.DataModels
{
    [BsonIgnoreExtraElements]
    public class Whitelist
    {
        public List<string> Usernames { get; set; }
    }
}
