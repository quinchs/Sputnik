using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using Dynmap;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik.DataModels
{
    [BsonIgnoreExtraElements]
    public class ChatMessage
    {
        public string MinecraftAccount { get; set; }
        public string PlayerName { get; set; }
        public string Message { get; set; }
        public string Source { get; set; }
        [BsonRepresentation(BsonType.DateTime)]
        public DateTime Time { get; set; }

        public ChatMessage() { }

        public ChatMessage(Dynmap.API.Update update)
        {
            if (update.Type != Dynmap.API.TypeEnum.Chat)
                return;

            this.MinecraftAccount = update.Account;
            this.PlayerName = update.PlayerName;
            this.Message = update.Message;
            this.Source = update.Source;
            this.Time = update.Timestamp.JavaTimeStampToDateTime();
        }
    }
}
