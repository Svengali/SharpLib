using System;
using System.Diagnostics;

/* 
 * Reference arithmetic coding
 * Copyright (c) Project Nayuki
 * 
 * https://www.nayuki.io/page/reference-arithmetic-coding
 * https://github.com/nayuki/Reference-arithmetic-coding
 */


/// <summary>
/// Provides the state and behaviors that arithmetic coding encoders and decoders share. </summary>
/// <seealso cref= ArithmeticEncoder </seealso>
/// <seealso cref= ArithmeticDecoder </seealso>
public abstract class ArithmeticCoderBase
{

	/*---- Configuration fields ----*/

	/// <summary>
	/// Number of bits for the 'low' and 'high' state variables. Must be in the range [1, 62].
	/// <ul>
	///   <li>For state sizes less than the midpoint of around 32, larger values are generally better -
	///   they allow a larger maximum frequency total (maximumTotal), and they reduce the approximation
	///   error inherent in adapting fractions to integers; both effects reduce the data encoding loss
	///   and asymptotically approach the efficiency of arithmetic coding using exact fractions.</li>
	///   <li>But for state sizes greater than the midpoint, because intermediate computations are limited
	///   to the long integer type's 63-bit unsigned precision, larger state sizes will decrease the
	///   maximum frequency total, which might constrain the user-supplied probability model.</li>
	///   <li>Therefore numStateBits=32 is recommended as the most versatile setting
	///   because it maximizes maximumTotal (which ends up being slightly over 2^30).</li>
	///   <li>Note that numStateBits=62 is legal but useless because it implies maximumTotal=1,
	///   which means a frequency table can only support one symbol with non-zero frequency.</li>
	/// </ul>
	/// </summary>
	protected internal readonly int numStateBits;

	/// <summary>
	/// Maximum range (high+1-low) during coding (trivial), which is 2^numStateBits = 1000...000. </summary>
	protected internal readonly long fullRange;

	/// <summary>
	/// The top bit at width numStateBits, which is 0100...000. </summary>
	protected internal readonly long halfRange;

	/// <summary>
	/// The second highest bit at width numStateBits, which is 0010...000. This is zero when numStateBits=1. </summary>
	protected internal readonly long quarterRange;

	/// <summary>
	/// Minimum range (high+1-low) during coding (non-trivial), which is 0010...010. </summary>
	protected internal readonly long minimumRange;

	/// <summary>
	/// Maximum allowed total from a frequency table at all times during coding. </summary>
	protected internal readonly long maximumTotal;

	/// <summary>
	/// Bit mask of numStateBits ones, which is 0111...111. </summary>
	protected internal readonly long stateMask;



	/*---- State fields ----*/

	/// <summary>
	/// Low end of this arithmetic coder's current range. Conceptually has an infinite number of trailing 0s.
	/// </summary>
	protected internal long low;

	/// <summary>
	/// High end of this arithmetic coder's current range. Conceptually has an infinite number of trailing 1s.
	/// </summary>
	protected internal long high;



	/*---- Constructor ----*/

	/// <summary>
	/// Constructs an arithmetic coder, which initializes the code range. </summary>
	/// <param name="numBits"> the number of bits for the arithmetic coding range </param>
	/// <exception cref="IllegalArgumentException"> if stateSize is outside the range [1, 62] </exception>
	public ArithmeticCoderBase(int numBits)
	{
		if (numBits < 1 || numBits > 62)
		{
			throw new System.ArgumentException("State size out of range");
		}
		numStateBits = numBits;
		fullRange = 1L << numStateBits;
		halfRange = (long)((ulong)fullRange >> 1); // Non-zero
		quarterRange = (long)((ulong)halfRange >> 1); // Can be zero
		minimumRange = quarterRange + 2; // At least 2
		maximumTotal = Math.Min(long.MaxValue / fullRange, minimumRange);
		stateMask = fullRange - 1;

		low = 0;
		high = stateMask;
	}



	/*---- Methods ----*/

	/// <summary>
	/// Updates the code range (low and high) of this arithmetic coder as a result
	/// of processing the specified symbol with the specified frequency table.
	/// <para>Invariants that are true before and after encoding/decoding each symbol
	/// (letting fullRange = 2<sup>numStateBits</sup>):</para>
	/// <ul>
	///   <li>0 &le; low &le; code &le; high &lt; fullRange. ('code' exists only in the decoder.)
	///   Therefore these variables are unsigned integers of numStateBits bits.</li>
	///   <li>low &lt; 1/2 &times; fullRange &le; high.
	///   In other words, they are in different halves of the full range.</li>
	///   <li>(low &lt; 1/4 &times; fullRange) || (high &ge; 3/4 &times; fullRange).
	///   In other words, they are not both in the middle two quarters.</li>
	///   <li>Let range = high &minus; low + 1, then fullRange/4 &lt; minimumRange &le; range &le;
	///   fullRange. These invariants for 'range' essentially dictate the maximum total that the
	///   incoming frequency table can have, such that intermediate calculations don't overflow.</li>
	/// </ul> </summary>
	/// <param name="freqs"> the frequency table to use </param>
	/// <param name="symbol"> the symbol that was processed </param>
	/// <exception cref="IllegalArgumentException"> if the symbol has zero frequency or the frequency table's total is too large </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: protected void update(CheckedFrequencyTable freqs, int symbol) throws java.io.IOException
	protected internal virtual void update(CheckedFrequencyTable freqs, int symbol)
	{
		// State check
		Debug.Assert(low >= high || (low & stateMask) != low || (high & stateMask) != high, "Low or high out of range");

		long range = high - low + 1;
		Debug.Assert(range < minimumRange || range > fullRange, "Range out of range");

		// Frequency table values check
		long total = freqs.Total;
		long symLow = freqs.getLow(symbol);
		long symHigh = freqs.getHigh(symbol);
		Debug.Assert( symLow == symHigh, "Symbol has zero frequency");

		Debug.Assert( total > maximumTotal, "Cannot code symbol because total is too large");

		// Update range
		long newLow = low + symLow * range / total;
		long newHigh = low + symHigh * range / total - 1;
		low = newLow;
		high = newHigh;

		// While low and high have the same top bit value, shift them out
		while (((low ^ high) & halfRange) == 0)
		{
			shift();
			low = ((low << 1) & stateMask);
			high = ((high << 1) & stateMask) | 1;
		}
		// Now low's top bit must be 0 and high's top bit must be 1

		// While low's top two bits are 01 and high's are 10, delete the second highest bit of both
		while ((low & ~high & quarterRange) != 0)
		{
			underflow();
			low = (low << 1) ^ halfRange;
			high = ((high ^ halfRange) << 1) | halfRange | 1;
		}
	}


	/// <summary>
	/// Called to handle the situation when the top bit of {@code low} and {@code high} are equal. </summary>
	/// <exception cref="IOException"> if an I/O exception occurred </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: protected abstract void shift() throws java.io.IOException;
	protected internal abstract void shift();


	/// <summary>
	/// Called to handle the situation when low=01(...) and high=10(...). </summary>
	/// <exception cref="IOException"> if an I/O exception occurred </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: protected abstract void underflow() throws java.io.IOException;
	protected internal abstract void underflow();

}
