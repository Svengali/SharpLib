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
/// Compression application using adaptive arithmetic coding.
/// <para>Usage: java AdaptiveArithmeticCompress InputFile OutputFile</para>
/// <para>Then use the corresponding "AdaptiveArithmeticDecompress" application to recreate the original input file.</para>
/// <para>Note that the application starts with a flat frequency table of 257 symbols (all set to a frequency of 1),
/// and updates it after each byte encoded. The corresponding decompressor program also starts with a flat
/// frequency table and updates it after each byte decoded. It is by design that the compressor and
/// decompressor have synchronized states, so that the data can be decompressed properly.</para>
/// </summary>
public class AdaptiveArithmeticCompress
{

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: public static void main(String[] args) throws java.io.IOException
	public static void Main(string[] args)
	{
		/* @@@@ PORT
		// Handle command line arguments
		if (args.Length != 2)
		{
			Console.Error.WriteLine("Usage: java AdaptiveArithmeticCompress InputFile OutputFile");
			Environment.Exit(1);
			return;
		}
		File inputFile = new File(args[0]);
		File outputFile = new File(args[1]);

		// Perform file compression
		using (Stream @in = new BufferedInputStream(new FileStream(inputFile, FileMode.Open, FileAccess.Read)), BitOutputStream @out = new BitOutputStream(new BufferedOutputStream(new FileStream(outputFile, FileMode.Create, FileAccess.Write))))
		{
			compress(@in, @out);
		}
		*/
	}


	// To allow unit testing, this method is package-private instead of private.
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: static void compress(java.io.InputStream in, BitOutputStream out) throws java.io.IOException
	internal static void compress(Stream @in, BitOutputStream @out)
	{
		FlatFrequencyTable initFreqs = new FlatFrequencyTable(257);
		FrequencyTable freqs = new SimpleFrequencyTable(initFreqs);
		ArithmeticEncoder enc = new ArithmeticEncoder(32, @out);
		while (true)
		{
			// Read and encode one byte
			int symbol = @in.ReadByte();
			if (symbol == -1)
			{
				break;
			}
			enc.write(freqs, symbol);
			freqs.increment(symbol);
		}
		enc.write(freqs, 256); // EOF
		enc.finish(); // Flush remaining code bits
	}

}
