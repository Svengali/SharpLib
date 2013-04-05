using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net.Sockets;
using System.IO;

//using Util;

namespace lib
{



public class Conn
{
	public Socket Sock { get { return m_socket; } }
	public Stream Stream { get { return m_streamNet; } }
	public IFormatter Formatter { get { return m_formatter; } }


	public Conn( Socket sock, IFormatter formatter )
	{
		m_socket = sock;

		//sock.DontFragment = true;
		sock.NoDelay = true;
			
		m_streamNet = new NetworkStream( m_socket );
		//m_streamBufIn = new BufferedStream( m_streamNet );

		m_formatter = formatter;
		//m_formatter = new VersionFormatter();

		//mm_memStream = new MemoryStream( mm_buffer );


	}

	public object recieveObject()
	{
		return recieveObject( Stream );
	}

	public object recieveObject( Stream stream )
	{
		object obj = null;
		lock( this )
		{
			try
			{
				obj = m_formatter.Deserialize( stream );
			}
			catch( System.Xml.XmlException e )
			{
				lib.Log.error( "Outer Exception {0}", e.ToString() );
				//lib.Log.error( "Inner Exception {0}", e.InnerException.ToString() );
			}
		}

		return obj;
	}

	public void send( object obj )
	{
		lock( this )
		{
			try
			{
				var ms = new MemoryStream( 1024 );
				m_formatter.Serialize( ms, obj );

				//var str = System.Text.Encoding.Default.GetString( mm_buffer, 0, (int)ms.Position );
				//lib.Log.info( "Sent data {0} of length {1}", str, ms.Position );
				//lib.Log.info( "Sent {0}", obj );

				byte[] byteSize = BitConverter.GetBytes( (uint)ms.Position );
				m_streamNet.Write( byteSize, 0, 4 );
				m_streamNet.Write( ms.GetBuffer(), 0, (int)ms.Position );

				m_streamNet.Flush();
			}
			catch( Exception e )
			{
				lib.Log.warn( "Exception sending obj {0} of {1}", obj, e );
				//m_streamNet.Close();
				//m_socket.Close();
			}
		}
	}
		
	public virtual void recieve( object obj )
	{
		//Log.log.msg( "Recieved " + obj.ToString() );
	}
		
	private Socket m_socket;

	private NetworkStream m_streamNet;
	//private BufferedStream m_streamBufIn;
	//private BufferedStream m_streamBufOut;
		
	private IFormatter m_formatter;
		
}



}
