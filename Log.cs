using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
//using System.Threading.Tasks;

namespace lib
{

	public delegate void Log_delegate( String type, String cat, String msg );

	public class Log : TraceListener 
	{
		static public void create( String filename )
		{
			s_log = new Log( filename );
		}

		static public void destroy()
		{
			string msg = "==============================================================================\nLogfile shutdown at " + DateTime.Now.ToString();

			s_log.writeToAll( "info", "log", msg );

			s_log.stop();

			s_log = null;
		}

		static private Log s_log;
	
		static public Log log
		{
			get 
			{
				return s_log;
			}
		}

		// Forwards.
		static public void error( String msg, params object[] args ) { lock( s_log ) { log.error_i( msg, args ); } }
		static public void warn( String msg, params object[] args ) { lock( s_log ) { log.warn_i( msg, args ); } }
		static public void info( String msg, params object[] args ) { lock( s_log ) { log.info_i( msg, args ); } }


		private Log( String filename )
		{
			//TODO: Fix this so itll work without a directory.
			Directory.CreateDirectory( Path.GetDirectoryName( filename ) );

			m_stream = new FileStream( filename, FileMode.Append, FileAccess.Write );
			m_writer = new StreamWriter( m_stream );
			
			m_errorStream = new FileStream( filename + ".error", FileMode.Append, FileAccess.Write );
			m_errorWriter = new StreamWriter( m_errorStream );
			
			Debug.Listeners.Add( this );

			string msg = "\n==============================================================================\nLogfile " +  filename + " startup at " + DateTime.Now.ToString();

			writeToAll( "info", "log", msg );
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
		
		private void writeFileAndLine( StackTrace st )
		{
			StackFrame frame = st.GetFrame( 1 );
			
			String srcFile = frame.GetFileName();
			String srcLine = frame.GetFileLineNumber().ToString();
			
			Console.WriteLine( "{0} ({1}):", srcFile, srcLine );
		}
		
		private void writeStack( StackTrace st )
		{
			for( int i=0; i<st.FrameCount; ++i )
			{
				StackFrame frame = st.GetFrame( i );
			
				String srcFile = frame.GetFileName();
				String srcLine = frame.GetFileLineNumber().ToString();
			
				if( srcFile != null )
				{			
					Console.WriteLine( "{0} ({1})", srcFile, srcLine );
				}
			}
		}

		private char getSymbol( String type )
		{
			if( type == "info" )
				return ' ';
			if( type == "warn" )
				return '-';
			if( type == "error" )
				return '*';

			return '?';
		}

		private void writeToAll( String type, String cat, String msg )
		{
			try
			{
				lock( this )
				{
					char sym = getSymbol( type );

					String finalMsg = String.Format( "{0,-10}{1}| {2}", type, sym, msg );

					Console.WriteLine( finalMsg );
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
							cb( type, cat, msg );
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

		private void error_i( String msg, params object[] args )
		{
			//var t = Task.Run( () => {
				StackTrace st = new StackTrace( true );

				writeStack( st );
			
				String msgPrint = msg;

				if( args.Length > 0 )
				{
					msgPrint = String.Format( msg, args );
				}

				writeToAll( "error", "log", msgPrint );
			//} );			
		}

		private void warn_i( String msg, params object[] args )
		{
			//var t = Task.Run( () => {
				StackTrace st = new StackTrace( true );

				writeStack( st );

				String msgPrint = msg;

				if( args.Length > 0 )
				{
					msgPrint = String.Format( msg, args );
				}

				writeToAll( "warn", "log", msgPrint );
			//});
		}

		private void info_i( String msg, params object[] args )
		{
			//var t = Task.Run( () => {
				StackTrace st = new StackTrace( true );

				String msgPrint = msg;

				if( args.Length > 0 )
				{
					msgPrint = String.Format( msg, args );
				}

				writeToAll( "info", "log", msgPrint );
			//} );
		}
		
		
		private Stream       m_stream;
		private StreamWriter m_writer;
		
		private Stream       m_errorStream;
		private StreamWriter m_errorWriter;
		
		private ArrayList    m_delegates = new ArrayList();
	}
	
	
	
	
	
	
	
	
	
}
