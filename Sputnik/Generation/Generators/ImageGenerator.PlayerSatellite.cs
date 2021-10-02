using Sputnik.DataModels;
using Sputnik.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik.Generation
{
    public partial class ImageGenerator
    {
        public static async Task<PlayerSatelliteResult> GetPlayerSatelliteImageAsync(string username, string world = null)
        {
            var coordinateMapResult = await GetCoordinateMapAsync(username, world).ConfigureAwait(false);

            world = coordinateMapResult.World;

            if (coordinateMapResult.Result.Count == 0)
                return new PlayerSatelliteResult(null, 0, new Point(0, 0), null);

            var center = coordinateMapResult.CurrentPos;

            var background = await GetOrCreateBackgroundAsync(center, coordinateMapResult.Radius, world).ConfigureAwait(false);

            var graphics = Graphics.FromImage(background.backgroundImage);

            var rect = new Rectangle(center.X - coordinateMapResult.Radius, center.Y - coordinateMapResult.Radius, coordinateMapResult.Radius * 2, coordinateMapResult.Radius * 2);

            var colorMap = new Dictionary<string, Color>();

            foreach (var player in coordinateMapResult.Result)
            {
                var clr = Utils.GetMapColor(player.Key);
                colorMap.Add(player.Key, clr);
                await DrawPlayerAsync(graphics, (MathUtils.IsInsideOf(player.Value.First(), rect), player.Value.ToList()), clr, background.result).ConfigureAwait(false);
            }

            return new PlayerSatelliteResult(background.backgroundImage, coordinateMapResult.Radius, center, colorMap);
        }

        private static async Task<CoordinateMapResult> GetCoordinateMapAsync(string baseUser, string world = null)
        {
            DateTime to = DateTime.UtcNow, from = DateTime.UtcNow - TimeSpan.FromMinutes(2);

            var pos = await CoordinateHelper.GetPlayerCoordinatesAsync(baseUser, from, to).ConfigureAwait(false);

            world ??= pos.First()?.World;

            pos = pos.Where(x => x.World == world).OrderByDescending(x => x.Time.Ticks);

            var currentPos = pos.FirstOrDefault();

            if (currentPos == null)
                return new CoordinateMapResult(new Dictionary<string, IOrderedEnumerable<UserCoordinates>>(), 250, new Point(0, 0), world);

            var farthestPos = CoordinateHelper.GetFarthestDistance(pos, currentPos);
            var radius = MathUtils.CalculateDistance(farthestPos.X, farthestPos.Z, currentPos.X, currentPos.Z);

            if (radius < 250)
                radius = 250;

            // find any related people within the same area
            var otherPos = await CoordinateHelper.GetIntersectingPlayersAsync(baseUser, currentPos, radius, from, to, world).ConfigureAwait(false);

            var dict = new Dictionary<string, IOrderedEnumerable<UserCoordinates>>(otherPos.ToDictionary(x => x.Key, x => x.Value));
            dict.Add(currentPos.Username, pos);
            return new CoordinateMapResult(dict, radius, currentPos, world);
        }

        private struct CoordinateMapResult
        {
            public IReadOnlyDictionary<string, IOrderedEnumerable<UserCoordinates>> Result { get; }
            public int Radius { get; }
            public Point CurrentPos { get; }
            public string World { get; }

            public CoordinateMapResult(Dictionary<string, IOrderedEnumerable<UserCoordinates>> dict, int r, Point c, string w)
            {
                this.Result = dict;
                this.Radius = r;
                this.CurrentPos = c;
                this.World = w;
            }
        }
    }

    public struct PlayerSatelliteResult : IDisposable
    {
        public Image Image { get; }
        public int BlockRadius { get; }
        public Point Center { get; }
        public IReadOnlyDictionary<string, Color> Colors { get; private set; }

        public PlayerSatelliteResult(Image m, int b, Point c, Dictionary<string, Color> clr)
        {
            this.Image = m;
            this.BlockRadius = b;
            this.Center = c;
            this.Colors = clr;
        }

        public void Dispose()
        {
            Image.Dispose();
            Colors = null;
        }
    }
}
