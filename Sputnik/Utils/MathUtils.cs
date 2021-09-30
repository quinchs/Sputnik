using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik
{
    public static class MathUtils
    {
        public static int CalculateDistance(int x1, int z1, int x2, int z2)
        {
            return (int)Math.Sqrt(Math.Pow(Math.Abs(x1 - x2), 2) + Math.Pow(Math.Abs(z1 - z2),2));
        }
    }
}
