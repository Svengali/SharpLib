using System;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Threading;

namespace lib
{
	public class Timer
	{
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(
            out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(
            out long lpFrequency);

        private long startTime;
				private long stopTime;
				private long freq;
				private long freq_millis;

        // Constructor

				public Timer()
				{
					startTime = 0;
					stopTime  = 0;

					if (QueryPerformanceFrequency(out freq) == false)
					{
						// high-performance counter not supported
						throw new Win32Exception();
					}

					freq_millis = freq / 1000;

				}

        // Start the timer

        public void Start()
        {
            // lets do the waiting threads there work

            //Thread.Sleep(0);

            QueryPerformanceCounter(out startTime);
        }

        // Stop the timer

        public void Stop()
        {
            QueryPerformanceCounter(out stopTime);
        }

		public double Seconds
		{
			get
			{
				long current;

				QueryPerformanceCounter( out current );

				return (double)( current - startTime ) / freq;
			}
		}

		public long Current
		{
			get
			{
				long current;

				QueryPerformanceCounter( out current );

				return ( current - startTime ) / freq_millis;
			}
		}

		public double Duration
		{
			get
			{
				return (double)( stopTime - startTime ) / (double)freq;
			}
		}
		
		public long DurationMS
		{
			get { return (stopTime - startTime) / freq_millis; }
		}
	}
}
