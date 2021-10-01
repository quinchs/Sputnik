using Sputnik.Generation.Models;
using Sputnik.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Sputnik.Generation
{
    public class BackgroundResult
    {
        public float BlocksPerPixel { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int CenterX { get; set; }
        public int CenterY { get; set; }
    }

    public class Utils
    {
        public const int MaxZoom = 6;

        public static Point ToRelativePoint(Point minecraftLoc, BackgroundResult result)
        {
            return new Point(
                (int)((result.Width / 2) + ((minecraftLoc.X - result.CenterX) * result.BlocksPerPixel)),
                (int)((result.Height / 2) + ((minecraftLoc.Y - result.CenterY) * result.BlocksPerPixel))
                );
        }

        public static async Task<BackgroundResult> DrawBackgroundAsync(Graphics g, int width, int height, Point minecraftCoordsCenter, int blockRadius)
        {
            if (width != height)
                return null;

            var zoomFactor = GetZoom(blockRadius);
            var blockToZoom = 16 * (2 << (6 - zoomFactor));

            minecraftCoordsCenter.Offset(0, 32);

            // 0_0 will have the coord 0x, 0y in the bottom left. 1_0 will be 32x, 0y.

            // first lets calculate the tile coords for the location

            var chunkX = (int)Math.Floor(minecraftCoordsCenter.X / (double)blockToZoom); // the x coord for the tile containing our location
            var chunkY = (int)Math.Floor(minecraftCoordsCenter.Y / (double)blockToZoom); // the y coord for the tile containing our location

            // lets calculate our radius and how many tiles we need to load.

            var tileRadius = (int)Math.Ceiling(blockRadius / (double)blockToZoom) + 1;

            // now we need to map the tiles we need to load into a list

            var xTileCount = Math.Abs((chunkX - tileRadius) - (chunkX + tileRadius));
            var yTileCount = Math.Abs((chunkY - tileRadius) - (chunkY + tileRadius));

            Point[,] Tiles = new Point[xTileCount, yTileCount];

            var baseX = chunkX - tileRadius;
            var baseY = chunkY - tileRadius;

            for (int x = baseX; x != chunkX + tileRadius; x++)
            {
                for(int y = baseY; y != chunkY + tileRadius; y++)
                {
                    Tiles[x - baseX, y - baseY] = new Point(x, y);
                }
            }

            // lets calculate the size of each tile were drawing on our graphics
            float tileSize = width / ((tileRadius - 1) * 2);

            // now we need to calculate the offset so the minecraft coords are in the center of the image.
            var xOffset = -(int)Map(Math.Abs(minecraftCoordsCenter.X % blockToZoom), 0, blockToZoom, minecraftCoordsCenter.X >= 0 ? 0 : tileSize, minecraftCoordsCenter.X >= 0 ? tileSize : 0);
            var yOffset = -(int)Map(Math.Abs(minecraftCoordsCenter.Y % blockToZoom), 0, blockToZoom, minecraftCoordsCenter.Y >= 0 ? 0 : tileSize, minecraftCoordsCenter.Y >= 0 ? tileSize : 0);

            List<Task<ImageDetails>> work = new();
            for (int y = 0; y != yTileCount; y++)
            {
                for (int x = 0; x != xTileCount; x++)
                {
                    var tile = Tiles[x, y];
                    var url = GetTileUrl("world", tile, zoomFactor);

                    work.Add(GetImageDetailsAsync(url, x, y, (int)tileSize, xOffset, yOffset));
                }
            }

            await Task.WhenAll(work);

            foreach(var task in work)
            {
                var d = await task;
                g.DrawImage(d.Image, d.X, d.Y, d.Width, d.Height);
            }

            var centerX = width / 2;
            var centerY = height / 2;

            g.DrawLine(new Pen(new SolidBrush(Color.Red), 2), centerX - 10, centerY, centerX + 10, centerY);
            g.DrawLine(new Pen(new SolidBrush(Color.Red), 2), centerX , centerY - 10, centerX, centerY + 10);

            return new BackgroundResult()
            {
                BlocksPerPixel = (width / (blockRadius * 2f)),
                Width = width,
                Height = height,
                CenterX = minecraftCoordsCenter.X,
                CenterY = minecraftCoordsCenter.Y - 32,
            };
        }

        private static async Task<ImageDetails> GetImageDetailsAsync(string url, int x, int y, int tileSize, int xOffset, int yOffset)
        {
            var image = await MapDownloaderService.GetTileAsync(url);

            return new ImageDetails(image, (((x * tileSize) + xOffset) - tileSize), ((y * tileSize) + yOffset) - tileSize, tileSize, tileSize);
        }

       

        private struct ImageDetails
        {
            public Image Image { get; set; }
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }

            public ImageDetails(Image i, int x, int y, int w, int h)
            {
                this.Image = i;
                this.X = x;
                this.Y = y;
                this.Width = w;
                this.Height = h;
            }
        }

        private static int GetZoom(int blockRadius)
        {
            if (blockRadius >= 1100)
                return 1;

            if (blockRadius >= 850)
                return 2;

            if (blockRadius >= 280)
                return 3;

            if (blockRadius >= 170)
                return 4;
            

            if (blockRadius >= 96)
                return 5;

            return 6;
        }

        private static double Map(double value, double s1, double e1, double s2, double e2)
        {
            var _1 = Math.Abs(s1 - e1);
            var _2 = Math.Abs(s2 - e2);

            var r = (value / _1);

            if (r == 0)
                return s2;

            return s2 > e2 ? s2 - _2 * r : s2 + _2 * r;
        }

        public static void AddFontFromResource(PrivateFontCollection privateFontCollection, byte[] font)
        {
            var fontData = Marshal.AllocCoTaskMem(font.Length);
            Marshal.Copy(font, 0, fontData, font.Length);
            privateFontCollection.AddMemoryFont(fontData, font.Length);
            // Marshal.FreeCoTaskMem(fontData);  Nasty bug alert, read the comment
        }

        public static async Task DrawPlayerHead(Graphics g, Point loc, int width, int height, string username)
        {
            var headImage = await MapDownloaderService.GetPlayerheadAsync(username);

            loc.Offset(-(width / 2), -(height / 2));

            g.DrawImage(headImage, loc.X, loc.Y, width, height);
        }

        public static string GetPlayerheadUrl(string username)
            => $"{ConfigService.Config.DynmapUri}tiles/faces/32x32/{username}.png";

        public static string ZoomPrefix(int amount)
        {
            if (amount == 0)
            {
                return "";
            }

            return "zzzzzzzzzzzzzzzzzzzzzz".Substring(0, amount) + (amount == 0 ? "" : "_");
        }

        public static int ZoomToWidth(string z)
        {
            return z.Count(x => x == 'z') * 64;
        }

        public static string GetTileUrl(string world, Point p, int zoom)
        {
            var tileName = GetTileName(p, zoom);
            return ConfigService.Config.DynmapUri + "tiles/" + $"{world}/{tileName}";
        }

        public static string GetTileName(Point p, int zoom)
        {
            var info = GetTileInfo(p, zoom);

            info.Y = -info.Y;

            return $"{info.Prefix}/{info.ScaledX}_{info.ScaledY}/{info.Zoom}{info.X}_{info.Y}.{info.ImageFormat}";
        }

        public static TileInfo GetTileInfo(Point p, int zoom)
        {
            var izoom = MaxZoom - zoom;
            var zoomOutLevel = Math.Max(0, izoom);
            var scale = 1 << zoomOutLevel;
            var x = scale * p.X;
            var y = scale * p.Y;

            return new TileInfo()
            {
                Prefix = "flat",
                ScaledX = x >> 5,
                ScaledY = y >> 5,
                Zoom = ZoomPrefix(zoomOutLevel),
                X = x,
                Y = y,
                ImageFormat = "jpg",
                ZoomPrefix = zoomOutLevel == 0 ? "" : (ZoomPrefix(zoomOutLevel) + "_")
            };
        }
    }
}
