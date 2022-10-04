// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2011 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

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
