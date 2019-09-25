using System;
using System.IO;
using System.Xml;
using System.Runtime.Serialization;
//using System.Web.Configuration;
using System.Collections;
using System.Collections.Generic;


using System.Reflection;
//using System.Collections;
//using System.Diagnostics;
//using System.Globalization;
//using System.ComponentModel;


namespace lib
{
	//Old, use 2 now.
	class XmlFormatter : IFormatter
	{
		StreamingContext m_context;
		//SerializationMode m_mode;
		//KnownTypeCollection known_types;
		//IDataContractSurrogate m_surrogate;
		//int m_maxItems;

		public XmlFormatter()
		{
		}

		/*
		public XmlFormatter( SerializationMode mode )
		{
			m_mode = mode;
		}

		public XmlFormatter( StreamingContext context )
		{
			m_context = context;
		}

		public XmlFormatter( SerializationMode mode,
			StreamingContext context )
		{
			m_mode = mode;
			m_context = context;
		}
		*/

		//public XmlFormatter (SerializationMode mode,
		//	StreamingContext context, KnownTypeCollection knownTypes)
		//{
		//}

		SerializationBinder IFormatter.Binder
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		ISurrogateSelector IFormatter.SurrogateSelector
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		public StreamingContext Context
		{
			get { return m_context; }
			set { m_context = value; }
		}


		/*
		public KnownTypeCollection KnownTypes {
			get { return known_types; }
		}

		public int MaxItemsInObjectGraph {
			get { return m_maxItems; }
			set { m_maxItems= value; }
		}
		*/


		object IFormatter.Deserialize( Stream stream )
		{
			return Deserialize( stream, null );
		}

		public object Deserialize( Stream stream, Type type )
		{
			XmlTextReader reader = new XmlTextReader( stream );

			return Deserialize( reader, type );
		}

		public object Deserialize( XmlReader reader, Type type )
		{
			return Deserialize( reader, type, false );
		}

		public object Deserialize( XmlReader reader, Type type, bool readContentOnly )
		{
			reader.Read();

			XmlDocument doc = new XmlDocument();

			doc.Load( reader );

			return Deserialize( doc.DocumentElement );
		}

		ConstructorInfo getNoParamCons( ConstructorInfo[] ciArr )
		{
			foreach( ConstructorInfo ci in ciArr )
			{
				if( ci.GetParameters().Length == 0 )
				{
					return ci;
				}
			}

			return null;
		}

		private static FormatterConverter s_conv = new FormatterConverter();

		private Dictionary<int, object> m_alreadySerialized = new Dictionary<int, object>();

		public object Deserialize( XmlElement elem )
		{
			string strType = elem.GetAttribute( "t" );

			return Deserialize( elem, strType );
		}

		public object Deserialize( XmlElement elem, string strType )
		{
			Type type = Type.GetType( strType );

			MemberInfo[] miArr = FormatterServices.GetSerializableMembers( type );

			object obj = Activator.CreateInstance( type );


			/*
			object obj = FormatterServices.GetUninitializedObject( type );


			ConstructorInfo[] ciArr = obj.GetType().GetConstructors();

			ConstructorInfo ci = getNoParamCons( ciArr );

			if( ci == null )
				return null;

			obj = ci.Invoke( null );
			*/

