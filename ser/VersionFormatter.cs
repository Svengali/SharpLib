using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Diagnostics;
//using System.Globalization;
//using System.ComponentModel;
using System.Runtime.Serialization;

namespace lib
{
	/// <summary>
	/// 
	/// </summary>
	public class VersionFormatter : IFormatter
	{
		public enum ETypes
		{
			Array,
			Int32,
			Ref,
			Object,
			EndObject,
			Single,
			Double,
			Char,
			String,
			Boolean,
			EndStream,
		}


		public VersionFormatter()
		{
			// 
			// TODO: Add constructor logic here
			//
		}


		#region Useless
		public ISurrogateSelector SurrogateSelector
		{
			get
			{
				return null;
			}

			set
			{
			}
		}

		public SerializationBinder Binder
		{
			get
			{
				return null;
			}

			set
			{
			}
		}

		public StreamingContext Context
		{
			get
			{
				return new StreamingContext();
			}

			set
			{
			}
		}
		#endregion Useless

		Queue m_objectsToBeDeserialized = new Queue();
		Hashtable m_alreadyDeserialzied = new Hashtable();
		//int m_GUID                      = 0;

		#region Serialize
		public void Serialize( Stream stream, object obj )
		{
			//Default is 4k
			//BufferedStream bufStream = new BufferedStream( stream );

			BinaryWriter writer = new BinaryWriter( stream );

			writeObject( writer, obj );

			while( m_objectsToBeDeserialized.Count != 0 )
			{
				object objToDes = m_objectsToBeDeserialized.Dequeue();

				writeObject( writer, objToDes );
			}

			writer.Write( (char)ETypes.EndStream );
		}

		void writeRefAndSched( BinaryWriter writer, object obj )
		{
			//if( m_alreadyDeserialzied[ obj.GetType().GetArrayRank(

			if( obj == null )
			{
				writer.Write( 0 );
				return;
			}


			//Now write the address.
			//Bad bad.  Need to do this correctly.
			int objRef = obj.GetHashCode();
			writer.Write( objRef );

			if( m_alreadyDeserialzied[obj] == null )
			{
				m_alreadyDeserialzied[obj] = obj;
				m_objectsToBeDeserialized.Enqueue( obj );
			}
		}

		void dispatchWrite( BinaryWriter writer, object parentObj, FieldInfo fi )
		{
			string typeName = fi.FieldType.Name;

			string name = fi.Name;

			if( fi.IsNotSerialized )
			{
				return;
			}

			if( fi.FieldType.IsArray )
			{
				writer.Write( (char)ETypes.Array );
				writer.Write( name.GetHashCode() );

				writeArray( writer, (Array)fi.GetValue( parentObj ) );
			}
			else if( ( fi.FieldType.IsClass || fi.FieldType.IsInterface ) && typeName != "String" )
			{
				writer.Write( (char)ETypes.Ref );
				writer.Write( name.GetHashCode() );

				writeRefAndSched( writer, fi.GetValue( parentObj ) );
			}
			else if( fi.FieldType.IsEnum )
			{
				writer.Write( (char)ETypes.Int32 );
				writer.Write( name.GetHashCode() );

				write( writer, Convert.ToInt32( fi.GetValue( parentObj ) ) );
			}
			else
			{
				switch( typeName )
				{
					case "Int32":
					writer.Write( (char)ETypes.Int32 );
					writer.Write( name.GetHashCode() );

					write( writer, Convert.ToInt32( fi.GetValue( parentObj ) ) );
					break;
					case "Single":
					writer.Write( (char)ETypes.Single );
					writer.Write( name.GetHashCode() );

					write( writer, Convert.ToSingle( fi.GetValue( parentObj ) ) );
					break;
					case "Double":
					writer.Write( (char)ETypes.Double );
					writer.Write( name.GetHashCode() );

					write( writer, Convert.ToDouble( fi.GetValue( parentObj ) ) );
					break;
					case "Char":
					writer.Write( (char)ETypes.Char );
					writer.Write( name.GetHashCode() );

					write( writer, Convert.ToChar( fi.GetValue( parentObj ) ) );
					break;
					case "String":
					writer.Write( (char)ETypes.String );
					writer.Write( name.GetHashCode() );

					write( writer, Convert.ToString( fi.GetValue( parentObj ) ) );
					break;
					case "Boolean":
					writer.Write( (char)ETypes.Boolean );
					writer.Write( name.GetHashCode() );

					writer.Write( Convert.ToBoolean( fi.GetValue( parentObj ) ) );
					break;
					default:
					Console.WriteLine( "VersionFormatter does not understand type " + typeName );
					break;
				}
			}
		}

