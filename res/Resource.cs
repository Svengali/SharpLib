using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;

namespace res
{

[Serializable]
public class Ref : lib.I_Serialize
{
	public string filename { get { return m_filename; } }

	//For construction
	public Ref()
	{
	}

	public Ref( string filename )
	{
		m_filename = filename;
	}

	virtual public void OnSerialize()
	{
	}

	virtual public void OnDeserialize( object enclosing )
	{
	}

	virtual public void OnChange()
	{
	}

	private string m_filename;
}
		
[Serializable]
public class Ref<T> : Ref
{
	public T res{ get{ return m_res; } set{ m_res = value; } }

	//For construction
	public Ref()
	{
	}

	public Ref( string filename )
		: base( filename )
	{
	}

	public Ref( string filename, T res ) : base( filename )
	{
		m_res = res;
	}

  [NonSerialized]
	private T m_res;
}




public class Resource
{
	static public Mgr mgr;

}


/*
public class Loader<T>
{
	static public T load( string filename )
	{
		Debug.Assert( false, "Specialize Loader for your type for file" );
		return default(T);
	}
}
*/
public delegate Ref Load( string filename );
public delegate Ref LoadType( string filename, Type t );


public class Mgr
{


	static public void startup()
	{
		Resource.mgr = new Mgr();
	}

	static public void register<T>( Load loader )
	{
		Debug.Assert( !Resource.mgr.m_loaders.ContainsKey( typeof( T ) ) );
		Resource.mgr.m_loaders[ typeof( T ) ] = loader;
	}

	//Register all subclasses of a particular type
	static public void registerSub<T>( Load loaderOfType )
	{
		
		Type[] typeParams = new Type[1];
		foreach( var mi in typeof( T ).GetMethods() )
		{
			if( mi.Name == "res_load" && mi.IsGenericMethod )
			{
				foreach( var ass in AppDomain.CurrentDomain.GetAssemblies() )
				{
					foreach( var t in ass.GetTypes() )
					{
						if( t.IsSubclassOf( typeof( T ) ) )
						{
							typeParams[0] = t;
							var mi_ng = mi.MakeGenericMethod( typeParams );
							Resource.mgr.m_loaders[ t ] = (Load)Delegate.CreateDelegate( typeof(Load), mi_ng );
						}
					}
				}
				return;				
			}
		}
	}

	static public Ref<T> load<T>( string filename ) where T : class
	{
		Load loader;
		Resource.mgr.m_loaders.TryGetValue( typeof( T ), out loader );

		if( loader != null )
		{
			var rf_raw = loader( filename );
			Ref<T> rf = rf_raw as Ref<T>;
			return rf;
		}

		return new Ref<T>( filename );
	}

	static public Ref load( string filename, Type t )
	{
		Load loader;
		Resource.mgr.m_loaders.TryGetValue( t, out loader );

		if( loader != null )
		{
			var rf_raw = loader( filename );
			return rf_raw;
		}

		return new Ref<object>( filename );
	}

	private Mgr()
	{
		
	}


	private Dictionary<Type, Load> m_loaders = new Dictionary<Type, Load>();
	//private Dictionary<Type, LoadType> m_loadersOfType = new Dictionary<Type, LoadType>();
}





}
