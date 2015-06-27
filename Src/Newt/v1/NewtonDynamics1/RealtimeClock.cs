using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Newt.v1.NewtonDynamics1
{
    public static class RealtimeClock
    {
        private static DateTime _lastUpdate;
		/// <summary>
		/// This is in seconds
		/// </summary>
        private static double _timeStep;
        private static bool _firstCall = true;

        public static void Update()
        {
            DateTime time = DateTime.UtcNow;
			if (!_firstCall)
			{
				_timeStep = (time - _lastUpdate).TotalSeconds;
			}
			else
			{
				_firstCall = false;
			}

            _lastUpdate = time;
        }

        public static double TimeStep
        {
            get { return _timeStep; }
        }
    }
}
