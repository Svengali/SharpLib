using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net.Sockets;
using System.IO;

//using Util;

namespace lib
{




	public interface IProcess
	{
		void process( object obj );
	}


	public interface ISerDes<T> where T : IFormatter
	{

		T getInstance();



	}


	public class NewEveryCall<T>: ISerDes<T> where T : IFormatter, new()
	{
		public T getInstance()
		{
			return new T();
		}
	}

	public class Conn
	{
		public static int BufferSize = 2048;
	}


	public class Conn<T, TInst> : Conn
	                              where T : IFormatter, new()
	                              where TInst : ISerDes<T>, new()
	{
		public Socket Sock { get { return m_socket; } }
		public Stream Stream { get { return m_streamNet; } }


		private TInst m_formatter = new TInst();


		public Conn( Socket sock, IProcess proc )
		{
			m_socket = sock;

			sock.NoDelay = true;

			m_streamNet = new NetworkStream( m_socket );

			m_proc = proc;
		}

		public object recieveObject()
		{
			return recieveObject( Stream );
		}

		public object recieveObject( Stream stream )
		{
			object obj = null;

			var formatter = m_formatter.getInstance();

			try
			{
				obj = formatter.Deserialize( stream );
			}
			catch( System.Xml.XmlException ex )
			{
				log.error( $"Outer Exception {ex.Message}" );
			}

			return obj;
		}

		public void send( object obj )
		{

			var formatter = m_formatter.getInstance();

			try
			{
				var ms = new MemoryStream( BufferSize );
				formatter.Serialize( ms, obj );

				//var str = System.Text.Encoding.Default.GetString( mm_buffer, 0, (int)ms.Position );
				//log.info( $"Sent data {str} of length {ms.Position}" );
				//log.info( $"Sent {obj}" );

				byte[] byteSize = BitConverter.GetBytes( (uint)ms.Position );
				m_streamNet.Write( byteSize, 0, 4 );
				m_streamNet.Write( ms.GetBuffer(), 0, (int)ms.Position );

				m_streamNet.Flush();
			}
			catch( Exception e )
			{
				log.warn( $"Exception sending obj {obj} of {e}" );
				throw;
			}
		}

		public virtual void recieve( object obj )
		{
			if( m_proc != null )
				m_proc.process( obj );
		}

		Socket m_socket;

		NetworkStream m_streamNet;

		IProcess m_proc;



		//private BufferedStream m_streamBufIn;
		//private BufferedStream m_streamBufOut;
	}



}
