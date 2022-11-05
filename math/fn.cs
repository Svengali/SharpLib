using Microsoft.CodeAnalysis.CSharp.Syntax;

using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;



namespace math
{

	static public class fn
	{

		static public float Clamp( float v, float min, float max )
		{
			return v < min ? min : v > max ? max : v;
		}

		static public float LogisticsUnit( float v, float spread = 4.0f, float height = 1.0f )
		{
			return LogisticsFull( v, height, spread, 0, 0.1f, -0.5f );
		}

		static public float LogisticsFull( float v, float height, float spread, float f, float g, float h )
		{
			float res = height / (1.0f + (float)Math.Pow( g, (spread * (v+h))) ) + f;

			return res;
		}

		static public float LogisticsSymmetric( float v, float height = 1.0f, float spread = 3.65f )
		{

			float fullHeight = height * 2.0f;

			float negF = -height;

			float res = LogisticsFull( v, fullHeight, spread, negF, 0.1f, 0.0f );

			return res;
		}

		//Tracked these down in Desmos
		static public float s_a =   0.0f;
		static public float s_b =   0.155f;
		static public float s_c =   1.03f;
		static public float s_d =   6.13f;
		static public float s_f = -10.2f;
		static public float s_g =   4.06f;

		static public float Quintic( float v )
		{
			var vv   = v * v;
			var vvv  = vv * v;
			var vvvv = vvv * v;
			var vvvvv= vvvv * v;

			var res = s_a + s_b*v + s_c*vv + s_d*vvv + s_f*vvvv + s_g * vvvvv;

			return res;
		}

		static public float s_p =  0.37f;
		static public float s_o =  0.15f;
		static public float s_m =  2.11f;
		static public float s_n = -0.57f;

		static public float PerlinToContinent( float h )
		{
			var res = Quintic( s_m * h + s_n ) * s_o + s_p;

			return res;
		}


		static public float SmoothStepCos( float v )
		{
			var dV = (double)v;

			var newV = 0.5 - 0.5 * Math.Cos( Math.PI * dV );

			return (float)newV;

		}

		static public float SmoothStepSquare( float v )
		{
			var dV = (double)v;

			var newV = dV * dV * (3.0 - 2.0 * dV);

			return (float)newV;

		}

		static public float SmoothStepCube( float v )
		{
			var dV = (double)v;

			var newV = dV * dV * dV * ( 6.0 * dV * dV - 15.0 * dV + 10.0 );

			return (float)newV;

		}
	}

}
