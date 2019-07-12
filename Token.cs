using System;
using System.Diagnostics;

namespace lib
{

	//TODO PERF fix this and make it fast.  

	[Serializable]
	public struct Token
	{
		public string str { get { return m_str; } }

		public Token( String str )
		{
			m_str = str;
			m_hash = m_str.GetHashCode();
		}

		public override bool Equals( object obj )
		{
			if( !( obj is Token ) )
				return false;

			//This doesnt use as because Token is a struct
			var otherId = (Token)obj;

			if( m_hash != otherId.m_hash )
				return false;

			return m_str == otherId.m_str;
		}


		public bool Equals_fast( Token other )
		{
			return m_hash == other.m_hash && m_str == other.m_str;
		}

		public override int GetHashCode()
		{
			return m_hash;
		}

		public override string ToString()
		{
			return m_str;
		}

		int     m_hash;
		String  m_str;
	}

}