			for( int i = 0; i < miArr.Length; ++i )
			{
				FieldInfo fi = (FieldInfo)miArr[ i ];

				XmlNodeList nodeList = elem.GetElementsByTagName( fi.Name );

				if( nodeList.Count == 1 )
				{
					Type t = fi.FieldType;

					TypeCode tc = Type.GetTypeCode( t );

					XmlElement child = (XmlElement)nodeList[ 0 ];

					object childObj = null;

					if( tc != TypeCode.Object || fi.FieldType.FullName == "System.String" )
					{
						childObj = s_conv.Convert( child.GetAttribute( "v" ), fi.FieldType );
					}
					else
					{
						if( !t.IsArray )
						{
							string refStr = child.GetAttribute( "ref" );
							int refInt = Convert.ToInt32( refStr );

							if( child.HasAttribute( "t" ) )
							{
								childObj = Deserialize( child );

								m_alreadySerialized[refInt] = childObj;
							}
							else
							{
								childObj = m_alreadySerialized[refInt];
							}
						}
						else
						{
							//FormatterServices.GetUninitializedObject()

							int length = s_conv.ToInt32( child.GetAttribute( "c" ) );

							string elemType = child.GetAttribute( "t" );

							Array arr = Array.CreateInstance( t.GetElementType(), length );

							XmlNodeList arrNodeList = child.ChildNodes;

							for( int iElems = 0; iElems < arr.Length; ++iElems )
							{
								XmlElement arrElem = (XmlElement)arrNodeList.Item( iElems );

								arr.SetValue( Deserialize( arrElem, elemType ), iElems );
							}
						}
					}

					fi.SetValue( obj, childObj );
				}
				else
				{
					if( nodeList.Count == 0 )
					{
						// Should be 

						//object obj2 = fi.GetRawConstantValue();
					}
					else //More than 1.
					{
						//Log.error( "Too many fields named the same thing" );
					}
				}
			}

			//FieldInfo fi = (FieldInfo)miArr[0];

			//ConstructorInfo ci = fi.FieldType.TypeInitializer;

			//ci.Invoke( null );

			return obj;
		}


		/*
		public T Deserialize<T> (Stream stream)
		{
			return (T) Deserialize (XmlReader.Create (stream), typeof (T));
		}

		public T Deserialize<T> (XmlReader reader)
		{
			return (T) Deserialize (reader, typeof (T), false);
		}

		public T Deserialize<T> (XmlReader reader, bool readContentOnly)
		{
			return (T) Deserialize (reader, typeof (T), readContentOnly);
		}
		*/

		public void Serialize( Stream stream, object graph )
		{
			/*
			XmlWriterSettings settings = new XmlWriterSettings();

			settings.Indent = true;

			Serialize( XmlWriter.Create( stream, settings ), graph );
			*/

			XmlTextWriter writer = new XmlTextWriter( stream, null );

			writer.Formatting = Formatting.Indented;

			Serialize( writer, graph );

			writer.Close();
		}

		public void Serialize( XmlWriter writer, object graph )
		{
			Serialize( writer, graph, null, true, false, true );
		}

		public void Serialize( XmlWriter writer, object graph,
			Type rootType, bool preserveObjectReferences,
			bool writeContentOnly,
			bool ignoreUnknownSerializationData )
		{
			Type t = graph.GetType();

			//writer.WriteStartDocument();

			if( Type.GetTypeCode( t ) == TypeCode.Object )
			{
				writer.WriteStartElement( "root" );

				Assembly assem = t.Assembly;

				string assemName = assem.GetName().Name;

				writer.WriteAttributeString( "t", graph.GetType().FullName + ", " + assemName );

				FieldInfo[] fiArr  = t.GetFields( BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly );

				foreach( FieldInfo fi in fiArr )
				{
					Serialize( writer, fi.Name, fi.GetValue( graph ) );

					/*
					if( fi.FieldType.IsClass )
					{
						Serialize( writer, fi.GetValue( graph ), rootType, preserveObjectReferences, writeContentOnly, ignoreUnknownSerializationData );
					}
					else
					{
						SerializePod( writer, fi.GetValue( graph ) );
					}
					*/
				}

				writer.WriteEndElement();
			}

			//writer.WriteEndDocument();
		}

		private ObjectIDGenerator m_idGenerator = new ObjectIDGenerator();
		private Dictionary<long, object> m_alreadyDeserialzied = new Dictionary<long, object>();

