using System;
using System.IO;
using System.Xml;
using System.Runtime.Serialization;
using System.Web.Configuration;
using System.Collections;
using System.Collections.Generic;


using System.Reflection;
using System.Diagnostics;

using System.Runtime.InteropServices;

namespace lib
{

 public interface I_Serialize
{
	void OnSerialize();
	void OnDeserialize( object enclosing );
}


public class XmlFormatter2 : IFormatter
{
	public StreamingContext Context { get; set; }

	static private Random s_rnd = new Random();
	private int m_rndVal = s_rnd.Next();
		
	#region Unimplimented
	public ISurrogateSelector SurrogateSelector
	{
		get { throw new NotImplementedException(); }
		set { throw new NotImplementedException(); }
	}

	public SerializationBinder Binder
	{
		get { throw new NotImplementedException(); }
		set { throw new NotImplementedException(); }
	}
	#endregion


	public XmlFormatter2()
	{
		Context = new StreamingContext( StreamingContextStates.All );
	}



	#region Deserialize
	private static FormatterConverter s_conv = new FormatterConverter();

	public object Deserialize( Stream stream )
	{
		//lib.log.info( "Deserialize( Stream stream ) {0} {1}", m_rndVal, m_alreadySerialized.Count );
		return DeserializeKnownType( stream, null );
		//lib.log.info( "Deserialize END ( Stream stream ) {0} {1}", m_rndVal, m_alreadySerialized.Count );
	}

	public object DeserializeKnownType( Stream stream, Type t )
	{
		//lib.log.info( "DeserializeKnownType( Stream stream, Type t ) {0} {1}", m_rndVal, m_alreadySerialized.Count );

		XmlTextReader reader = new XmlTextReader( stream );
		//reader.Settings = System.Text.Encoding.ASCII;

		object obj = Deserialize( reader, t );
		//lib.log.info( "DeserializeKnownType END( Stream stream, Type t ) {0} {1}", m_rndVal, m_alreadySerialized.Count );
		return obj;
	}

	private object Deserialize( XmlReader reader, Type t )
	{
		//lib.log.info( "Deserialize( XmlReader reader, Type t ) {0} {1}", m_rndVal, m_alreadySerialized.Count );

		m_alreadySerialized.Clear();
		m_objectID = new ObjectIDGenerator();

		reader.Read();

		XmlDocument doc = new XmlDocument();

		doc.Load( reader );

		////lib.log.info( "What to deserialize {0}", doc.OuterXml.ToString() );

		if( t == null ) return Deserialize( doc.DocumentElement );

		return Deserialize( doc.DocumentElement, t );
	}

	private object Deserialize( XmlElement elem )
	{
		//lib.log.info( "object Deserialize( XmlElement elem ) {0} {1}", m_rndVal, m_alreadySerialized.Count );

		string typename = elem.HasAttribute( "t" ) ? elem.GetAttribute( "t" ) : elem.Name;

		return Deserialize( elem, typename );
	}

	private object Deserialize( XmlElement elem, string typename )
	{
		AppDomain currentDomain = AppDomain.CurrentDomain;
		Assembly[] assems = currentDomain.GetAssemblies();

		Type type = null;

		// @@@@:	This should go backwards, we tend to lookup our own stuff, then builtins.  
		//			Also, cache a typename into its assembly.  
		foreach(Assembly a in assems)
		{
			type = a.GetType( typename );

			if(type != null) break;
		}

		if( type == null )
		{
				return null;
		}

		return Deserialize( elem, type );
	}

	private object Deserialize( XmlElement elem, Type type, object enclosing = null )
	{
		TypeCode typeCode = Type.GetTypeCode( type );

		if( typeCode != TypeCode.Object )
		{
			return DeserializeConcrete( elem, type );
		}
		else
		{
			if( !type.IsArray )
			{
				object obj = DeserializeObject( elem, type );

				if( obj is I_Serialize )
				{
					var iser = obj as I_Serialize;

					iser.OnDeserialize( enclosing );
				}

				return obj;
			}
			else
			{
				return DeserializeArray( elem, type );
			}
		}
	}

