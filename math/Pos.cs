using System;

namespace lib
{

	[Serializable]
	public struct Pos
	{
		public float x { get; private set; }
		public float y { get; private set; }
		public float z { get; private set; }


		public Pos( float _x, float _y, float _z ) : this()
		{
			x = _x;
			y = _y;
			z = _z;
		}

		// overload operator +
		public static Pos operator +( Pos a, Pos b )
		{
			return new Pos( a.x + b.x, a.y + b.y, a.z + b.z );
		}

		public static Pos operator -( Pos a, Pos b )
		{
			return new Pos( a.x - b.x, a.y - b.y, a.z - b.z );
		}

		public static Pos operator /( Pos a, float val )
		{
			return new Pos( a.x / val, a.y / val, a.z / val );
		}

		public static Pos operator *( Pos a, float val )
		{
			return new Pos( a.x * val, a.y * val, a.z * val );
		}

		public float distSqr( Pos other )
		{
			float dx = x - other.x;
			float dy = y - other.y;
			float dz = z - other.z;

			return dx * dx + dy * dy + dz * dz;
		}
	}

}
