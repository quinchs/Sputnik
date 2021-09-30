using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik.Generation.Models
{
    public struct TileInfo
    {
        public string Prefix { get; set; }
        public int ScaledX { get; set; }
        public int ScaledY { get; set; }
        public string Zoom { get; set; }
        public string ZoomPrefix { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string ImageFormat { get; set; }
    }
}
