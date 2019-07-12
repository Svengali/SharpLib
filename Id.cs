using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

//using System.MemoryExtensions;

namespace lib
{


	public struct Id<T> : IComparable, IFormattable, IConvertible, IComparable<ulong>, IEquatable<ulong>
	{
		public const ulong Min = 0uL;
		public const ulong Max = 18446744073709551615uL;

		static Random s_rand = new Random();


		// TODO PERF Make span versions of all these functions

		unsafe public static Id<T> Generate()
		{
			var buf = new byte[8];

			s_rand.NextBytes( buf );

			var newId = BitConverter.ToUInt64( buf, 0 );

			return new Id<T> { m_value = newId };
		}

		ulong m_value;


		public int CompareTo( object value )
		{
			if( value == null )
			{
				return 1;
			}
			if( value is ulong )
			{
				ulong num = (ulong)value;
				if( m_value < num )
				{
					return -1;
				}
				if( m_value > num )
				{
					return 1;
				}
				return 0;
			}
			throw new ArgumentException( "" );
		}

		public int CompareTo( ulong value )
		{
			if( m_value < value )
			{
				return -1;
			}
			if( m_value > value )
			{
				return 1;
			}
			return 0;
		}

		public override bool Equals( object obj )
		{
			if( !( obj is ulong ) )
			{
				return false;
			}
			return m_value == (ulong)obj;
		}

		public bool Equals( ulong obj )
		{
			return m_value == obj;
		}
		
		public override int GetHashCode()
		{
			return (int)m_value ^ (int)( m_value >> 32 );
		}

		#region ToString
		//
		// Summary:
		//     Converts the numeric value of m_value instance to its equivalent string representation.
		//
		// Returns:
		//     The string representation of the value of m_value instance, consisting of a sequence
		//     of digits ranging from 0 to 9, without a sign or leading zeroes.
		[SecuritySafeCritical]

		public override string ToString()
		{
			return m_value.ToString( null, NumberFormatInfo.CurrentInfo );
		}

		//
		// Summary:
		//     Converts the numeric value of m_value instance to its equivalent string representation
		//     using the specified culture-specific format information.
		//
		// Parameters:
		//   provider:
		//     An object that supplies culture-specific formatting information.
		//
		// Returns:
		//     The string representation of the value of m_value instance as specified by provider.
		[SecuritySafeCritical]

		public string ToString( IFormatProvider provider )
		{
			return m_value.ToString( null, NumberFormatInfo.GetInstance( provider ) );
		}

		//
		// Summary:
		//     Converts the numeric value of m_value instance to its equivalent string representation
		//     using the specified format.
		//
		// Parameters:
		//   format:
		//     A numeric format string.
		//
		// Returns:
		//     The string representation of the value of m_value instance as specified by format.
		//
		// Exceptions:
		//   T:System.FormatException:
		//     The format parameter is invalid.
		[SecuritySafeCritical]

		public string ToString( string format )
		{
			return m_value.ToString( format, NumberFormatInfo.CurrentInfo );
		}

		//
		// Summary:
		//     Converts the numeric value of m_value instance to its equivalent string representation
		//     using the specified format and culture-specific format information.
		//
		// Parameters:
		//   format:
		//     A numeric format string.
		//
		//   provider:
		//     An object that supplies culture-specific formatting information about m_value instance.
		//
		// Returns:
		//     The string representation of the value of m_value instance as specified by format
		//     and provider.
		//
		// Exceptions:
		//   T:System.FormatException:
		//     The format parameter is invalid.
		[SecuritySafeCritical]

		public string ToString( string format, IFormatProvider provider )
		{
			return m_value.ToString( format, NumberFormatInfo.GetInstance( provider ) );
		}

		#endregion

		#region Parse
		//
		// Summary:
		//     Converts the string representation of a number to its 64-bit unsigned integer
		//     equivalent.
		//
		// Parameters:
		//   s:
		//     A string that represents the number to convert.
		//
		// Returns:
		//     A 64-bit unsigned integer equivalent to the number contained in s.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     The s parameter is null.
		//
		//   T:System.FormatException:
		//     The s parameter is not in the correct format.
		//
		//   T:System.OverflowException:
		//     The s parameter represents a number less than System.UInt64.MinValue or greater
		//     than System.UInt64.MaxValue.
		