		void writeArray( BinaryWriter writer, Array array )
		{
			if( array == null )
			{
				writer.Write( (int)-1 );
				return;
			}

			writer.Write( array.Length );

			foreach( object obj in array )
			{
				writeRefAndSched( writer, obj );
			}
		}

		void getAllFields( object obj, ArrayList list )
		{
			Type t = obj.GetType();

			while( t != null )
			{
				FieldInfo[] fiArr  = t.GetFields( BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly );
				list.AddRange( fiArr );

				t = t.BaseType;
			}
		}


		void writeObject( BinaryWriter writer, object obj )
		{
			Type objType = obj.GetType();

			writer.Write( (char)ETypes.Object );
			writer.Write( objType.FullName );

			int objRef = obj.GetHashCode();
			writer.Write( objRef );

			ArrayList list = new ArrayList();

			getAllFields( obj, list );

			foreach( FieldInfo fi in list )
			{
				dispatchWrite( writer, obj, fi );
			}

			writer.Write( (char)ETypes.EndObject );
		}

		void write<TType>( BinaryWriter wr, TType val )
		{
			//wr.Write( val );
		}

		/*
		void writeInt( BinaryWriter writer, int val )
		{
			writer.Write( val );
		}
		
		void writeSingle( BinaryWriter writer, float val )
		{
			writer.Write( val );
		}
		
		void writeDouble( BinaryWriter writer, double val )
		{
			writer.Write( val );
		}
		
		void writeChar( BinaryWriter writer, char val )
		{
			writer.Write( val );
		}
		
		void writeString( BinaryWriter writer, string val )
		{
			writer.Write( val );
		}
		
		void writeBool( BinaryWriter writer, bool val )
		{
			writer.Write( val );
		}
		*/
		#endregion Serialize


		#region Deserialize

		class Fixup
		{
			public Fixup( int guid, object obj, FieldInfo fi )
			{
				m_guid = guid;
				m_obj = obj;
				m_fi = fi;
			}

			public Fixup( int guid, object obj, int index )
			{
				m_guid = guid;
				m_obj = obj;
				m_index = index;
			}

			public readonly int       m_guid = 0;
			public readonly object    m_obj  = null;

			public readonly FieldInfo m_fi   = null;
			public readonly int       m_index= -1;

		}

		Hashtable m_mapGUIDToObject = new Hashtable();
		ArrayList m_fixupList       = new ArrayList();

		ArrayList m_desObjects = new ArrayList();

		public object Deserialize( Stream stream )
		{
			BinaryReader reader = new BinaryReader( stream );

			object objRoot = null;

			//Read in the first object.
			{
				ETypes type = (ETypes)reader.ReadChar();

				Debug.Assert( type == ETypes.Object );

				objRoot = readObject( reader );

				m_desObjects.Add( objRoot );
			}

			bool readObjects = true;

			while( readObjects )
			{
				ETypes type = (ETypes)reader.ReadChar();

				Debug.Assert( type == ETypes.Object || type == ETypes.EndStream );

				if( type == ETypes.Object )
				{
					object obj = readObject( reader );

					m_desObjects.Add( obj );
				}
				else
				{
					Debug.Assert( type == ETypes.EndStream );

					readObjects = false;
				}
			}

			foreach( Fixup fu in m_fixupList )
			{
				//Fixup fix = m_fixups[ 

				object obj = m_mapGUIDToObject[ fu.m_guid ];

				if( obj != null )
				{
					if( fu.m_fi != null )
					{
						fu.m_fi.SetValue( fu.m_obj, obj );
					}
					else
					{
						Debug.Assert( fu.m_index >= 0 );

						object []array = (object [])fu.m_obj;

						array[fu.m_index] = obj;
					}
				}
				else
				{
					Console.WriteLine( "Obj to ref is null." );
				}
			}

			foreach( object obj in m_desObjects )
			{
				if( typeof( IDeserializationCallback ).IsAssignableFrom( obj.GetType() ) )
				{
					IDeserializationCallback desCB = (IDeserializationCallback)obj;

					if( desCB != null )
					{
						desCB.OnDeserialization( this );
					}
				}
			}

			return objRoot;
		}



