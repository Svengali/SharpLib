using System;
using System.IO;
using System.Xml;
using System.Reflection;

namespace lib
{

public class DescAttribute : Attribute
{
	public string Desc { get; private set; }

	public DescAttribute( string desc )
	{
		Desc = desc;
	}
}



[Serializable]
public class ResRefConfig<T> : res.Ref<T> where T: Config 
{
	public ResRefConfig()
	{
	}

	public ResRefConfig( string filename, T cfg )
		: base( filename, cfg )
	{
	}

	override public void OnDeserialize( object enclosing )
	{
		base.OnDeserialize( enclosing );

		var cfg = Config.load<T>( filename );

		res = cfg;
	}
}

[Serializable]
public class Config
{
	/*
	static public Config Load( string filename )
	{
		return null;
	}
	*/

	static public void startup()
	{
		res.Mgr.register<Config>( res_load );
		res.Mgr.registerSub<Config>( res_load );
	}


	#region SaveLoad
	static public ResRefConfig<Config> res_load( string filename )
	{
		return new ResRefConfig<Config>( filename, load( filename ) );
	}

	static public ResRefConfig<T> res_load<T>( string filename ) where T : Config
	{
		return new ResRefConfig<T>( filename, load<T>( filename ) );
	}

	/*
	static public ResRefConfig res_load( string filename, Type t )
	{
		return new ResRefConfig( filename, load( filename, t ) );
	}
	*/


	static public Config load( string filename )
	{
		FileStream fs = new FileStream( filename, FileMode.Open, FileAccess.Read );

		XmlFormatter2 formatter = new XmlFormatter2();

		Config cfg = (Config)formatter.Deserialize( fs );

		return cfg;
	}

	static public T load<T>( string filename ) where T : Config
	{
		return (T)load( filename, typeof( T ) );
	}

	static public Config load( string filename, Type t )
	{
		Config cfg = null;

		try
		{
			FileStream fs = new FileStream( filename, FileMode.Open, FileAccess.Read );

			XmlFormatter2 formatter = new XmlFormatter2();

			cfg = (Config)( t != null ? formatter.DeserializeKnownType( fs,t ) : formatter.Deserialize( fs ) );

			cfg.SetFilename( filename );
		}
		catch( FileNotFoundException )
		{
			Type[] types = new Type[ 0 ];
			object[] parms = new object[ 0 ];

			//types[ 0 ] = typeof( string );
			//parms[ 0 ] = filename;

			ConstructorInfo cons = t.GetConstructor( types );

			try
			{
				cfg = (Config)cons.Invoke( parms );
			}
			catch( Exception e )
			{
				Log.error( "Caught exception {0}", e );
			}

			cfg.SetFilename( filename );

			Config.save( cfg, filename );
		}

		return cfg;
	}

	static public void save( Config cfg )
	{
		Config.save( cfg, cfg.m_filename );
	}

	static public void save( Config cfg, String filename )
	{
		FileStream fs = new FileStream( filename, FileMode.Create, FileAccess.Write );

		XmlFormatter2 formatter = new XmlFormatter2();

		formatter.Serialize( fs, cfg );

		fs.Close();
	}
	#endregion 

	private string m_filename = "";

	public Config()
	{
	}

	public Config( string filename )
	{
		m_filename = filename;
	}

	public String Filename { get { return m_filename; } }

	protected void SetFilename( String filename ) { m_filename = filename; }

}
}

