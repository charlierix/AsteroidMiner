using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Newt.v2.NewtonDynamics
{
    /// <summary>
    /// This holds the current time as elapsed seconds from creation
    /// NOTE: This isn't super high precision, so there could be a tiny bit drift over long times, but will work fine for smaller intervals
    /// </summary>
    /// <remarks>
    /// There are certain classes that need to run on an interval, and they don't want to be called regularly, but instead
    /// use a threading timer, or some other means
    /// 
    /// If this class didn't exist, they would need to use DateTime, but that assumes the world is running in realtime
    /// </remarks>
    public class WorldClock
    {
        private const double RATIO = 1000000d;
        private const double INVERTRATIO = 1d / RATIO;

        private long _currentTime = 0;      // using long, because interlocked is faster than lock, and this property will get hit a lot
        public double CurrentTime
        {
            get
            {
                // It's stored in microseconds, so divide by one million to turn it back into a second
                return Interlocked.Read(ref _currentTime) * INVERTRATIO;
            }
        }

        public void AddSeconds(double seconds)
        {
            // It is stored in a long that holds millionths of a second
            Interlocked.Add(ref _currentTime, Convert.ToInt64(seconds * RATIO));
        }
    }
}
