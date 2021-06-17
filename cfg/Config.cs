using System;
using System.IO;
using System.Xml;
using System.Reflection;

namespace lib
{

	public class DescAttribute: Attribute
	{
		public string Desc { get; private set; }

		public DescAttribute( string desc )
		{
			Desc = desc;
		}
	}

	[Serializable]
	public class ConfigCfg: Config
	{
		public readonly bool writeOutTemplateFiles = true;
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

		static ConfigCfg s_cfg = new ConfigCfg();

		static public void startup( string filename )
		{
			res.Mgr.register<Config>( load );
			res.Mgr.registerSub( typeof( Config ) );

			s_cfg = Config.load<ConfigCfg>( filename );

		}


		#region SaveLoad
		/*
		static public res.Ref<Config> res_load( string filename )
		{
			return new res.Ref<Config>( filename, load( filename ) );
		}
		*/

		static public T res_load<T>( string filename ) where T : Config
		{
			return load<T>( filename );
		}

		/*
		static public ResRefConfig res_load( string filename, Type t )
		{
			return new ResRefConfig( filename, load( filename, t ) );
		}
		*/


		static public Config load( string filename )
		{
			return load( filename, null );
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
				FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);

				XmlFormatter2 formatter = new XmlFormatter2();

				cfg = (Config)( t != null ? formatter.DeserializeKnownType( fs, t ) : formatter.Deserialize( fs ) );

				cfg.SetFilename( filename );
			}
			catch( FileNotFoundException )
			{
				Type[] types = new Type[0];
				object[] parms = new object[0];

				//types[ 0 ] = typeof( string );
				//parms[ 0 ] = filename;

				ConstructorInfo cons = t?.GetConstructor(types);

				try
				{
					cfg = (Config)cons?.Invoke( parms );
				}
				catch( Exception e )
				{
					log.error( $"Exception while creating config {t.ToString()}, Msg {e.Message}" );
				}

				//cfg.SetFilename( filename );

				if( s_cfg.writeOutTemplateFiles )
				{
					var templateFile = $"templates/{filename}";

					var dirName = Path.GetDirectoryName(templateFile);

					lib.Util.checkAndAddDirectory( dirName );

					log.info( $"Writing out template config of type {t?.Name} in {templateFile}" );

					Config.save( cfg, templateFile );
				}
			}

			return cfg;
		}

		static public void save( Config cfg )
		{
			Config.save( cfg, cfg.m_filename );
		}

		static public void save( Config cfg, String filename )
		{
			FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write);

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

