using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynmap
{
    public static class DateTimeExtensions
    {
        public static DateTime JavaTimeStampToDateTime(this ulong javaTimeStamp)
        {
            // Java timestamp is milliseconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddMilliseconds(javaTimeStamp).ToLocalTime();
            return dateTime;
        }

        public static ulong ToJavaDateTime(this DateTimeOffset offset)
        {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return (ulong)(offset - dateTime).TotalMilliseconds;
        }
    }
}
