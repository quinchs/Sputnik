using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynmap
{
    public class DynmapClientConfig
    {
        public string Uri { get; set; }
        public int RefreshInterval { get; set; } = 3;
    }
}
