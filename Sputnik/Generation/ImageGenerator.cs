using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik.Generation
{
    public static partial  class ImageGenerator
    {
        public const string BackgroundCacheLocation = "./BackgroundsCache";
        private static readonly PrivateFontCollection _fonts;

        static ImageGenerator()
        {
            if (!Directory.Exists(BackgroundCacheLocation))
                Directory.CreateDirectory(BackgroundCacheLocation);

            _fonts = new PrivateFontCollection();
            Utils.AddFontFromResource(_fonts, Properties.Resources.minecraft_font_otf);
        }

        public static Task<(BackgroundResult Result, Image Image)> GetBackgroundAsync(int x, int z, int radius, string world = "world")
            => GetOrCreateBackgroundAsync(new Point(x, z), radius, world);

        private static async Task<(BackgroundResult result, Image backgroundImage)> GetOrCreateBackgroundAsync(Point location, int blockRadius, string world = "world")
        {
            var fPath = BackgroundCacheLocation + $"/{world}_{location.X}_{location.Y}_{blockRadius}";

            if (File.Exists(fPath + ".json"))
            {
                var result = JsonConvert.DeserializeObject<BackgroundResult>(File.ReadAllText(fPath + ".json"));
                var image = Image.FromFile(fPath + ".png");

                return (result, image);
            }

            var bm = new Bitmap(1024, 1024);
            var grapics = Graphics.FromImage(bm);
            var drawResult = await Utils.DrawBackgroundAsync(grapics, 1024, 1024, location, blockRadius, world).ConfigureAwait(false);

            if (drawResult == null)
                return (null, null);

            File.WriteAllText(fPath + ".json", JsonConvert.SerializeObject(drawResult));
            bm.Save(fPath + ".png", System.Drawing.Imaging.ImageFormat.Png);

            return (drawResult, bm);
        }

        public static Image CreateColorImage(Color color)
        {
            var bm = new Bitmap(32, 32);
            var g = Graphics.FromImage(bm);

            g.Clear(color);

            return bm;
        }
    }
}
