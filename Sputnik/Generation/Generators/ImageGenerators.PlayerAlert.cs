using Sputnik.DataModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik.Generation
{
    public class AlertImageResult
    {
        public Dictionary<string, Color> PlayerColors { get; set; }
        public BackgroundResult BackgroundResult { get; set; }
        public Image Image { get; set; }
    }

    public static partial class ImageGenerator
    {
        public static async Task<AlertImageResult> CreateAlertAsync(Point location, int blockRadius, List<(bool drawHead, List<UserCoordinates> positions)> offenderPositions, Dictionary<string, Color> playerColors = null)
        {
            var background = await GetOrCreateBackgroundAsync(location, blockRadius);

            if (background.result == null)
                return null;

            var grapics = Graphics.FromImage(background.backgroundImage);

            grapics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            Dictionary<string, Color> colors = playerColors ?? new Dictionary<string, Color>();

            foreach(var coords in offenderPositions)
            {
                Color color;
                var un = coords.positions.First().Username;


                if (colors.TryGetValue(un, out var c))
                    color = c;
                else
                {
                    var n = (int)(0xF7000000 + (new Random().Next(0xFFFFFF) & 0x7F7F7F));
                    color = Color.FromArgb(n);
                    colors.Add(un, color);
                }

                await DrawPlayerAsync(grapics, coords, color, background.result);
            }

            return new AlertImageResult()
            {
                BackgroundResult = background.result,
                Image = background.backgroundImage,
                PlayerColors = colors
            };
        }

        private static async Task DrawPlayerAsync(Graphics grapics, (bool drawHead, List<UserCoordinates> positions) offender, Color color, BackgroundResult background)
        {
            var pos = offender.positions.OrderByDescending(x => x.Time.Ticks).ToArray();
            var mostRecentPos = pos.FirstOrDefault();
            var playerName = mostRecentPos?.Username;

            var pen = new Pen(color, 5);

            for (int i = 0; i != pos.Length - 1; i++)
            {
                var currentPos = pos[i];
                var nextPos = pos[i + 1];

                var pointA = Utils.ToRelativePoint(new Point(currentPos.X, currentPos.Z), background);
                var pointB = Utils.ToRelativePoint(new Point(nextPos.X, nextPos.Z), background);

                grapics.DrawLine(pen, pointA, pointB);
            }

            var playerPoint = Utils.ToRelativePoint(new Point(mostRecentPos.X, mostRecentPos.Z), background);
            //playerPoint.Offset(0, -48);

            if(offender.drawHead)
            {
                await Utils.DrawPlayerHead(grapics, playerPoint, 64, 64, playerName);
                grapics.DrawString(playerName, new Font(_fonts.Families[0], 20, FontStyle.Regular), new SolidBrush(Color.White), playerPoint.X, playerPoint.Y + 48, new StringFormat()
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                });
            }

            
        }
    }
}