	Type[] mm_types = new Type[1];
	private object GetDefault( Type t )
	{
		mm_types[0] = t;

		var fn = GetType().GetMethod( "GetDefaultGeneric" ).MakeGenericMethod( mm_types );

		return fn.Invoke( this, null );
	}

	public T GetDefaultGeneric<T>()
	{
		return default( T );
	}

	private object DeserializeConcrete( XmlElement elem, Type type )
	{
		string val = elem.GetAttribute( "v" );

		if( !type.IsEnum )
		{
			try
			{
				return s_conv.Convert( val, type );
			}
			catch( Exception )
			{
				return GetDefault( type );
			}
		}
		else
		{
			return Enum.Parse( type, val );
		}

	}

	private XmlElement getNamedChild( XmlNodeList list, string name )
	{
		foreach( XmlNode node in list )
		{
			if( node.Name == name )
			{
				return (XmlElement)node;
			}
		}

		return null;
	}

	private Type FindType( string shortname )
	{
		Assembly[] ass = AppDomain.CurrentDomain.GetAssemblies();

		foreach( Assembly a in ass )
		{
			Type t = a.GetType( shortname );

			if( t != null )
			{
				return t;
			}
		}

		return null;
	}

	private Type[] mm_consType = new Type[ 2 ];
	private object[] mm_args = new object[ 2 ];
	private object DeserializeObject( XmlElement elem, Type type )
	{
		string refString = elem.GetAttribute( "ref" );

		int refInt = refString.Length > 0 ? Convert.ToInt32( refString ) : -1;

		var finalType = type;
		if( elem.HasAttribute( "t" ) )
		{
			var typename = elem.GetAttribute( "t" );
			finalType = FindType( typename );

			if( finalType == null ) finalType = type;
		}

		object obj = createObject( finalType, refInt );

		if( obj is IList )
		{
			var list = obj as IList;

			return DeserializeList( elem, type, list );
		}

		Type typeISerializable = typeof( ISerializable );

		if( obj is ISerializable ) //   type.IsSubclassOf( typeISerializable ) )
		{
			XmlNodeList allChildren = elem.ChildNodes;

			//ISerializable ser = obj as ISerializable;

			var serInfo = new SerializationInfo( finalType, new FormatterConverter() );

			//var serInfoForTypes = new SerializationInfo( type, new FormatterConverter() );

			//ser.GetObjectData( serInfoForTypes, Context );

			foreach( var objNode in allChildren )
			{
				var node = objNode as XmlElement;

				String name = node.Name;

				String childType = node.GetAttribute( "t" );

				name = scr.TypeToIdentifier( name );

				XmlElement childElem = getNamedChild( allChildren, name );

				var des = Deserialize( childElem, childType );

				serInfo.AddValue( name, des, des.GetType() );
			}

			//ConstructorInfo[] allCons = obj.GetType().GetConstructors( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );

			//var serMem = FormatterServices.GetSerializableMembers( finalType );

			//object objUn = FormatterServices.GetSafeUninitializedObject( finalType );

			IDeserializationCallback objUnOnDeser = obj as IDeserializationCallback;

			mm_consType[ 0 ] = typeof( SerializationInfo );
			mm_consType[ 1 ] = typeof( StreamingContext );
			ConstructorInfo serCons = finalType.GetConstructor( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, mm_consType, null );

			mm_args[0] = serInfo;
			mm_args[1] = Context;
			serCons.Invoke( obj, mm_args );

			if( objUnOnDeser != null )
			{
				objUnOnDeser.OnDeserialization( objUnOnDeser );
			}

				/*
				ser.GetObjectData( serInfo, Context );

				//var serEnum = ;

				foreach( var serMember in serInfo )
				{
					String name = serMember.Name;

					name = scr.TypeToIdentifier( name );

					XmlElement childElem = getNamedChild( allChildren, name );

					var des = Deserialize( childElem, name );
				}
				*/
			}
			else
		{
			XmlNodeList allChildren = elem.ChildNodes;

				var fields = scr.GetAllFields( type );

				//MemberInfo[] miArr = FormatterServices.GetSerializableMembers( type, Context );

				foreach( var childFi in fields )
				{ 

					String name = childFi.Name;

					name = scr.TypeToIdentifier( name );

					XmlElement childElem = getNamedChild( allChildren, name );


				if( childElem != null )
				{
					object childObj = Deserialize( childElem, childFi.FieldType, obj );

					childFi.SetValue( obj, childObj );
				}
				else if( fields.Count == 1 )
				{
					object childObj = Deserialize( elem, childFi.FieldType, obj );

					childFi.SetValue( obj, childObj );
				}


				}
		}

