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
/// Compression application using static arithmetic coding.
/// <para>Usage: java ArithmeticCompress InputFile OutputFile</para>
/// <para>Then use the corresponding "ArithmeticDecompress" application to recreate the original input file.</para>
/// <para>Note that the application uses an alphabet of 257 symbols - 256 symbols for the byte
/// values and 1 symbol for the EOF marker. The compressed file format starts with a list
/// of 256 symbol frequencies, and then followed by the arithmetic-coded data.</para>
/// </summary>
public class ArithmeticCompress
{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: public static void main(String[] args) throws java.io.IOException
	public static void Main(string[] args)
	{
		/* @@ PORT
		// Handle command line arguments
		if (args.Length != 2)
		{
			Console.Error.WriteLine("Usage: java ArithmeticCompress InputFile OutputFile");
			Environment.Exit(1);
			return;
		}
		File inputFile = new File(args[0]);
		File outputFile = new File(args[1]);

		// Read input file once to compute symbol frequencies
		FrequencyTable freqs = getFrequencies(inputFile);
		freqs.increment(256); // EOF symbol gets a frequency of 1

		// Read input file again, compress with arithmetic coding, and write output file
		using (Stream @in = new BufferedInputStream(new FileStream(inputFile, FileMode.Open, FileAccess.Read)), BitOutputStream @out = new BitOutputStream(new BufferedOutputStream(new FileStream(outputFile, FileMode.Create, FileAccess.Write))))
		{
			writeFrequencies(@out, freqs);
			compress(freqs, @in, @out);
		}
		*/
	}


	// Returns a frequency table based on the bytes in the given file.
	// Also contains an extra entry for symbol 256, whose frequency is set to 0.
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: private static FrequencyTable getFrequencies(java.io.File file) throws java.io.IOException
	private static FrequencyTable getFrequencies(string file)
	{
		

		FrequencyTable freqs = new SimpleFrequencyTable(new int[257]);
		using (Stream input = new BufferedStream( new FileStream(file, FileMode.Open, FileAccess.Read)))
		{
			while (true)
			{
				int b = input.ReadByte();
				if (b == -1)
				{
					break;
				}
				freqs.increment(b);
			}
		}
		return freqs;
	}


	// To allow unit testing, this method is package-private instead of private.
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: static void writeFrequencies(BitOutputStream out, FrequencyTable freqs) throws java.io.IOException
	internal static void writeFrequencies(BitOutputStream @out, FrequencyTable freqs)
	{
		for (int i = 0; i < 256; i++)
		{
			writeInt(@out, 32, freqs.get(i));
		}
	}


	// To allow unit testing, this method is package-private instead of private.
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: static void compress(FrequencyTable freqs, java.io.InputStream in, BitOutputStream out) throws java.io.IOException
	internal static void compress(FrequencyTable freqs, Stream @in, BitOutputStream @out)
	{
		ArithmeticEncoder enc = new ArithmeticEncoder(32, @out);
		while (true)
		{
			int symbol = @in.ReadByte();
			if (symbol == -1)
			{
				break;
			}
			enc.write(freqs, symbol);
		}
		enc.write(freqs, 256); // EOF
		enc.finish(); // Flush remaining code bits
	}


	// Writes an unsigned integer of the given bit width to the given stream.
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: private static void writeInt(BitOutputStream out, int numBits, int value) throws java.io.IOException
	private static void writeInt(BitOutputStream @out, int numBits, int value)
	{
		if (numBits < 0 || numBits > 32)
		{
			throw new System.ArgumentException();
		}

		for (int i = numBits - 1; i >= 0; i--)
		{
			@out.write(((int)((uint)value >> i)) & 1); // Big endian
		}
	}

}