		bool dispatchRead( BinaryReader reader, object obj, Hashtable ht )
		{

			//Read the type
			ETypes type = (ETypes)reader.ReadChar();

			if( type == ETypes.EndObject )
			{
				return false;
			}

			int nameHash = reader.ReadInt32();

			FieldInfo fi = (FieldInfo)ht[ nameHash ];

			if( fi == null )
			{
				Console.WriteLine( "Field no longer exists" );
			}

			try
			{
				switch( type )
				{
					case ETypes.Array:
					readArray( reader, obj, fi );
					break;
					case ETypes.Int32:
					readInt( reader, obj, fi );
					break;
					case ETypes.Single:
					readSingle( reader, obj, fi );
					break;
					case ETypes.Double:
					readDouble( reader, obj, fi );
					break;
					case ETypes.Char:
					readChar( reader, obj, fi );
					break;
					case ETypes.Boolean:
					readBool( reader, obj, fi );
					break;
					case ETypes.String:
					readString( reader, obj, fi );
					break;
					case ETypes.Ref:
					readRef( reader, obj, fi );
					break;
					case ETypes.Object:
					readObject( reader );
					break;
					default:
					Debug.Fail( "Unknown type on read." );
					break;
				}
			}
			catch( Exception ex )
			{
				Console.WriteLine( "Exception: " + ex.Message );
				Console.WriteLine( "Stack: " + ex.StackTrace );
			}


			return true;
		}

		object createObject( string objTypeName )
		{
			Assembly[] ass = AppDomain.CurrentDomain.GetAssemblies();

			foreach( Assembly a in ass )
			{
				Type t = a.GetType( objTypeName );

				if( t != null )
				{
					object obj = FormatterServices.GetUninitializedObject( t );

					if( obj != null )
					{
						return obj;
					}
				}
			}

			return null;
		}


		object readObject( BinaryReader reader )
		{
			//ETypes type = (ETypes)reader.ReadChar();

			//Debug.Assert( type == ETypes.Object, "Expecting type Object" );

			string objTypeName = reader.ReadString();
			int    objGUID = reader.ReadInt32();

			try
			{
				object obj = createObject( objTypeName );

				m_mapGUIDToObject[objGUID] = obj;

				ArrayList list = new ArrayList();
				Hashtable ht   = new Hashtable();

				if( obj != null )
				{
					getAllFields( obj, list );

					foreach( FieldInfo fi in list )
					{
						ht[fi.Name.GetHashCode()] = fi;
					}
				}

				while( dispatchRead( reader, obj, ht ) )
				{
				}

				return obj;
			}
			catch( Exception ex )
			{
				Console.WriteLine( "Exception: " + ex.Message );
			}

			return null;
		}

		void readArray( BinaryReader reader, object obj, FieldInfo fi )
		{
			int length = reader.ReadInt32();

			if( length < 0 )
			{
				if( fi == null )
					return;

				fi.SetValue( obj, null );

				return;
			}

			object[] array = new object[length];

			if( fi != null )
			{
				fi.SetValue( obj, array );
			}

			for( int i = 0; i < length; ++i )
			{
				int val = reader.ReadInt32();

				//m_fixups[ val ] = new Fixup( obj, fi );

				if( fi != null )
				{
					m_fixupList.Add( new Fixup( val, array, i ) );
				}
			}
		}

		void readRef( BinaryReader reader, object obj, FieldInfo fi )
		{
			int val = reader.ReadInt32();

			//m_fixups[ val ] = new Fixup( obj, fi );

			m_fixupList.Add( new Fixup( val, obj, fi ) );
		}

		void readInt( BinaryReader reader, object obj, FieldInfo fi )
		{
			int val = reader.ReadInt32();

			if( fi == null )
				return;

			if( !fi.FieldType.IsEnum )
			{
				fi.SetValue( obj, val );
			}
			else
			{
				object enumVal = Enum.Parse( fi.FieldType, val.ToString() );
				fi.SetValue( obj, Convert.ChangeType( enumVal, fi.FieldType ) );
			}

		}

		void readSingle( BinaryReader reader, object obj, FieldInfo fi )
		{
			float val = reader.ReadSingle();

			if( fi == null )
				return;

			fi.SetValue( obj, val );
		}

		void readDouble( BinaryReader reader, object obj, FieldInfo fi )
		{
			double val = reader.ReadDouble();

			if( fi == null )
				return;

			fi.SetValue( obj, val );
		}

		void readChar( BinaryReader reader, object obj, FieldInfo fi )
		{
			char val = reader.ReadChar();

			if( fi == null )
				return;

			fi.SetValue( obj, val );
		}

		void readString( BinaryReader reader, object obj, FieldInfo fi )
		{
			string val = reader.ReadString();

			if( fi == null )
				return;

			fi.SetValue( obj, val );
		}

		void readBool( BinaryReader reader, object obj, FieldInfo fi )
		{
			bool val = reader.ReadBoolean();

			if( fi == null )
				return;

			fi.SetValue( obj, val );
		}

		#endregion Deserialize


	}
}



