		return obj;
	}

	private object DeserializeList( XmlElement elem, Type type, IList list )
	{
		XmlNodeList arrNodeList = elem.ChildNodes;

		Type t = list.GetType();

		Type[] genT = t.GetGenericArguments();

		Debug.Assert( genT.Length == 1 );

		for( int i = 0; i < arrNodeList.Count; ++i )
		{
			if( arrNodeList.Item( i ) is XmlElement )
			{
				XmlElement arrElem = (XmlElement)arrNodeList.Item( i );

				list.Add( Deserialize( arrElem, genT[0] ) );
			}
		}

		return list;
	}

	private object DeserializeArray( XmlElement elem, Type type )
	{
		Type typeElem = type.GetElementType();

		string refString = elem.GetAttribute( "ref" );
		int refInt = refString.Length > 0 ? Convert.ToInt32( refString ) : -1;

		XmlNodeList arrNodeList = elem.ChildNodes;

		int length = arrNodeList.Count;

		Array arr = createArray( typeElem, refInt, length );

		for( int i = 0; i < arr.Length; ++i )
		{
			if( arrNodeList.Item( i ) is XmlElement )
			{
				XmlElement arrElem = (XmlElement)arrNodeList.Item( i );

				arr.SetValue( Deserialize( arrElem, typeElem ), i );
			}
		}

		return arr;
	}

	private object createObject( string typename, int refInt )
	{
		Type type = Type.GetType( typename );

		return createObject( type, refInt );
	}

	private object createObject( Type type, int refInt )
	{
		TypeCode tc = Type.GetTypeCode( type );

		if( refInt > 0 && m_alreadySerialized.ContainsKey( refInt ) )
		{
			//lib.log.info( "Reusing object for {0}", refInt );
			return m_alreadySerialized[ refInt ];
		}
		else
		{
			//lib.log.info( "Creating new object for {0}", refInt );
			object obj = Activator.CreateInstance( type );

			if( refInt > 0 )
			{
				m_alreadySerialized[ refInt ] = obj;
			}

			return obj;
		}
	}

	private Array createArray( string elemTypename, int refInt, int length )
	{
		Type elemType = Type.GetType( elemTypename );

		return createArray( elemType, refInt, length );
	}

	private Array createArray( Type elemType, int refInt, int length )
	{
		TypeCode elemTC = Type.GetTypeCode( elemType );

		if( refInt > 0 && m_alreadySerialized.ContainsKey( refInt ) )
		{
			return (Array)m_alreadySerialized[ refInt ];
		}
		else
		{
			Array arr = Array.CreateInstance( elemType, length ) ;

			m_alreadySerialized[ refInt ] = arr;

			return arr;
		}
	}

	private ObjectIDGenerator m_objectID = new ObjectIDGenerator();
	private Dictionary<long, object> m_alreadySerialized = new Dictionary<long, object>();

	#endregion

	#region Serialize

	private string getTypeName( Type type )
	{
		//Assembly ass = type.Assembly;

		//string assName = ass.GetName().Name;

		return type.FullName; // + ", " + assName;
	}

