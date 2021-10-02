using Sputnik.DataModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik.Generation
{
    public partial class ImageGenerator
    {
        private static MD5 md5 = MD5.Create();

        public static async Task<AreaMapResult> CreateAlertAreaMapAsync(IEnumerable<CoordAlerts> alerts, int blockRadius)
        {
            var background = await GetOrCreateBackgroundAsync(new Point(0,0), blockRadius).ConfigureAwait(false);

            var graphics = Graphics.FromImage(background.backgroundImage);

            Dictionary<string, Color> colorMap = new Dictionary<string, Color>();

            foreach (var item in alerts.Where(x => x.World == "world"))
            {
                var color = Utils.GetMapColor(item.Name);
                colorMap.Add(item.Name, color);
                Utils.DrawCircle(graphics, color, item, background.result, item.Radius);
            }

            return new AreaMapResult(background.backgroundImage, colorMap);

        }
    }

    public struct AreaMapResult : IDisposable
    {
        public IReadOnlyDictionary<string, Color> Colors { get; }

        public Image Image { get; }

        public AreaMapResult(Image i, Dictionary<string, Color> c)
        {
            Image = i;
            Colors = c;
        }

        public void Dispose()
        {
            Image.Dispose();
        }
    }
}
