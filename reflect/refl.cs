using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;








static public class refl
{

	public class PredEnumerator
	{
		public static PredEnumerator<T> Create<T>( IEnumerator<T> en, Predicate<T> pred )
		{
			return new PredEnumerator<T>( en, pred );
		}

		public static PredEnumerator<T> Create<T>( IEnumerable<T> en, Predicate<T> pred )
		{
			return new PredEnumerator<T>( en.GetEnumerator(), pred );
		}
	}

	public class PredEnumerator<T>: PredEnumerator, IEnumerator<T>
	{

		public T Current => m_en.Current;

		object IEnumerator.Current => m_en.Current;

		public PredEnumerator( IEnumerator<T> en, Predicate<T> pred )
		{
			m_en = en;
			m_pred = pred;
		}


		public bool MoveNext()
		{
			var success = m_en.MoveNext();

			if( !success )
				return false;

			while( !m_pred( m_en.Current ) && ( success = m_en.MoveNext() ) )
			{

			}

			return success;
		}

		public void Reset()
		{
			m_en.Reset();
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose( bool disposing )
		{
			if( !disposedValue )
			{
				if( disposing )
				{
					// TODO: dispose managed state (managed objects).
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~MyEnumerator()
		// {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose( true );
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion

		IEnumerator<T> m_en;
		Predicate<T> m_pred;
	}


	public class PredEnumerable<T>: IEnumerable<T>
	{
		public PredEnumerable( PredEnumerator<T> en )
		{
			m_en = en;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return m_en;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return m_en;
		}

		PredEnumerator<T> m_en;
	}


	public static void GetAllFields( Type t, List<FieldInfo> list )
	{
		var fieldArr = t.GetFields(
					 BindingFlags.DeclaredOnly |
					 BindingFlags.NonPublic |
					 BindingFlags.Public |
					 BindingFlags.Instance );

		var en = PredEnumerator.Create<FieldInfo>( fieldArr.AsEnumerable<FieldInfo>(), fa => fa.GetCustomAttribute( typeof( NonSerializedAttribute ) ) == null );

		list.AddRange( new PredEnumerable<FieldInfo>( en ) );

		if( t.BaseType != null && t.BaseType != typeof( object ) )
		{
			GetAllFields( t.BaseType, list );
		}
	}


	public static ImmutableList<FieldInfo> GetAllFields( Type t )
	{
		{
			if( s_fieldCache.TryGetValue( t, out var first ) )
				return first;
		}

		lock( t )
		{
			if( s_fieldCache.TryGetValue( t, out var second ) )
				return second;

			var list = new List<FieldInfo>();

			GetAllFields( t, list );

			var immList = list.ToImmutableList();

			Interlocked.Exchange( ref s_fieldCache, s_fieldCache.Add( t, immList ) );

			return immList;
		}
	}


	public static void GetAllProperties( Type t, List<PropertyInfo> list )
	{
		var propArr = t.GetProperties(
					 BindingFlags.DeclaredOnly |
					 BindingFlags.NonPublic |
					 BindingFlags.Public |
					 BindingFlags.Instance
					 );



		var en = PredEnumerator.Create<PropertyInfo>( propArr.AsEnumerable<PropertyInfo>(),
			fa => fa.GetCustomAttribute( typeof( NonSerializedAttribute ) ) == null && !list.Exists( f => f.Name == fa.Name ) );

		list.AddRange( new PredEnumerable<PropertyInfo>( en ) );

		if( t.BaseType != null && t.BaseType != typeof( object ) )
		{
			GetAllProperties( t.BaseType, list );
		}
	}

	public static ImmutableList<PropertyInfo> GetAllProperties( Type t )
	{
		if( s_propCache.TryGetValue( t, out var info ) )
			return info;

		var list = new List<PropertyInfo>();

		GetAllProperties( t, list );

		var immList = list.ToImmutableList();

		s_propCache = s_propCache.Add( t, immList );

		return immList;
	}


	static ImmutableDictionary<Type, ImmutableList<FieldInfo>> s_fieldCache = ImmutableDictionary<Type, ImmutableList<FieldInfo>>.Empty;
	static ImmutableDictionary<Type, ImmutableList<PropertyInfo>> s_propCache = ImmutableDictionary<Type, ImmutableList<PropertyInfo>>.Empty;




	//SLOW
	static public string TypeToIdentifier( string typename )
	{
		return typename.Replace( '<', '_' ).Replace( '>', '_' ).Replace( ',', '_' ).Replace( ' ', '_' ).Replace( '.', '_' ).Replace( '+', '_' ).Replace( '[', '_' ).Replace( ']', '_' ).Replace( '$', '_' ).Replace( ':', '_' );
	}





}