	public void Serialize( Stream stream, object root )
	{
		//lib.log.info( "Serialize( Stream stream, object root ) {0} {1}", m_rndVal, m_alreadySerialized.Count );

		m_alreadySerialized.Clear();
		m_objectID = new ObjectIDGenerator();

		XmlTextWriter writer = new XmlTextWriter( stream, System.Text.Encoding.ASCII );

		writer.Formatting = Formatting.Indented;

		Serialize( writer, root );

		//Rely on the parent closing the stream.  
		//writer.Close();
		writer.Flush();

		//lib.log.info( "Serialize END ( Stream stream, object root ) {0} {1}", m_rndVal, m_alreadySerialized.Count );
	}

	private void Serialize( XmlWriter writer, object root )
	{
		//writer.WriteStartDocument();
		Serialize( writer, root, "root", true );
		//writer.WriteEndDocument();
	}

	private void Serialize( XmlWriter writer, object root, string name, bool forceType )
	{
		writer.WriteStartElement( name );

		if( root != null )
		{
			Type type = root.GetType();

			TypeCode typeCode = Type.GetTypeCode( type );

			if( typeCode != TypeCode.Object )
			{
				SerializeConcrete( writer, root, forceType );
			}
			else
			{
				if( !type.IsArray )
				{
					SerializeObject( writer, root );
				}
				else
				{
					SerializeArray( writer, root );
				}
			}
		}
		else
		{
			writer.WriteAttributeString( "v", "null" );
		}

		writer.WriteEndElement();
	}

	private void SerializeConcrete( XmlWriter writer, object root, bool forceType )
	{
		//TODO: Only write this out if debugging.
		if( forceType )
		{
			writer.WriteAttributeString( "t", getTypeName( root.GetType() ) );
		}
		writer.WriteAttributeString( "v", root.ToString() );
	}

	private void SerializeObject( XmlWriter writer, object root )
	{
		writer.WriteAttributeString( "t", getTypeName( root.GetType() ) );

		/*
		if( root is IList )
		{
			var list = root as IList;

			Type t = root.GetType();

			Type[] genT = t.GetGenericArguments();
		}
		*/

		bool first;

		long refInt = m_objectID.GetId( root, out first );

		writer.WriteAttributeString( "ref", refInt.ToString() );

		if( first )
		{
			m_alreadySerialized[ refInt ] = root;

			Type type = root.GetType();

			//*
			Type typeISerializable = typeof( ISerializable );

			if( root is ISerializable ) //   type.IsSubclassOf( typeISerializable ) )
			{
				ISerializable ser = root as ISerializable;

				var serInfo = new SerializationInfo( type, new FormatterConverter() );

				ser.GetObjectData( serInfo, Context );

				//var serEnum = ;

				foreach( var serMember in serInfo )
				{
					String name = serMember.Name;

						name = scr.TypeToIdentifier( name );

						Serialize( writer, serMember.Value, name, true );
				}

				//var sc = new SerializationContext( 

				//ser.GetObjectData( 
			}
			else
			//*/
			{
				var fields = scr.GetAllFields( type );

				//MemberInfo[] miArr = FormatterServices.GetSerializableMembers( type, Context );

				foreach( var childFi in fields )
				{

					object[] objs = childFi.GetCustomAttributes( typeof( NonSerializedAttribute ), true );

					if( objs.Length > 0 )
					{
						continue;
					}

					String name = childFi.Name;

						name = scr.TypeToIdentifier( name );

						Serialize( writer, childFi.GetValue( root ), name, false );
				}
			}
		}
	}

	private void SerializeArray( XmlWriter writer, object root )
	{
		Array arr = (Array)root;

		Type typeElem = arr.GetType().GetElementType();

		Type type = root.GetType();

		writer.WriteAttributeString( "t", getTypeName( type ) );

		bool first;

		long refInt = m_objectID.GetId( root, out first );

		writer.WriteAttributeString( "ref", refInt.ToString() );

		if( first )
		{
			m_alreadySerialized[ refInt ] = root;


			for( int i = 0; i < arr.Length; ++i )
			{
				Serialize( writer, arr.GetValue( i ), "i" + i.ToString(), false );
			}
		}
	}
	#endregion
}

}
