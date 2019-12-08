using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
//using System.Threading.Tasks;

namespace lib
{



	[Flags]
	public enum LogTypeNew
	{
		Invalid = 0,

		// Frequency
		FrequencyBase = 1,
		FrequencyBits = 2,
		FrequencyMask = ( ( 1 << FrequencyBits ) - 1 ) << FrequencyBase,

		Detail = 0b01 << FrequencyBase,
		Normal = 0b10 << FrequencyBase,
		Overview = 0b11 << FrequencyBase,

		// Type
		TypeBase = FrequencyBase + FrequencyBits,
		TypeBits = 3,
		TypeMask = ( ( 1 << TypeBits ) - 1 ) << TypeBase,

		Startup = 0b001 << TypeBase,
		Running = 0b010 << TypeBase,
		Shutdown = 0b011 << TypeBase,
		Error = 0b101 << TypeBase,

	}


	[Flags]
	public enum LogType
	{
		Invalid = 0,
		Trace   = 1,
		Debug   = 2,
		Info    = 3,
		High    = 4,
		Warn    = 5,
		Error   = 6,
		Fatal   = 7,
	}

	public struct LogEvent
	{
		public DateTime Time;
		public LogType  LogType;
		public string   Msg;
		public string   Path;
		public int      Line;
		public string   Member;

		public string   Cat;
		public object   Obj;



		static ImmutableDictionary<string, string> m_shortname = ImmutableDictionary<string, string>.Empty;


		public LogEvent( LogType logType, string msg, string path, int line, string member, string cat, object obj )
		{

			//Cache the automatic category names
			if( string.IsNullOrEmpty( cat ) )
			{
				if( m_shortname.TryGetValue( path, out var autoCat ) )
				{
					cat = autoCat;
				}
				else
				{
					var pathPieces = path.Split('\\');

					var lastDir = pathPieces[pathPieces.Length - 2];

					ImmutableInterlocked.AddOrUpdate( ref m_shortname, path, lastDir, ( key, value ) => { return lastDir; } );

					cat = lastDir;
				}
			}

			Time = DateTime.Now;
			LogType = logType;
			Msg = msg;
			Path = path;
			Line = line;
			Member = member;
			Cat = cat;
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

			var evt = CreateLogEvent( LogType.Info, msg, "System", null );

			s_log.writeToAll( evt );

			s_log.stop();

			s_log = null;
		}

		static public Log s_log;

		/*
		static public Log log
		{
			get 
			{
				return s_log;
			}
		}
		*/


		static LogEvent CreateLogEvent( LogType logType, string msg, string cat, object obj, [CallerFilePath] string path = "", [CallerLineNumber] int line = -1, [CallerMemberName] string member = "" )
		{
			var logEvent = new LogEvent( logType, msg, path, line, member, cat, obj );

			return logEvent;
		}




		// Forwards.
		static public void fatal( string msg, string cat = "", object obj = null, [CallerFilePath] string path = "", [CallerLineNumber] int line = -1, [CallerMemberName] string member = "" )
		{
			log( msg, LogType.Fatal, path, line, member, cat, obj );
		}

		static public void error( string msg, string cat = "", object obj = null, [CallerFilePath] string path = "", [CallerLineNumber] int line = -1, [CallerMemberName] string member = "" )
		{
			log( msg, LogType.Error, path, line, member, cat, obj );
		}

		static public void warn( string msg, string cat = "", object obj = null, [CallerFilePath] string path = "", [CallerLineNumber] int line = -1, [CallerMemberName] string member = "" )
		{
			log( msg, LogType.Warn, path, line, member, cat, obj );
		}

		static public void info( string msg, string cat = "", object obj = null, [CallerFilePath] string path = "", [CallerLineNumber] int line = -1, [CallerMemberName] string member = "" )
		{
			log( msg, LogType.Info, path, line, member, cat, obj );
		}

		static public void high( string msg, string cat = "", object obj = null, [CallerFilePath] string path = "", [CallerLineNumber] int line = -1, [CallerMemberName] string member = "" )
		{
			log( msg, LogType.High, path, line, member, cat, obj );
		}

		static public void debug( string msg, string cat = "", object obj = null, [CallerFilePath] string path = "", [CallerLineNumber] int line = -1, [CallerMemberName] string member = "" )
		{
			log( msg, LogType.Debug, path, line, member, cat, obj );
		}

		static public void trace( string msg, string cat = "", object obj = null, [CallerFilePath] string path = "", [CallerLineNumber] int line = -1, [CallerMemberName] string member = "" )
		{
			log( msg, LogType.Trace, path, line, member, cat, obj );
		}

		static public void log( string msg, LogType type = LogType.Debug, string path = "", int line = -1, string member = "", string cat = "unk", object obj = null )
		{
			// @@@@@ TODO Get rid of this lock. 
			var evt = new LogEvent( type, msg, path, line, member, cat, obj );

			lock( s_log )
			{
				s_log.writeToAll( evt );
			}
		}

		static public void logProps( object obj, string header, LogType type = LogType.Debug, string cat = "", string prefix = "" )
		{
			var list = scr.GetAllProperties( obj.GetType() );

			lock( s_log )
			{
				var evt = CreateLogEvent( type, header, cat, obj );

				s_log.writeToAll( evt );

				foreach( var pi in list )
				{
					try
					{
						var v = pi.GetValue( obj );

						log( $"{prefix}{pi.Name} = {v}", type, cat );
					}
					catch( Exception ex )
					{
						log( $"Exception processing {pi.Name} {ex.Message}", LogType.Error, "log" );
					}
				}

			}
		}

		//This might seem a little odd, but the intent is that usually you wont need to set notExpectedValue. 
		static public void expected<T>( T value, string falseString, string trueString = "", T notExpectedValue = default( T ) )
		{

			if( !value.Equals( notExpectedValue ) )
			{
				lib.Log.info( $"Properly got {value}{trueString}" );
			}
			else
			{
				lib.Log.warn( $"Got {notExpectedValue} instead of {value}{falseString}" );
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

			//Debug.Listeners.Add( this );

			string msg = "\n==============================================================================\nLogfile " +  filename + " startup at " + DateTime.Now.ToString();

			var evt = CreateLogEvent( LogType.Info, msg, "System", null );

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
			
			Console.WriteLine( $"{srcFile} ({srcLine}):" );
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
					Console.WriteLine( $"{srcFile} ({srcLine})" );
				}
			}
		}
		*/

		public static char getSymbol( LogType type )
		{
			switch( type )
			{
				case LogType.Trace:
				return ' ';
				case LogType.Debug:
				return ' ';
				case LogType.Info:
				return ' ';
				case LogType.High:
				return '+';
				case LogType.Warn:
				return '+';
				case LogType.Error:
				return '*';
				case LogType.Fatal:
				return '*';
				default:
				return '?';
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

					var truncatedCat = evt.Cat.Substring(0, Math.Min( 8, evt.Cat.Length ) );

					string finalLine = string.Format( "{0,-8}{1}| {2}", truncatedCat, sym, evt.Msg );

					//Console.WriteLine( finalMsg );
					//Console.Out.Write( finalMsg );

					/*
					foreach( var l_obj in Debug.Listeners )
					{
						var l = l_obj as TraceListener;
						if( l != null && l != this )
						{
							l.WriteLine( finalMsg );
						}
					}
					*/

					m_writer.WriteLine( finalLine );

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
			catch( Exception ex )
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
