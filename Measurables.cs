using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlloouBot
{
    public static class Measurables
    {
        public static ushort viewerCount = 0; // 16-bit int
        public static List<string> currentViewers = new List<string>();
        public static List<string> allViewers = new List<string>();
        public static List<string> newFollowers = new List<string>();
    }
}