		public static ulong Parse( string s )
		{

			return ulong.Parse( s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo );
		}

		//
		// Summary:
		//     Converts the string representation of a number in a specified style to its 64-bit
		//     unsigned integer equivalent.
		//
		// Parameters:
		//   s:
		//     A string that represents the number to convert. The string is interpreted by
		//     using the style specified by the style parameter.
		//
		//   style:
		//     A bitwise combination of the enumeration values that specifies the permitted
		//     format of s. A typical value to specify is System.Globalization.NumberStyles.Integer.
		//
		// Returns:
		//     A 64-bit unsigned integer equivalent to the number specified in s.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     The s parameter is null.
		//
		//   T:System.ArgumentException:
		//     /// style is not a System.Globalization.NumberStyles value. -or-style is not
		//     a combination of System.Globalization.NumberStyles.AllowHexSpecifier and System.Globalization.NumberStyles.HexNumber
		//     values.
		//
		//   T:System.FormatException:
		//     The s parameter is not in a format compliant with style.
		//
		//   T:System.OverflowException:
		//     The s parameter represents a number less than System.UInt64.MinValue or greater
		//     than System.UInt64.MaxValue. -or-s includes non-zero, fractional digits.
		

		public static ulong Parse( string s, NumberStyles style )
		{
			return ulong.Parse( s, style, NumberFormatInfo.CurrentInfo );
		}

		//
		// Summary:
		//     Converts the string representation of a number in a specified culture-specific
		//     format to its 64-bit unsigned integer equivalent.
		//
		// Parameters:
		//   s:
		//     A string that represents the number to convert.
		//
		//   provider:
		//     An object that supplies culture-specific formatting information about s.
		//
		// Returns:
		//     A 64-bit unsigned integer equivalent to the number specified in s.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     The s parameter is null.
		//
		//   T:System.FormatException:
		//     The s parameter is not in the correct style.
		//
		//   T:System.OverflowException:
		//     The s parameter represents a number less than System.UInt64.MinValue or greater
		//     than System.UInt64.MaxValue.
		

		public static ulong Parse( string s, IFormatProvider provider )
		{
			return ulong.Parse( s, NumberStyles.Integer, NumberFormatInfo.GetInstance( provider ) );
		}

		//
		// Summary:
		//     Converts the string representation of a number in a specified style and culture-specific
		//     format to its 64-bit unsigned integer equivalent.
		//
		// Parameters:
		//   s:
		//     A string that represents the number to convert. The string is interpreted by
		//     using the style specified by the style parameter.
		//
		//   style:
		//     A bitwise combination of enumeration values that indicates the style elements
		//     that can be present in s. A typical value to specify is System.Globalization.NumberStyles.Integer.
		//
		//   provider:
		//     An object that supplies culture-specific formatting information about s.
		//
		// Returns:
		//     A 64-bit unsigned integer equivalent to the number specified in s.
		//
		// Exceptions:
		//   T:System.ArgumentNullException:
		//     The s parameter is null.
		//
		//   T:System.ArgumentException:
		//     /// style is not a System.Globalization.NumberStyles value. -or-style is not
		//     a combination of System.Globalization.NumberStyles.AllowHexSpecifier and System.Globalization.NumberStyles.HexNumber
		//     values.
		//
		//   T:System.FormatException:
		//     The s parameter is not in a format compliant with style.
		//
		//   T:System.OverflowException:
		//     The s parameter represents a number less than System.UInt64.MinValue or greater
		//     than System.UInt64.MaxValue. -or-s includes non-zero, fractional digits.
		

		public static ulong Parse( string s, NumberStyles style, IFormatProvider provider )
		{
			return ulong.Parse( s, style, NumberFormatInfo.GetInstance( provider ) );
		}

