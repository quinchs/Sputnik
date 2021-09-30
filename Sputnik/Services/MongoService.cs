using MongoDB.Driver;
using Sputnik.DataModels;
using Sputnik.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik
{
    public class MongoService
    {
        public static MongoClient Client = new MongoClient(ConfigService.Config.MongoCS);

        public static IMongoDatabase Database
            => Client.GetDatabase("Sputnik");

        public static IMongoCollection<UserCoordinates> UserCoordinatesCollection
            => Database.GetCollection<UserCoordinates>("user-coordinates");

        public static IMongoCollection<CoordAlerts> AlertCollection
            => Database.GetCollection<CoordAlerts>("alert-coords");

        public static IMongoCollection<Whitelist> Whitelist
            => Database.GetCollection<Whitelist>("alerts-whitelist");

        public static IMongoCollection<ActiveAlert> ActiveAlerts
            => Database.GetCollection<ActiveAlert>("active-alerts");
    }
}
