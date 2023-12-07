using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace _3dTesting.Helpers
{
    public static class PerformanceHelpers
    {
        readonly static Stopwatch timer = new Stopwatch();
        public static bool StartTime()
        {
            try
            {
                timer.Start();
                return true;
            }
            catch { return false; }            
        }
        public static long StopTime()
        {
            timer.Stop();
            return timer.ElapsedMilliseconds;
        }
    }
}
