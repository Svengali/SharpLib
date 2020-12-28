/* 
 * Reference arithmetic coding
 * Copyright (c) Project Nayuki
 * 
 * https://www.nayuki.io/page/reference-arithmetic-coding
 * https://github.com/nayuki/Reference-arithmetic-coding
 */



using System.Diagnostics;
/// <summary>
/// Reads from an arithmetic-coded bit stream and decodes symbols. Not thread-safe. </summary>
/// <seealso cref= ArithmeticEncoder </seealso>
public sealed class ArithmeticDecoder : ArithmeticCoderBase
{

	/*---- Fields ----*/

	// The underlying bit input stream (not null).
	private BitInputStream input;

	// The current raw code bits being buffered, which is always in the range [low, high].
	private long code;



	/*---- Constructor ----*/

	/// <summary>
	/// Constructs an arithmetic coding decoder based on the
	/// specified bit input stream, and fills the code bits. </summary>
	/// <param name="numBits"> the number of bits for the arithmetic coding range </param>
	/// <param name="in"> the bit input stream to read from </param>
	/// <exception cref="NullPointerException"> if the input steam is {@code null} </exception>
	/// <exception cref="IllegalArgumentException"> if stateSize is outside the range [1, 62] </exception>
	/// <exception cref="IOException"> if an I/O exception occurred </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: public ArithmeticDecoder(int numBits, BitInputStream in) throws java.io.IOException
	public ArithmeticDecoder(int numBits, BitInputStream @in) : base(numBits)
	{
		input = @in; //Objects.requireNonNull(@in);
		code = 0;
		for (int i = 0; i < numStateBits; i++)
		{
			code = code << 1 | readCodeBit();
		}
	}



	/*---- Methods ----*/

	/// <summary>
	/// Decodes the next symbol based on the specified frequency table and returns it.
	/// Also updates this arithmetic coder's state and may read in some bits. </summary>
	/// <param name="freqs"> the frequency table to use </param>
	/// <returns> the next symbol </returns>
	/// <exception cref="NullPointerException"> if the frequency table is {@code null} </exception>
	/// <exception cref="IOException"> if an I/O exception occurred </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: public int read(FrequencyTable freqs) throws java.io.IOException
	public int read(FrequencyTable freqs)
	{
		return read(new CheckedFrequencyTable(freqs));
	}


	/// <summary>
	/// Decodes the next symbol based on the specified frequency table and returns it.
	/// Also updates this arithmetic coder's state and may read in some bits. </summary>
	/// <param name="freqs"> the frequency table to use </param>
	/// <returns> the next symbol </returns>
	/// <exception cref="NullPointerException"> if the frequency table is {@code null} </exception>
	/// <exception cref="IllegalArgumentException"> if the frequency table's total is too large </exception>
	/// <exception cref="IOException"> if an I/O exception occurred </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: public int read(CheckedFrequencyTable freqs) throws java.io.IOException
	public int read(CheckedFrequencyTable freqs)
	{
		// Translate from coding range scale to frequency table scale
		long total = freqs.Total;
		if (total > maximumTotal)
		{
			throw new System.ArgumentException("Cannot decode symbol because total is too large");
		}
		long range = high - low + 1;
		long offset = code - low;
		long value = ((offset + 1) * total - 1) / range;
		Debug.Assert(value * range / total > offset);

		Debug.Assert(value < 0 || value >= total);

		// A kind of binary search. Find highest symbol such that freqs.getLow(symbol) <= value.
		int start = 0;
		int end = freqs.SymbolLimit;
		while (end - start > 1)
		{
			int middle = (int)((uint)(start + end) >> 1);
			if (freqs.getLow(middle) > value)
			{
				end = middle;
			}
			else
			{
				start = middle;
			}
		}
		Debug.Assert( start + 1 != end);


		int symbol = start;
		Debug.Assert(offset < freqs.getLow(symbol) * range / total || freqs.getHigh(symbol) * range / total <= offset);

		update(freqs, symbol);
		Debug.Assert(code < low || code > high);

		return symbol;
	}


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: protected void shift() throws java.io.IOException
	protected internal override void shift()
	{
		code = ((code << 1) & stateMask) | readCodeBit();
	}


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: protected void underflow() throws java.io.IOException
	protected internal override void underflow()
	{
		code = (code & halfRange) | ((code << 1) & ((long)((ulong)stateMask >> 1))) | readCodeBit();
	}


	// Returns the next bit (0 or 1) from the input stream. The end
	// of stream is treated as an infinite number of trailing zeros.
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: private int readCodeBit() throws java.io.IOException
	private int readCodeBit()
	{
		int temp = input.read();
		if (temp == -1)
		{
			temp = 0;
		}
		return temp;
	}

}
