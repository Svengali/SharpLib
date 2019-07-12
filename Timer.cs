using System;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Threading;

namespace lib
{

	public class MicroStopwatch : System.Diagnostics.Stopwatch
	{
		readonly double _microSecPerTick
						= 1000000D / System.Diagnostics.Stopwatch.Frequency;

		public MicroStopwatch()
		{
			if( !System.Diagnostics.Stopwatch.IsHighResolution )
			{
				throw new Exception( "On this system the high-resolution " +
														"performance counter is not available" );
			}
		}

		public long ElapsedMicroseconds
		{
			get
			{
				return (long)( ElapsedTicks * _microSecPerTick );
			}
		}
	}

	/// <summary>
	/// MicroTimer class
	/// </summary>
	public class MicroTimer
	{
		public delegate void MicroTimerElapsedEventHandler(
												 object sender,
												 MicroTimerEventArgs timerEventArgs );
		public event MicroTimerElapsedEventHandler MicroTimerElapsed;

		System.Threading.Thread _threadTimer = null;
		long _ignoreEventIfLateBy = long.MaxValue;
		long _timerIntervalInMicroSec = 0;
		bool _stopTimer = true;

		public MicroTimer()
		{
		}

		public MicroTimer( long timerIntervalInMicroseconds )
		{
			Interval = timerIntervalInMicroseconds;
		}

		public long Interval
		{
			get
			{
				return System.Threading.Interlocked.Read(
						ref _timerIntervalInMicroSec );
			}
			set
			{
				System.Threading.Interlocked.Exchange(
						ref _timerIntervalInMicroSec, value );
			}
		}

		public long IgnoreEventIfLateBy
		{
			get
			{
				return System.Threading.Interlocked.Read(
						ref _ignoreEventIfLateBy );
			}
			set
			{
				System.Threading.Interlocked.Exchange(
						ref _ignoreEventIfLateBy, value <= 0 ? long.MaxValue : value );
			}
		}

		public bool Enabled
		{
			set
			{
				if( value )
				{
					Start();
				}
				else
				{
					Stop();
				}
			}
			get
			{
				return ( _threadTimer != null && _threadTimer.IsAlive );
			}
		}

		public void Start()
		{
			if( Enabled || Interval <= 0 )
			{
				return;
			}

			_stopTimer = false;

			System.Threading.ThreadStart threadStart = delegate()
						{
							NotificationTimer(ref _timerIntervalInMicroSec,
																	ref _ignoreEventIfLateBy,
																	ref _stopTimer);
						};

			_threadTimer = new System.Threading.Thread( threadStart );
			_threadTimer.Priority = System.Threading.ThreadPriority.Highest;
			_threadTimer.Start();
		}

		public void Stop()
		{
			_stopTimer = true;

			if( _threadTimer != null && _threadTimer.ManagedThreadId ==
					System.Threading.Thread.CurrentThread.ManagedThreadId )
			{
				return;
			}

			while( Enabled )
			{
				System.Threading.Thread.SpinWait( 10 );
			}
		}

		void NotificationTimer( ref long timerIntervalInMicroSec,
													 ref long ignoreEventIfLateBy,
													 ref bool stopTimer )
		{
			int  timerCount = 0;
			long nextNotification = 0;

			MicroStopwatch microStopwatch = new MicroStopwatch();
			microStopwatch.Start();

			while( !stopTimer )
			{
				long callbackFunctionExecutionTime =
										microStopwatch.ElapsedMicroseconds - nextNotification;

				long timerIntervalInMicroSecCurrent =
										System.Threading.Interlocked.Read(ref timerIntervalInMicroSec);
				long ignoreEventIfLateByCurrent =
										System.Threading.Interlocked.Read(ref ignoreEventIfLateBy);

				nextNotification += timerIntervalInMicroSecCurrent;
				timerCount++;
				long elapsedMicroseconds = 0;

				while( ( elapsedMicroseconds = microStopwatch.ElapsedMicroseconds )
								< nextNotification )
				{
					System.Threading.Thread.SpinWait( 10 );
				}

				long timerLateBy = elapsedMicroseconds - nextNotification;

				if( timerLateBy >= ignoreEventIfLateByCurrent )
				{
					continue;
				}

				MicroTimerEventArgs microTimerEventArgs =
										 new MicroTimerEventArgs(timerCount,
																						 elapsedMicroseconds,
																						 timerLateBy,
																						 callbackFunctionExecutionTime);
				MicroTimerElapsed( this, microTimerEventArgs );
			}

			microStopwatch.Stop();
		}
	}

	/// <summary>
	/// MicroTimer Event Argument class
	/// </summary>
	public class MicroTimerEventArgs : EventArgs
	{
		// Simple counter, number times timed event (callback function) executed
		public int TimerCount { get; private set; }

		// Time when timed event was called since timer started
		public long ElapsedMicroseconds { get; private set; }

		// How late the timer was compared to when it should have been called
		public long TimerLateBy { get; private set; }

		// Time it took to execute previous call to callback function (OnTimedEvent)
		public long CallbackFunctionExecutionTime { get; private set; }

		public MicroTimerEventArgs( int timerCount,
															 long elapsedMicroseconds,
															 long timerLateBy,
															 long callbackFunctionExecutionTime )
		{
			TimerCount = timerCount;
			ElapsedMicroseconds = elapsedMicroseconds;
			TimerLateBy = timerLateBy;
			CallbackFunctionExecutionTime = callbackFunctionExecutionTime;
		}
	}


	public class Timer
	{
		MicroStopwatch m_watch;
		private long startTime;
		private long stopTime;
		private long freq;
		private long freq_millis;

		public Timer()
		{
			m_watch = new MicroStopwatch();
			//startTime = m_watch.ElapsedMicroseconds;
			//stopTime  = m_watch.ElapsedMicroseconds;
			freq = 1000 * 1000;
			freq_millis = freq / 1000;

			Start();
		}

		// Start the timer

		public Timer Start()
		{
			m_watch.Start();
			startTime = m_watch.ElapsedMicroseconds;
			stopTime = m_watch.ElapsedMicroseconds;
			return this;
		}

		// Stop the timer

		public Timer Stop()
		{
			m_watch.Stop();
			stopTime = m_watch.ElapsedMicroseconds;
			return this;
		}

		public double Seconds
		{
			get
			{
				long current = m_watch.ElapsedMicroseconds;
				return (double)( current - startTime ) / freq;
			}
		}

		public long Current
		{
			get
			{
				long current = m_watch.ElapsedMicroseconds;
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
			get { return ( stopTime - startTime ) / freq_millis; }
		}
	}


	public class TimerWin
	{
		[DllImport( "Kernel32.dll" )]
		private static extern bool QueryPerformanceCounter(
				out long lpPerformanceCount );

		[DllImport( "Kernel32.dll" )]
		private static extern bool QueryPerformanceFrequency(
				out long lpFrequency );

		private long startTime;
		private long stopTime;
		private long freq;
		private long freq_millis;

		// Constructor

		public TimerWin()
		{
			startTime = 0;
			stopTime = 0;

			if( QueryPerformanceFrequency( out freq ) == false )
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

			QueryPerformanceCounter( out startTime );
		}

		// Stop the timer

		public void Stop()
		{
			QueryPerformanceCounter( out stopTime );
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
			get { return ( stopTime - startTime ) / freq_millis; }
		}
	}
}
