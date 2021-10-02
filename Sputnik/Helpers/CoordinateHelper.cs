using MongoDB.Driver;
using Sputnik.DataModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik.Helpers
{
    public class CoordinateHelper
    {
        public static async Task<IOrderedEnumerable<UserCoordinates>> GetPlayerCoordinatesAsync(string username, DateTime from, DateTime to)
        {
            var data = await MongoService.UserCoordinatesCollection.FindAsync(x => x.Username == username && x.Time >= from && x.Time <= to);

            return data.ToList().OrderByDescending(x => x.Time.Ticks);
        }

        public static async Task<IReadOnlyDictionary<string, IOrderedEnumerable<UserCoordinates>>> GetIntersectingPlayersAsync(string baseUser, Point p, int radius, DateTime from, DateTime to, string world)
        {
            // we will be working with a square radius instead of a circle one, the math is a bit different
            var rect = new Rectangle(p.X - radius, p.Y - radius, radius* 2, radius * 2);

            var dict = new Dictionary<string, IOrderedEnumerable<UserCoordinates>>();

            var intersecting = (await MongoService.UserCoordinatesCollection.FindAsync(x =>
                x.Username != baseUser && 
                x.World == world &&
                x.Time >= from && x.Time <= to
            ).ConfigureAwait(false)).ToList().Where(x => MathUtils.IsInsideOf(new Point(x.X, x.Z), rect));

            var result = intersecting.GroupBy(x => x.Username);

            foreach(var item in result)
            {
                dict.Add(item.Key, item.OrderByDescending(x => x.Time.Ticks));
            }

            return dict;
        }

        public static UserCoordinates GetFarthestDistance(IEnumerable<UserCoordinates> positions, Point point)
        {
            return positions.Aggregate((x, y) => MathUtils.CalculateDistance(point.X, point.Y, x.X, x.Z) > MathUtils.CalculateDistance(point.X, point.Y, y.X, y.Z) ? x : y);
        }
    }
}
