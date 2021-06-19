using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using System.Collections.Immutable;
using System.Threading;


namespace res
{

	using ImmDefLoad = ImmutableQueue<(string name, Ref)>;


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

		virtual internal void load()
		{

		}

		private string m_filename;
	}

	[Serializable]
	public class Ref<T> : Ref where T : class
	{
		public T res => m_res != null ? m_res : m_res = Mgr.load<T>( filename );

		//For serialization
		public Ref()
			:
			base( "<unknown>" )
		{
		}

		public Ref( string filename )
			:
			base( filename )
		{
		}

		public Ref( string filename, T res ) : base( filename )
		{
			m_res = res;
		}


		override internal void load()
		{
			m_res = Mgr.load<T>( filename );
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

	public delegate T Load<out T>( string filename );


	class LoadHolder
	{
		internal virtual object load()
		{
			return null;
		}
	}


	class LoadHolder<T> : LoadHolder
	{
		public LoadHolder( Load<T> _dlgtLoad )
		{
			dlgtLoad = _dlgtLoad;
		}


		public Load<T> dlgtLoad;

		internal override object load()
		{
			return load();
		}
	}

	//generic classes make a new static per generic type
	class ResCache<T> where T : class
	{
		public static T s_default = default;
		public static ImmutableDictionary<string, WeakReference<T>> s_cache = ImmutableDictionary<string, WeakReference<T>>.Empty;



	}


	public class Mgr
	{


		static public void startup()
		{
			Resource.mgr = new Mgr();
		}

		static public void register<T>( Load<T> loader )
		{
			Debug.Assert( !Resource.mgr.m_loaders.ContainsKey( typeof( T ) ) );

			var lh = new LoadHolder<T>( loader );

			ImmutableInterlocked.TryAdd( ref Resource.mgr.m_loaders, typeof( T ), lh );
		}

		//Register all subclasses of a particular type
		//???? Should we just always do this?  
		static public void registerSub( Type baseType )
		{

			Type[] typeParams = new Type[1];
			foreach( var mi in baseType.GetMethods() )
			{
				if( mi.Name == "res_load" && mi.IsGenericMethod )
				{
					foreach( var ass in AppDomain.CurrentDomain.GetAssemblies() )
					{
						foreach( var t in ass.GetTypes() )
						{
							if( t.IsSubclassOf( baseType ) )
							{
								typeParams[0] = t;
								var mi_ng = mi.MakeGenericMethod( typeParams );

								var loadGenType = typeof(Load<>);

								var loadType = loadGenType.MakeGenericType( t );

								var loader = Delegate.CreateDelegate( loadType, mi_ng );

								var lhGenType = typeof(LoadHolder<>);

								var lhType = lhGenType.MakeGenericType( t );

								var lh = Activator.CreateInstance( lhType, loader ) as LoadHolder;

								ImmutableInterlocked.TryAdd( ref Resource.mgr.m_loaders, t, lh );
							}
						}
					}
					return;
				}
			}
		}


		static public Ref<T> lookup<T>( string filename ) where T : class
		{
			/*
			LoadHolder loader_gen;
			Resource.mgr.m_loaders.TryGetValue( typeof( T ), out loader_gen );

			var loaderHolder = loader_gen as LoadHolder<T>;

			if( loaderHolder != null )
			{
				var rf_raw = loaderHolder.dlgtLoad( filename );
				Ref<T> rf = rf_raw as Ref<T>;
				return rf;
			}
			*/

			return new Ref<T>( filename );
		}

		//*
		static public Ref lookup( string filename, Type t )
		{
			/*
			LoadHolder loader_gen;
			Resource.mgr.m_loaders.TryGetValue( t, out loader_gen );

			var lhGenType = typeof(LoadHolder<>);


			if( loaderHolder != null )
			{
				var rf_raw = loaderHolder.load( filename );
				return rf_raw;
			}
			*/

			return new Ref( filename );
		}
		//*/

		static public T load<T>( string filename ) where T : class
		{
			if( ResCache<T>.s_cache.TryGetValue( filename, out var wr ) )
			{
				if( wr.TryGetTarget( out var v ) )
					return v;

				lib.Log.info( $"{filename} was in cache, but its been dropped, reloading." );
			}

			lib.Log.warn( $"Block Loading {filename}." );

			var newV = actualLoad<T>( filename );

			return newV;
		}

		static public T actualLoad<T>( string filename ) where T : class
		{
			if( s_loading.TryGetValue( filename, out var evt ) )
			{
				evt.WaitOne();

				if( ResCache<T>.s_cache.TryGetValue( filename, out var wr ) )
				{
					if( wr.TryGetTarget( out var v ) )
						return v;

					lib.Log.error( $"{filename} was in cache, but its been dropped, reloading." );
				}
			}

			var evtNew = new AutoResetEvent( false );

			if( ImmutableInterlocked.TryAdd( ref s_loading, filename, evtNew ) )
			{
				if( Resource.mgr.m_loaders.TryGetValue( typeof( T ), out var loaderGen ) )
				{
					var loader = loaderGen as LoadHolder<T>;

					var v = loader.dlgtLoad( filename );

					var weak = new WeakReference<T>( v );

					var alreadyAdded = !ImmutableInterlocked.TryAdd( ref ResCache<T>.s_cache, filename, weak );

					evtNew.Set();

					//Done loading 
					if( !ImmutableInterlocked.TryRemove( ref s_loading, filename, out var oldEvt ) )
					{
						lib.Log.error( $"Error removing loading event for {filename}" );
					}

					if( alreadyAdded )
					{
						lib.Log.error( $"Key {filename} already existed, though it shouldnt." );
					}

					return v;
				}
				else
				{
					lib.Log.error( $"Loader could not be found for type {typeof( T )}" );

					return ResCache<T>.s_default;
				}
			}

			return actualLoad<T>( filename );
		}

		static object s_loadingLock = new object();

		static ImmutableDictionary< string, AutoResetEvent > s_loading = ImmutableDictionary< string, AutoResetEvent >.Empty;
		static ImmDefLoad s_deferredLoad = ImmDefLoad.Empty;


		Mgr()
		{
			var ts = new ThreadStart( deferredLoader );

			m_deferredLoader = new Thread( ts );

			m_deferredLoader.Start();
		}

		void deferredLoader()
		{
			while( true )
			{
				Thread.Sleep( 1 );

				if( ImmutableInterlocked.TryDequeue( ref s_deferredLoad, out var v ) )
				{
					v.Item2.load();
				}
			}
		}



		ImmutableDictionary<Type, LoadHolder> m_loaders = ImmutableDictionary<Type, LoadHolder>.Empty;

		Thread m_deferredLoader;

	}





}
