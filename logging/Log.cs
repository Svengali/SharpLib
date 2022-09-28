using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
//using System.Threading.Tasks;

static public class log
{


	[Flags]
	public enum LogType
	{
		Invalid = 0,
		Trace = 1,
		Debug = 2,
		Info = 3,
		High = 4,
		Warn = 5,
		Error = 6,
		Fatal = 7,
	}

	[Flags]
	public enum Endpoints
	{
		Invalid = 0,
		File = 1 <<	0,
		Console = 1 << 1,
	}


	public struct LogEvent
	{
		public DateTime Time;
		public LogType LogType;
		public string Msg;
		public string Path;
		public int Line;
		public string Member;

		public string Cat;
		public object Obj;



		static ImmutableDictionary<int, string> s_shortname = ImmutableDictionary<int, string>.Empty;


		public LogEvent( LogType logType, string msg, string path, int line, string member, string cat, object obj )
		{

			//Cache the automatic category names
			if( string.IsNullOrEmpty( cat ) )
			{
				var pathHash = path.GetHashCode();
				if( s_shortname.TryGetValue( pathHash, out var autoCat ) )
				{
					cat = autoCat;
				}
				else
				{
					var pathPieces = path.Split( '\\' );

					var lastDir = pathPieces[ pathPieces.Length - 2 ];

					ImmutableInterlocked.AddOrUpdate( ref s_shortname, pathHash, lastDir, ( key, value ) => { return lastDir; } );

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




	static public void create( string filename, Endpoints endpoints )
	{
		startup( filename, endpoints );
	}


	static public void destroy()
	{
		string msg = "==============================================================================\nLogfile shutdown at " + DateTime.Now.ToString();

		var evt = CreateLogEvent( LogType.Info, msg, "System", null );

		s_events.Enqueue( evt );

		stop();
	}


	static LogEvent CreateLogEvent( LogType logType, string msg, string cat, object obj, [CallerFilePath] string path = "", [CallerLineNumber] int line = -1, [CallerMemberName] string member = "" )
	{
		var logEvent = new LogEvent( logType, msg, path, line, member, cat, obj );

		return logEvent;
	}

	static internal ConcurrentQueue<LogEvent> s_events = new ConcurrentQueue<LogEvent>();

	static private Thread s_thread;

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
	static public void fatal( string msg, string cat = "", object obj = null, [CallerFilePath] string path = "", [CallerLineNumber] int line = -1, [CallerMemberName] string member = "" )
	{
		logBase( msg, LogType.Fatal, path, line, member, cat, obj );
	}

	static public void error( string msg, string cat = "", object obj = null, [CallerFilePath] string path = "", [CallerLineNumber] int line = -1, [CallerMemberName] string member = "" )
	{
		logBase( msg, LogType.Error, path, line, member, cat, obj );
	}

	static public void warn( string msg, string cat = "", object obj = null, [CallerFilePath] string path = "", [CallerLineNumber] int line = -1, [CallerMemberName] string member = "" )
	{
		logBase( msg, LogType.Warn, path, line, member, cat, obj );
	}

	static public void high(string msg, string cat = "", object obj = null, [CallerFilePath] string path = "", [CallerLineNumber] int line = -1, [CallerMemberName] string member = "")
	{
		logBase(msg, LogType.High, path, line, member, cat, obj);
	}

	static public void info( string msg, string cat = "", object obj = null, [CallerFilePath] string path = "", [CallerLineNumber] int line = -1, [CallerMemberName] string member = "" )
	{
		logBase( msg, LogType.Info, path, line, member, cat, obj );
	}

	static public void debug( string msg, string cat = "", object obj = null, [CallerFilePath] string path = "", [CallerLineNumber] int line = -1, [CallerMemberName] string member = "" )
	{
		logBase( msg, LogType.Debug, path, line, member, cat, obj );
	}

	static public void trace( string msg, string cat = "", object obj = null, [CallerFilePath] string path = "", [CallerLineNumber] int line = -1, [CallerMemberName] string member = "" )
	{
		logBase( msg, LogType.Trace, path, line, member, cat, obj );
	}

	static object s_lock = new object();

	static public void logBase_old( string msg, LogType type = LogType.Debug, string path = "", int line = -1, string member = "", string cat = "unk", object obj = null )
	{
		// @@@@@ TODO Get rid of this lock. 
		var evt = new LogEvent( type, msg, path, line, member, cat, obj );

		lock( s_lock )
		{
			writeToAll( evt );
		}
	}

	static public void logBase( string msg, LogType type = LogType.Debug, string path = "", int line = -1, string member = "", string cat = "unk", object obj = null )
	{
		var evt = new LogEvent( type, msg, path, line, member, cat, obj );

		s_events.Enqueue( evt );
	}


	static public void logProps( object obj, string header, LogType type = LogType.Debug, string cat = "", string prefix = "", [CallerFilePath] string path = "", [CallerLineNumber] int line = -1, [CallerMemberName] string member = "" )
	{
		var list = refl.GetAllProperties( obj.GetType() );

		lock( s_lock )
		{
			var evt = new LogEvent( type, header, path, line, member, cat, obj );
			//var evt = CreateLogEvent( type, header, cat, obj );

			//lock( s_log )
			{
				//var evt = CreateLogEvent( type, header, cat, obj );

				s_events.Enqueue( evt );

				//s_log.writeToAll( evt );

				foreach( var pi in list )
				{
					try
					{
						var v = pi.GetValue( obj );

						logBase( $"{prefix}{pi.Name} = {v}", type, path, line, member, cat );
					}
					catch( Exception ex )
					{
						logBase( $"Exception processing {pi.Name} {ex.Message}", LogType.Error, "log" );
					}
				}

			}
		}
	}

	//This might seem a little odd, but the intent is that usually you wont need to set notExpectedValue. 
	static public void expected<T>( T value, string falseString, string trueString = "", T notExpectedValue = default( T ) )
	{

		if( !value.Equals( notExpectedValue ) )
		{
			log.info( $"Properly got {value}{trueString}" );
		}
		else
		{
			log.warn( $"Got {notExpectedValue} instead of {value}{falseString}" );
		}
	}


	static void startup( string filename, Endpoints endpoints )
	{
		var start = new ThreadStart( run );

		s_thread = new Thread( start );
		s_thread.Start();

		//TODO: Fix this so itll work without a directory.
		Directory.CreateDirectory( Path.GetDirectoryName( filename ) );

		string dir = Path.GetDirectoryName( filename );

		if( dir.Length > 0 )
		{
			Directory.CreateDirectory( dir );
		}

		s_stream = new FileStream( filename, FileMode.Append, FileAccess.Write );
		s_writer = new StreamWriter( s_stream );

		s_errorStream = new FileStream( filename + ".error", FileMode.Append, FileAccess.Write );
		s_errorWriter = new StreamWriter( s_errorStream );

		//Debug.Listeners.Add( this );

		//var evt = CreateLogEvent( LogType.Info, $"startup", "System", null );

		//s_events.Enqueue( evt );

		/*
		if( (endpoints & Endpoints.Console) == Endpoints.Console )
		{
			addDelegate(WriteToConsole);
		}
		*/



		info( $"startup" );

	}

	static bool s_running = true;

	static void run()
	{
		while( s_running )
		{
			while( s_events.TryDequeue( out var evt ) )
			{
				writeToAll( evt );
			}

			// TODO PERF Replace this with a semaphore/mutex
			Thread.Sleep( 0 );
		}
	}

	static void stop()
	{
		s_running = false;

		s_writer.Close();
		s_stream.Close();

		s_errorWriter.Close();
		s_errorStream.Close();

	}

	static public void addDelegate( Log_delegate cb )
	{
		s_delegates.Add( cb );
	}

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

	private static void setConsoleColor( log.LogEvent evt )
	{
		switch( evt.LogType )
		{
			case log.LogType.Trace:
				Console.ForegroundColor = ConsoleColor.DarkGray;
				break;
			case log.LogType.Debug:
				Console.ForegroundColor = ConsoleColor.Gray;
				break;
			case log.LogType.Info:
				Console.ForegroundColor = ConsoleColor.DarkGreen;
				break;
			case log.LogType.High:
				Console.ForegroundColor = ConsoleColor.Cyan;
				break;
			case log.LogType.Warn:
				Console.ForegroundColor = ConsoleColor.Yellow;
				break;
			case log.LogType.Error:
				Console.ForegroundColor = ConsoleColor.DarkRed;
				Console.BackgroundColor = ConsoleColor.DarkGray;
				break;
			case log.LogType.Fatal:
				Console.ForegroundColor = ConsoleColor.Red;
				Console.BackgroundColor = ConsoleColor.DarkGray;
				break;
		}
	}


	static private void writeToAll( LogEvent evt )
	{
		try
		{
			// _SHOULDNT_ need this since we lock at the top.  
			//lock( this )
			{
				char sym = getSymbol( evt.LogType );


				var truncatedCat = evt.Cat.Substring( 0, Math.Min( 8, evt.Cat.Length ) );

				string finalLine = string.Format( "{0,-8}{1}| {2}", truncatedCat, sym, evt.Msg );

				//Console.WriteLine( finalMsg );
				//Console.Out.Write( finalMsg );

				s_writer.WriteLine( finalLine );

				setConsoleColor( evt );
					Console.WriteLine( finalLine );
				Console.ResetColor();


				//Debug.WriteLine( finalLine );

				s_writer.Flush();

				foreach( Log_delegate cb in s_delegates )
				{
					{
						cb( evt );
					}
				}
			}
		}
		catch( Exception ex )
		{
			Console.WriteLine( "EXCEPTION DURING LOGGING" );
			Console.WriteLine( "EXCEPTION DURING LOGGING" );
			Console.WriteLine( "EXCEPTION DURING LOGGING" );
			Console.WriteLine( "EXCEPTION DURING LOGGING" );
			Console.WriteLine( "EXCEPTION DURING LOGGING" );
			Console.WriteLine( $"Exception {ex}" );

			Debug.WriteLine( "EXCEPTION DURING LOGGING" );
			Debug.WriteLine( "EXCEPTION DURING LOGGING" );
			Debug.WriteLine( "EXCEPTION DURING LOGGING" );
			Debug.WriteLine( "EXCEPTION DURING LOGGING" );
			Debug.WriteLine( "EXCEPTION DURING LOGGING" );
			Debug.WriteLine( $"Exception {ex}" );
		}
	}

	public static void WriteToConsole(LogEvent evt)
	{
		char sym = getSymbol(evt.LogType);

		var truncatedCat = evt.Cat.Substring(0, Math.Min(8, evt.Cat.Length));

		string finalLine = string.Format("{0,-8}{1}| {2}", truncatedCat, sym, evt.Msg);

		Console.WriteLine(finalLine);
	}




	private static Stream s_stream;
	private static StreamWriter s_writer;

	private static Stream s_errorStream;
	private static StreamWriter s_errorWriter;

	private static ArrayList s_delegates = new ArrayList();









}
