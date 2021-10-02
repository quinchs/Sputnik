using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik.DataModels
{
    public class Whitelist
    {
        public ObjectId Id { get; set; }
        
        public List<string> Usernames { get; set; }
    }
}
