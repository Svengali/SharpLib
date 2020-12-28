using System;
using System.IO;

/* 
 * Reference arithmetic coding
 * Copyright (c) Project Nayuki
 * 
 * https://www.nayuki.io/page/reference-arithmetic-coding
 * https://github.com/nayuki/Reference-arithmetic-coding
 */



/// <summary>
/// A stream where bits can be written to. Because they are written to an underlying
/// byte stream, the end of the stream is padded with 0's up to a multiple of 8 bits.
/// The bits are written in big endian. Mutable and not thread-safe. </summary>
/// <seealso cref= BitInputStream </seealso>
public sealed class BitOutputStream : IDisposable
{

	/*---- Fields ----*/

	// The underlying byte stream to write to (not null).
	private Stream output;

	// The accumulated bits for the current byte, always in the range [0x00, 0xFF].
	private int currentByte;

	// Number of accumulated bits in the current byte, always between 0 and 7 (inclusive).
	private int numBitsFilled;



	/*---- Constructor ----*/

	/// <summary>
	/// Constructs a bit output stream based on the specified byte output stream. </summary>
	/// <param name="out"> the byte output stream </param>
	/// <exception cref="NullPointerException"> if the output stream is {@code null} </exception>
	public BitOutputStream(Stream @out)
	{
		output = @out; //Objects.requireNonNull(@out);
		currentByte = 0;
		numBitsFilled = 0;
	}



	/*---- Methods ----*/

	/// <summary>
	/// Writes a bit to the stream. The specified bit must be 0 or 1. </summary>
	/// <param name="b"> the bit to write, which must be 0 or 1 </param>
	/// <exception cref="IOException"> if an I/O exception occurred </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: public void write(int b) throws java.io.IOException
	public void write(int b)
	{
		if (b != 0 && b != 1)
		{
			throw new System.ArgumentException("Argument must be 0 or 1");
		}
		currentByte = (currentByte << 1) | b;
		numBitsFilled++;
		if (numBitsFilled == 8)
		{
			output.WriteByte((byte)currentByte);
			currentByte = 0;
			numBitsFilled = 0;
		}
	}


	/// <summary>
	/// Closes this stream and the underlying output stream. If called when this
	/// bit stream is not at a byte boundary, then the minimum number of "0" bits
	/// (between 0 and 7 of them) are written as padding to reach the next byte boundary. </summary>
	/// <exception cref="IOException"> if an I/O exception occurred </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: public void close() throws java.io.IOException
	public void close()
	{
		while (numBitsFilled != 0)
		{
			write(0);
		}
		output.Close();
	}

	public void Dispose()
	{
		throw new NotImplementedException();
	}
}
