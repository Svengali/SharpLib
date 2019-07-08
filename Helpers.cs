
/*
 * TODO: Need to verify types are correct when deserializing.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using System.Xml;
using System.Xml.Serialization;

using System.IO;

namespace lib
{
	public class Helpers
	{
		public void XmlSave( String filename, Object obj )
		{
			FileStream fs = new FileStream( filename, FileMode.Create, FileAccess.Write );

			XmlSerializer xs = new XmlSerializer( obj.GetType() );
			//MemoryStream memoryStream = new MemoryStream( StringToUTF8ByteArray( pXmlizedString ) );
			//XmlTextReader reader = new XmlTextReader( fs, Encoding.UTF8 );

			xs.Serialize( fs, obj );
		}

		public Object XmlLoad<TType>( String filename )
		{
			FileStream fs = new FileStream( filename, FileMode.Open, FileAccess.Read );

			XmlSerializer xs = new XmlSerializer( typeof( TType ) );
			//MemoryStream memoryStream = new MemoryStream( StringToUTF8ByteArray( pXmlizedString ) );
			//XmlTextReader reader = new XmlTextReader( fs, Encoding.UTF8 );

			return xs.Deserialize( fs );
		}


		static public MethodInfo FindConvertFunction( string typeName )
		{
			Type t = typeof( Convert );


			MethodInfo[] allMethods = t.GetMethods();

			foreach( MethodInfo mi in allMethods )
			{
				if( mi.GetParameters().Length == 1 )
				{
					string paramName = mi.GetParameters()[ 0 ].ParameterType.Name;

					if( paramName == "String" )
					{
						if( mi.ReturnType.FullName == typeName )
						{
							return mi;
						}
					}
				}
			}

			return null;

		}

		static public void SerializeDict<TKey, TVal>( string filename, Dictionary<TKey, TVal> dict )
		{
			XmlTextWriter xmlWriter = new XmlTextWriter( filename, null );

			xmlWriter.Formatting = Formatting.Indented;

			//xmlWriter.WriteStartDocument();

			xmlWriter.WriteStartElement( "dictionary" );

			Type[] types = dict.GetType().GetGenericArguments();

			xmlWriter.WriteAttributeString( "keyType", types[ 0 ].FullName );
			xmlWriter.WriteAttributeString( "valType", types[ 1 ].FullName );

			foreach( KeyValuePair<TKey, TVal> kvp in dict )
			{
				xmlWriter.WriteStartElement( "kvp" );

				xmlWriter.WriteAttributeString( "key", kvp.Key.ToString() );
				xmlWriter.WriteAttributeString( "value", kvp.Value.ToString() );

				xmlWriter.WriteEndElement();
			}

			xmlWriter.WriteEndElement();

			//xmlWriter.WriteEndDocument();

			xmlWriter.Close();
		}

		static public void DeserializeDict<TKey, TVal>( string filename, Dictionary<TKey, TVal> dict )
		{
			FileStream fs = new FileStream( filename, FileMode.Open, FileAccess.Read );

			XmlDocument doc = new XmlDocument();

			doc.Load( fs );

			//CreateTypeFor()

			XmlElement docElem = doc.DocumentElement;

			if( docElem.Name == "dictionary" )
			{
				string keyType = docElem.GetAttribute( "keyType" );
				string valType = docElem.GetAttribute( "valType" );

				MethodInfo keyMI = FindConvertFunction( keyType );
				MethodInfo valMI = FindConvertFunction( valType );


				if( keyMI != null && valMI != null )
				{
					XmlNodeList nodeList = docElem.ChildNodes;

					object[] args = new object[ 1 ];

					//fi.SetValue( newObj, obj );

					foreach( XmlElement node in nodeList )
					{
						if( node.Name == "kvp" )
						{
							if( node.Attributes != null )
							{
								args[ 0 ] = node.GetAttribute( "key" );

								TKey key = (TKey)keyMI.Invoke( null, args );

								args[ 0 ] = node.GetAttribute( "value" );

								TVal val = (TVal)valMI.Invoke( null, args );

								dict[ key ] = val;
							}
							else
							{
								Log.error( String.Format( $"No attributes in node while loading file {filename}" ) );
							}
						}
						else
						{
							Log.error( String.Format( $"Incorrect key {node.Name} found while loading file {filename}" ) );
						}
					}
				}
				else
				{
					if( keyMI == null )
						Log.error( String.Format( $"Key type conversion not found for type {keyType}" ) );

					if( valMI == null )
						Log.error( String.Format( $"Val type conversion not found for type {valType}" ) );		
				}

			}
			else
			{
				Log.error( String.Format( $"No dictionary element found while loading file {filename}" ) );
			}
		}

	}



}