		//
		// Summary:
		//     Tries to convert the string representation of a number to its 64-bit unsigned
		//     integer equivalent. A return value indicates whether the conversion succeeded
		//     or failed.
		//
		// Parameters:
		//   s:
		//     A string that represents the number to convert.
		//
		//   result:
		//     When m_value method returns, contains the 64-bit unsigned integer value that is
		//     equivalent to the number contained in s, if the conversion succeeded, or zero
		//     if the conversion failed. The conversion fails if the s parameter is null, is
		//     not of the correct format, or represents a number less than System.UInt64.MinValue
		//     or greater than System.UInt64.MaxValue. m_value parameter is passed uninitialized.
		//
		// Returns:
		//     true if s was converted successfully; otherwise, false.
		

		public static bool TryParse( string s, out ulong result )
		{
			return ulong.TryParse( s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result );
		}

		//
		// Summary:
		//     Tries to convert the string representation of a number in a specified style and
		//     culture-specific format to its 64-bit unsigned integer equivalent. A return value
		//     indicates whether the conversion succeeded or failed.
		//
		// Parameters:
		//   s:
		//     A string that represents the number to convert. The string is interpreted by
		//     using the style specified by the style parameter.
		//
		//   style:
		//     A bitwise combination of enumeration values that indicates the permitted format
		//     of s. A typical value to specify is System.Globalization.NumberStyles.Integer.
		//
		//   provider:
		//     An object that supplies culture-specific formatting information about s.
		//
		//   result:
		//     When m_value method returns, contains the 64-bit unsigned integer value equivalent
		//     to the number contained in s, if the conversion succeeded, or zero if the conversion
		//     failed. The conversion fails if the s parameter is null, is not in a format compliant
		//     with style, or represents a number less than System.UInt64.MinValue or greater
		//     than System.UInt64.MaxValue. m_value parameter is passed uninitialized.
		//
		// Returns:
		//     true if s was converted successfully; otherwise, false.
		//
		// Exceptions:
		//   T:System.ArgumentException:
		//     /// style is not a System.Globalization.NumberStyles value. -or-style is not
		//     a combination of System.Globalization.NumberStyles.AllowHexSpecifier and System.Globalization.NumberStyles.HexNumber
		//     values.
		

		public static bool TryParse( string s, NumberStyles style, IFormatProvider provider, out ulong result )
		{
			return ulong.TryParse( s, style, NumberFormatInfo.GetInstance( provider ), out result );
		}

		#endregion


		//
		// Summary:
		//     Returns the System.TypeCode for value type System.UInt64.
		//
		// Returns:
		//     The enumerated constant, System.TypeCode.UInt64.
		public TypeCode GetTypeCode()
		{
			return TypeCode.UInt64;
		}

		#region Converters
		//
		// Summary:
		//     For a description of m_value member, see System.IConvertible.ToBoolean(System.IFormatProvider).
		//
		// Parameters:
		//   provider:
		//     m_value parameter is ignored.
		//
		// Returns:
		//     true if the value of the current instance is not zero; otherwise, false.

		bool IConvertible.ToBoolean( IFormatProvider provider )
		{
			return Convert.ToBoolean( m_value );
		}

		//
		// Summary:
		//     For a description of m_value member, see System.IConvertible.ToChar(System.IFormatProvider).
		//
		// Parameters:
		//   provider:
		//     m_value parameter is ignored.
		//
		// Returns:
		//     The value of the current instance, converted to a System.Char.

		char IConvertible.ToChar( IFormatProvider provider )
		{
			return Convert.ToChar( m_value );
		}

		//
		// Summary:
		//     For a description of m_value member, see System.IConvertible.ToSByte(System.IFormatProvider).
		//
		// Parameters:
		//   provider:
		//     m_value parameter is ignored.
		//
		// Returns:
		//     The value of the current instance, converted to an System.SByte.

		sbyte IConvertible.ToSByte( IFormatProvider provider )
		{
			return Convert.ToSByte( m_value );
		}

