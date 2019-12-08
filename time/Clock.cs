using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace lib
{
	public class Clock
	{
		public Clock( long timeOffset )
		{
			m_timer = new Timer();

			m_lastTime = m_timer.Current;

			m_totalMillis = timeOffset;
			m_totalSeconds = (double)m_totalMillis / 1000.0;
		}

		public void tick()
		{
			long current = m_timer.Current;

			m_dtMillis = (int)( current - m_lastTime );

			m_dtSeconds = (double)m_dtMillis / 1000.0;

			m_totalMillis += m_dtMillis;
			m_totalSeconds = (double)m_totalMillis / 1000.0;

			m_lastTime = current;
		}

		public int dtMs { get { return m_dtMillis; } }
		public double dtSec { get { return m_dtSeconds; } }

		public long ms { get { return m_totalMillis; } }
		public double sec { get { return m_totalSeconds; } }


		Timer m_timer;

		long m_lastTime = 0;

		int m_dtMillis = 0;
		double m_dtSeconds = 0;

		long m_totalMillis = 0;
		double m_totalSeconds = 0;

	}
}