		public void Serialize( XmlWriter writer, string name, object obj )
		{
			writer.WriteStartElement( name );

			if( obj != null )
			{
				Type t = obj.GetType();

				if( Type.GetTypeCode( t ) == TypeCode.Object )
				{
					bool first = false;
					if( !m_alreadyDeserialzied.ContainsKey( m_idGenerator.GetId( obj, out first ) ) )
					{
						m_alreadyDeserialzied[m_idGenerator.GetId( obj, out first )] = obj;

						Assembly assem = t.Assembly;

						string assemName = assem.GetName().Name;

						if( !t.IsArray )
						{
							writer.WriteAttributeString( "t", t.FullName + ", " + assemName );

							writer.WriteAttributeString( "ref", m_idGenerator.GetId( obj, out first ).ToString() );

							FieldInfo[] fiArr  = t.GetFields( BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public );

							foreach( FieldInfo fi in fiArr )
							{
								Serialize( writer, fi.Name, fi.GetValue( obj ) );
							}
						}
						else
						{
							Array arr = (Array)obj;

							Type aType = t.GetElementType();

							string aTypeString = aType.FullName;

							writer.WriteAttributeString( "t", aTypeString + ", " + assemName );

							writer.WriteAttributeString( "c", arr.Length.ToString() );

							for( int i = 0; i < arr.Length; ++i )
							{
								Serialize( writer, "val", arr.GetValue( i ) );
							}

							//writer.WriteStartElement( "values" );



							//writer.WriteEndElement();

						}
					}
					else
					{
						writer.WriteAttributeString( "ref", m_idGenerator.GetId( obj, out first ).ToString() );
					}
				}
				else
				{
					writer.WriteAttributeString( "t", t.FullName );

					writer.WriteAttributeString( "v", obj.ToString() );
				}
			}
			else
			{
				writer.WriteAttributeString( "null", "" );
			}

			writer.WriteEndElement();
		}

	}
}


















