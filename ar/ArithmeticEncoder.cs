/* 
 * Reference arithmetic coding
 * Copyright (c) Project Nayuki
 * 
 * https://www.nayuki.io/page/reference-arithmetic-coding
 * https://github.com/nayuki/Reference-arithmetic-coding
 */



using System;
/// <summary>
/// Encodes symbols and writes to an arithmetic-coded bit stream. Not thread-safe. </summary>
/// <seealso cref= ArithmeticDecoder </seealso>
public sealed class ArithmeticEncoder : ArithmeticCoderBase
{

	/*---- Fields ----*/

	// The underlying bit output stream (not null).
	private BitOutputStream output;

	// Number of saved underflow bits. This value can grow without bound,
	// so a truly correct implementation would use a BigInteger.
	private int numUnderflow;



	/*---- Constructor ----*/

	/// <summary>
	/// Constructs an arithmetic coding encoder based on the specified bit output stream. </summary>
	/// <param name="numBits"> the number of bits for the arithmetic coding range </param>
	/// <param name="out"> the bit output stream to write to </param>
	/// <exception cref="NullPointerException"> if the output stream is {@code null} </exception>
	/// <exception cref="IllegalArgumentException"> if stateSize is outside the range [1, 62] </exception>
	public ArithmeticEncoder(int numBits, BitOutputStream @out) : base(numBits)
	{
		output = @out; //Objects.requireNonNull(@out);
		numUnderflow = 0;
	}



	/*---- Methods ----*/

	/// <summary>
	/// Encodes the specified symbol based on the specified frequency table.
	/// This updates this arithmetic coder's state and may write out some bits. </summary>
	/// <param name="freqs"> the frequency table to use </param>
	/// <param name="symbol"> the symbol to encode </param>
	/// <exception cref="NullPointerException"> if the frequency table is {@code null} </exception>
	/// <exception cref="IllegalArgumentException"> if the symbol has zero frequency
	/// or the frequency table's total is too large </exception>
	/// <exception cref="IOException"> if an I/O exception occurred </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: public void write(FrequencyTable freqs, int symbol) throws java.io.IOException
	public void write(FrequencyTable freqs, int symbol)
	{
		write(new CheckedFrequencyTable(freqs), symbol);
	}


	/// <summary>
	/// Encodes the specified symbol based on the specified frequency table.
	/// Also updates this arithmetic coder's state and may write out some bits. </summary>
	/// <param name="freqs"> the frequency table to use </param>
	/// <param name="symbol"> the symbol to encode </param>
	/// <exception cref="NullPointerException"> if the frequency table is {@code null} </exception>
	/// <exception cref="IllegalArgumentException"> if the symbol has zero frequency
	/// or the frequency table's total is too large </exception>
	/// <exception cref="IOException"> if an I/O exception occurred </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: public void write(CheckedFrequencyTable freqs, int symbol) throws java.io.IOException
	public void write(CheckedFrequencyTable freqs, int symbol)
	{
		update(freqs, symbol);
	}


	/// <summary>
	/// Terminates the arithmetic coding by flushing any buffered bits, so that the output can be decoded properly.
	/// It is important that this method must be called at the end of the each encoding process.
	/// <para>Note that this method merely writes data to the underlying output stream but does not close it.</para> </summary>
	/// <exception cref="IOException"> if an I/O exception occurred </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: public void finish() throws java.io.IOException
	public void finish()
	{
		output.write(1);
	}


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: protected void shift() throws java.io.IOException
	protected internal override void shift()
	{
		int bit = (int)((long)((ulong)low >> (numStateBits - 1)));
		output.write(bit);

		// Write out the saved underflow bits
		for (; numUnderflow > 0; numUnderflow--)
		{
			output.write(bit ^ 1);
		}
	}


	protected internal override void underflow()
	{
		if (numUnderflow == int.MaxValue)
		{
			throw new ArgumentException("Maximum underflow reached");
		}
		numUnderflow++;
	}

}
