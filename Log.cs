using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Runtime.CompilerServices;
//using System.Threading.Tasks;

namespace lib
{



	[Flags]
	public enum LogTypeNew
	{
		Invalid		= 0,

		// Frequency
		FrequencyBase		= 1,
		FrequencyBits		= 2,
		FrequencyMask		= ( ( 1 << FrequencyBits ) - 1 ) << FrequencyBase,

		Detail		= 0b01 << FrequencyBase,
		Normal		= 0b10 << FrequencyBase,
		Overview	= 0b11 << FrequencyBase,

		// Type
		TypeBase = FrequencyBase + FrequencyBits,
		TypeBits = 3,
		TypeMask = ( ( 1 << TypeBits ) - 1 ) << TypeBase,

		Startup	= 0b001 << TypeBase,
		Running	= 0b010 << TypeBase,
		Shutdown= 0b011 << TypeBase,
		Error		= 0b101 << TypeBase,

	}


	[Flags]
	public enum LogType
	{
		Invalid	= 0,
		Trace		= 1,
		Debug		= 2,
		Info		= 3,
		Warn		= 4,
		Error		= 5,
		Fatal		= 6,
	}

	public struct LogEvent
	{
		public DateTime Time;
		public LogType	LogType;
		public string		Cat;
		public string		Msg;
		public object		Obj;

		public LogEvent( LogType logType, string cat, string msg, object obj )
		{
			Time = DateTime.Now;
			LogType = logType;
			Cat = cat;
			Msg = msg;
			Obj = obj;
		}
	}

	public delegate void Log_delegate( LogEvent evt );


	public class Log : TraceListener 
	{
		static public void create( string filename )
		{
			s_log = new Log( filename );
		}

		static public void destroy()
		{
			string msg = "==============================================================================\nLogfile shutdown at " + DateTime.Now.ToString();

			var evt = new LogEvent( LogType.Info, "System", msg, null );

			s_log.writeToAll( evt );

			s_log.stop();

			s_log = null;
		}

		static private Log s_log;

		/*
		static public Log log
		{
			get 
			{
				return s_log;
			}
		}
		*/

		// Forwards.
		static public void fatal( string msg, string cat = "unknown", object obj = null )
		{
			log(msg, LogType.Fatal, cat, obj);
		}

		static public void error( string msg, string cat = "unknown", object obj = null )
		{
			log(msg, LogType.Error, cat, obj);
		}

		static public void warn( string msg, string cat = "unknown", object obj = null ) 
		{
			log( msg, LogType.Warn, cat, obj );
		}

		static public void info( string msg, string cat = "unknown", object obj = null )
		{
			log(msg, LogType.Info, cat, obj);
		}

		static public void debug( string msg, string cat = "unknown", object obj = null )
		{
			log(msg, LogType.Debug, cat, obj);
		}

		static public void trace( string msg, string cat = "unknown", object obj = null )
		{
			log(msg, LogType.Trace, cat, obj);
		}

		static public void log( string msg, LogType type = LogType.Debug, string cat = "unknown", object obj = null )
		{
			lock(s_log)
			{
				var evt = new LogEvent( type, cat, msg, obj );

				s_log.writeToAll( evt );
			}
		}


		private Log( string filename )
		{
			//TODO: Fix this so itll work without a directory.
			Directory.CreateDirectory( Path.GetDirectoryName( filename ) );

			m_stream = new FileStream( filename, FileMode.Append, FileAccess.Write );
			m_writer = new StreamWriter( m_stream );
			
			m_errorStream = new FileStream( filename + ".error", FileMode.Append, FileAccess.Write );
			m_errorWriter = new StreamWriter( m_errorStream );
			
			Debug.Listeners.Add( this );

			string msg = "\n==============================================================================\nLogfile " +  filename + " startup at " + DateTime.Now.ToString();

			var evt = new LogEvent( LogType.Info, "System", msg, null );

			writeToAll( evt );
		}

		public override void Write( string msg )
		{
			WriteLine( msg );
		}
		
		public override void WriteLine( string msg )
		{
			error( msg );
			//base.WriteLine( msg );
		}

		void stop()
		{
			m_writer.Close();
			m_stream.Close();

			m_errorWriter.Close();
			m_errorStream.Close();
		}

		public void addDelegate( Log_delegate cb )
		{
			m_delegates.Add( cb );
		}
		
		/*
		private void writeFileAndLine( StackTrace st )
		{
			StackFrame frame = st.GetFrame( 1 );
			
			string srcFile = frame.GetFileName();
			string srcLine = frame.GetFileLineNumber().Tostring();
			
			Console.WriteLine( "{0} ({1}):", srcFile, srcLine );
		}
		
		private void writeStack( StackTrace st )
		{
			for( int i=0; i<st.FrameCount; ++i )
			{
				StackFrame frame = st.GetFrame( i );
			
				string srcFile = frame.GetFileName();
				string srcLine = frame.GetFileLineNumber().Tostring();
			
				if( srcFile != null )
				{			
					Console.WriteLine( "{0} ({1})", srcFile, srcLine );
				}
			}
		}
		*/

		private char getSymbol( LogType type )
		{
			switch( type )
			{
				case LogType.Trace:
				return '.';
				case LogType.Debug:
				return '-';
				case LogType.Info:
				return ' ';
				case LogType.Warn:
				return '+';
				case LogType.Error:
				return '*';
				case LogType.Fatal:
				return '*';
				default: return '?';
			}
		}

		private void writeToAll( LogEvent evt )
		{
			try
			{
				// _SHOULDNT_ need this since we lock at the top.  
				//lock( this )
				{
					char sym = getSymbol( evt.LogType );

					string finalMsg = string.Format( "{0,-6}{1}| {2}", evt.Cat, sym, evt.Msg );

					//Console.WriteLine( finalMsg );
					//Console.Out.Write( finalMsg );

					foreach( var l_obj in Debug.Listeners )
					{
						var l = l_obj as TraceListener;
						if( l != null && l != this )
						{
							l.WriteLine( finalMsg );
						}
					}

					m_writer.WriteLine( finalMsg );

					m_writer.Flush();

					foreach( Log_delegate cb in m_delegates )
					{
						//lock( cb )
						{
							cb( evt );
						}
					}
				}
			}
			catch( Exception )
			{
				//oops.  
				//int dummy = 0;
			}
		}

		/*
		private void error_i( string msg, object obj )
		{
			//var t = Task.Run( () => {
				StackTrace st = new StackTrace( true );

				writeStack( st );
			
				string msgPrint = msg;

				if( args.Length > 0 )
				{
					msgPrint = string.Format( msg, args );
				}

				writeToAll( "error", "log", msgPrint );
			//} );			
		}
		private void warn_i( string msg, object obj )
		{
			//var t = Task.Run( () => {
				StackTrace st = new StackTrace( true );

				writeStack( st );

				string msgPrint = msg;

				if( args.Length > 0 )
				{
					msgPrint = string.Format( msg, args );
				}

				writeToAll( "warn", "log", msgPrint );
			//});
		}
		private void info_i( string msg, object obj )
		{
			//var t = Task.Run( () => {
				StackTrace st = new StackTrace( true );

				string msgPrint = msg;

				if( args.Length > 0 )
				{
					msgPrint = string.Format( msg, args );
				}

				writeToAll( "info", "log", msgPrint );
			//} );
		}
		*/
		
		private Stream       m_stream;
		private StreamWriter m_writer;
		
		private Stream       m_errorStream;
		private StreamWriter m_errorWriter;
		
		private ArrayList    m_delegates = new ArrayList();
	}
	
	
	
	
	
	
	
	
	
}
