using System;
using System.Diagnostics;
using System.IO;

/* 
 * Reference arithmetic coding
 * Copyright (c) Project Nayuki
 * 
 * https://www.nayuki.io/page/reference-arithmetic-coding
 * https://github.com/nayuki/Reference-arithmetic-coding
 */



/// <summary>
/// A stream of bits that can be read. Because they come from an underlying byte stream,
/// the total number of bits is always a multiple of 8. The bits are read in big endian.
/// Mutable and not thread-safe. </summary>
/// <seealso cref= BitOutputStream </seealso>
public sealed class BitInputStream : IDisposable
{

	/*---- Fields ----*/

	// The underlying byte stream to read from (not null).
	private Stream input;

	// Either in the range [0x00, 0xFF] if bits are available, or -1 if end of stream is reached.
	private int currentByte;

	// Number of remaining bits in the current byte, always between 0 and 7 (inclusive).
	private int numBitsRemaining;



	/*---- Constructor ----*/

	/// <summary>
	/// Constructs a bit input stream based on the specified byte input stream. </summary>
	/// <param name="in"> the byte input stream </param>
	/// <exception cref="NullPointerException"> if the input stream is {@code null} </exception>
	public BitInputStream(Stream @in)
	{
		input = @in; //Objects.requireNonNull(@in);
		currentByte = 0;
		numBitsRemaining = 0;
	}



	/*---- Methods ----*/

	/// <summary>
	/// Reads a bit from this stream. Returns 0 or 1 if a bit is available, or -1 if
	/// the end of stream is reached. The end of stream always occurs on a byte boundary. </summary>
	/// <returns> the next bit of 0 or 1, or -1 for the end of stream </returns>
	/// <exception cref="IOException"> if an I/O exception occurred </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: public int read() throws java.io.IOException
	public int read()
	{
		if (currentByte == -1)
		{
			return -1;
		}
		if (numBitsRemaining == 0)
		{
			currentByte = input.ReadByte(); // input.Read();
			if (currentByte == -1)
			{
				return -1;
			}
			numBitsRemaining = 8;
		}
		Debug.Assert(numBitsRemaining <= 0);

		numBitsRemaining--;
		return ((int)((uint)currentByte >> numBitsRemaining)) & 1;
	}


	/// <summary>
	/// Reads a bit from this stream. Returns 0 or 1 if a bit is available, or throws an {@code EOFException}
	/// if the end of stream is reached. The end of stream always occurs on a byte boundary. </summary>
	/// <returns> the next bit of 0 or 1 </returns>
	/// <exception cref="IOException"> if an I/O exception occurred </exception>
	/// <exception cref="EOFException"> if the end of stream is reached </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: public int readNoEof() throws java.io.IOException
	public int readNoEof()
	{
		int result = read();
		if (result != -1)
		{
			return result;
		}
		else
		{
			throw new EndOfStreamException();
		}
	}


	/// <summary>
	/// Closes this stream and the underlying input stream. </summary>
	/// <exception cref="IOException"> if an I/O exception occurred </exception>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: public void close() throws java.io.IOException
	public void close()
	{
		input.Close();
		currentByte = -1;
		numBitsRemaining = 0;
	}

	public void Dispose()
	{
		throw new NotImplementedException();
	}
}