/*
/// <summary>
/// 
/// </summary>
public class XmlFormatter : IFormatter
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


	public XmlFormatter()
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
	public void Serialize( System.IO.Stream stream, object obj )
	{
		//Default is 4k
		//BufferedStream bufStream = new BufferedStream( stream );

		TextWriter writer = new StreamWriter( stream );

		writeObject( writer, obj );

		while( m_objectsToBeDeserialized.Count != 0 )
		{
			object objToDes = m_objectsToBeDeserialized.Dequeue();

			writeObject( writer, objToDes );
		}

		writer.Write( (char)ETypes.EndStream );
	}

	void writeRefAndSched( TextWriter writer, object obj )
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

		if( m_alreadyDeserialzied[ obj ] == null )
		{
			m_alreadyDeserialzied[ obj ] = obj;
			m_objectsToBeDeserialized.Enqueue( obj );
		}
	}

	void dispatchWrite( TextWriter writer, object parentObj, FieldInfo fi )
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

			writer.Write( Convert.ToInt32( fi.GetValue( parentObj ) ) );
		}
		else
		{
			switch( typeName )
			{
				case "Int32":
					writer.Write( (char)ETypes.Int32 );
					writer.Write( name.GetHashCode() );

					writer.Write( Convert.ToInt32( fi.GetValue( parentObj ) ) );
				break;
				case "Single":
					writer.Write( (char)ETypes.Single );
					writer.Write( name.GetHashCode() );

					writer.Write( Convert.ToSingle( fi.GetValue( parentObj ) ) );
					break;					
				case "Double":
					writer.Write( (char)ETypes.Double );
					writer.Write( name.GetHashCode() );

					writer.Write( Convert.ToDouble( fi.GetValue( parentObj ) ) );
					break;					
				case "Char":
					writer.Write( (char)ETypes.Char );
					writer.Write( name.GetHashCode() );

					writer.Write( Convert.ToChar( fi.GetValue( parentObj ) ) );
					break;					
				case "String":
					writer.Write( (char)ETypes.String );
					writer.Write( name.GetHashCode() );

					writer.Write( Convert.ToString( fi.GetValue( parentObj ) ) );
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

	void writeArray( TextWriter writer, Array array )
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


	void writeObject( TextWriter writer, object obj )
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

	void write<TType>( TextWriter wr, TType val )
	{
		//wr.Write( val );
	}

	/*
	void writeInt( TextWriter writer, int val )
	{
		writer.Write( val );
	}

	void writeSingle( TextWriter writer, float val )
	{
		writer.Write( val );
	}

	void writeDouble( TextWriter writer, double val )
	{
		writer.Write( val );
	}

	void writeChar( TextWriter writer, char val )
	{
		writer.Write( val );
	}

	void writeString( TextWriter writer, string val )
	{
		writer.Write( val );
	}

	void writeBool( TextWriter writer, bool val )
	{
		writer.Write( val );
	}
	* /
	#endregion Serialize


	#region Deserialize

	class Fixup
	{
		public Fixup( int guid, object obj, FieldInfo fi )
		{
			m_guid= guid;
			m_obj = obj;
			m_fi  = fi;
		}

		XmlFormatter

		public Fixup( int guid, object obj, int index )
		{
			m_guid = guid;
			m_obj  = obj;
			m_index= index;
		}

		public readonly int       m_guid = 0;
		public readonly object    m_obj  = null;

		public readonly FieldInfo m_fi   = null;
		public readonly int       m_index= -1;

	}

	Hashtable m_mapGUIDToObject = new Hashtable();
	ArrayList m_fixupList       = new ArrayList();

	ArrayList m_desObjects = new ArrayList();

	public object Deserialize( System.IO.Stream stream )
	{
		StreamReader reader = new StreamReader( stream );

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

					array[ fu.m_index ] = obj;
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



	bool dispatchRead( StreamReader reader, object obj, Hashtable ht )
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


	object readObject( StreamReader reader )
	{
		//ETypes type = (ETypes)reader.ReadChar();

		//Debug.Assert( type == ETypes.Object, "Expecting type Object" );

		string objTypeName = reader.ReadString();
		int    objGUID = reader.ReadInt32();

		try
		{
			object obj = createObject( objTypeName );

			m_mapGUIDToObject[ objGUID ] = obj;

			ArrayList list = new ArrayList();
			Hashtable ht   = new Hashtable();

			if( obj != null )
			{
				getAllFields( obj, list );

				foreach( FieldInfo fi in list )
				{
					ht[ fi.Name.GetHashCode() ] = fi;
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

	void readArray( StreamReader reader, object obj, FieldInfo fi )
	{
		int length = reader.ReadInt32();

		if( length < 0 )
		{
			if( fi == null ) return;

			fi.SetValue( obj, null );

			return;
		}

		object[] array = new object[length];

		if( fi != null )
		{
			fi.SetValue( obj, array );
		}

		for( int i=0; i<length; ++i )
		{
			int val = reader.ReadInt32();

			//m_fixups[ val ] = new Fixup( obj, fi );

			if( fi != null )
			{
				m_fixupList.Add( new Fixup( val, array, i ) );
			}
		}
	}

	void readRef( StreamReader reader, object obj, FieldInfo fi )
	{
		int val = reader.ReadInt32();

		//m_fixups[ val ] = new Fixup( obj, fi );

		m_fixupList.Add( new Fixup( val, obj, fi ) );
	}

	void readInt( StreamReader reader, object obj, FieldInfo fi )
	{
		int val = reader.ReadInt32();

		if( fi == null ) return;

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

	void readSingle( StreamReader reader, object obj, FieldInfo fi )
	{
		float val = reader.ReadSingle();

		if( fi == null ) return;

		fi.SetValue( obj, val );
	}

	void readDouble( StreamReader reader, object obj, FieldInfo fi )
	{
		double val = reader.ReadDouble();

		if( fi == null ) return;

		fi.SetValue( obj, val );
	}

	void readChar( StreamReader reader, object obj, FieldInfo fi )
	{
		char val = reader.ReadChar();

		if( fi == null ) return;

		fi.SetValue( obj, val );
	}

	void readString( StreamReader reader, object obj, FieldInfo fi )
	{
		string val = reader.ReadString();

		if( fi == null ) return;

		fi.SetValue( obj, val );
	}

	void readBool( StreamReader reader, object obj, FieldInfo fi )
	{
		bool val = reader.ReadBoolean();

		if( fi == null ) return;

		fi.SetValue( obj, val );
	}

	#endregion Deserialize


}
*/
//}



