		//
		// Summary:
		//     For a description of m_value member, see System.IConvertible.ToByte(System.IFormatProvider).
		//
		// Parameters:
		//   provider:
		//     m_value parameter is ignored.
		//
		// Returns:
		//     The value of the current instance, converted to a System.Byte.

		byte IConvertible.ToByte( IFormatProvider provider )
		{
			return Convert.ToByte( m_value );
		}

		//
		// Summary:
		//     For a description of m_value member, see System.IConvertible.ToInt16(System.IFormatProvider).
		//
		// Parameters:
		//   provider:
		//     m_value parameter is ignored.
		//
		// Returns:
		//     The value of the current instance, converted to an System.Int16.

		short IConvertible.ToInt16( IFormatProvider provider )
		{
			return Convert.ToInt16( m_value );
		}

		//
		// Summary:
		//     For a description of m_value member, see System.IConvertible.ToUInt16(System.IFormatProvider).
		//
		// Parameters:
		//   provider:
		//     m_value parameter is ignored.
		//
		// Returns:
		//     The value of the current instance, converted to a System.UInt16.

		ushort IConvertible.ToUInt16( IFormatProvider provider )
		{
			return Convert.ToUInt16( m_value );
		}

		//
		// Summary:
		//     For a description of m_value member, see System.IConvertible.ToInt32(System.IFormatProvider).
		//
		// Parameters:
		//   provider:
		//     m_value parameter is ignored.
		//
		// Returns:
		//     The value of the current instance, converted to an System.Int32.

		int IConvertible.ToInt32( IFormatProvider provider )
		{
			return Convert.ToInt32( m_value );
		}

		//
		// Summary:
		//     For a description of m_value member, see System.IConvertible.ToUInt32(System.IFormatProvider).
		//
		// Parameters:
		//   provider:
		//     m_value parameter is ignored.
		//
		// Returns:
		//     The value of the current instance, converted to a System.UInt32.

		uint IConvertible.ToUInt32( IFormatProvider provider )
		{
			return Convert.ToUInt32( m_value );
		}

		//
		// Summary:
		//     For a description of m_value member, see System.IConvertible.ToInt64(System.IFormatProvider).
		//
		// Parameters:
		//   provider:
		//     m_value parameter is ignored.
		//
		// Returns:
		//     The value of the current instance, converted to an System.Int64.

		long IConvertible.ToInt64( IFormatProvider provider )
		{
			return Convert.ToInt64( m_value );
		}

		//
		// Summary:
		//     For a description of m_value member, see System.IConvertible.ToUInt64(System.IFormatProvider).
		//
		// Parameters:
		//   provider:
		//     m_value parameter is ignored.
		//
		// Returns:
		//     The value of the current instance, unchanged.

		ulong IConvertible.ToUInt64( IFormatProvider provider )
		{
			return m_value;
		}

		//
		// Summary:
		//     For a description of m_value member, see System.IConvertible.ToSingle(System.IFormatProvider).
		//
		// Parameters:
		//   provider:
		//     m_value parameter is ignored.
		//
		// Returns:
		//     The value of the current instance, converted to a System.Single.

		float IConvertible.ToSingle( IFormatProvider provider )
		{
			return Convert.ToSingle( m_value );
		}

		//
		// Summary:
		//     For a description of m_value member, see System.IConvertible.ToDouble(System.IFormatProvider).
		//
		// Parameters:
		//   provider:
		//     m_value parameter is ignored.
		//
		// Returns:
		//     The value of the current instance, converted to a System.Double.

		double IConvertible.ToDouble( IFormatProvider provider )
		{
			return Convert.ToDouble( m_value );
		}

		decimal IConvertible.ToDecimal( IFormatProvider provider )
		{
			return Convert.ToDecimal( m_value );
		}

		DateTime IConvertible.ToDateTime( IFormatProvider provider )
		{
			throw new InvalidCastException( $"InvalidCast_FromTo UInt64 DateTime" );
		}

		public object ToType( Type conversionType, IFormatProvider provider )
		{
			return Convert.ChangeType( m_value, conversionType, provider );
		}

		#endregion

	}




}
