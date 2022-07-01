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
/// Compression application using prediction by partial matching (PPM) with arithmetic coding.
/// <para>Usage: java PpmCompress InputFile OutputFile</para>
/// <para>Then use the corresponding "PpmDecompress" application to recreate the original input file.</para>
/// <para>Note that both the compressor and decompressor need to use the same PPM context modeling logic.
/// The PPM algorithm can be thought of as a powerful generalization of adaptive arithmetic coding.</para>
/// </summary>
public sealed class PpmCompress
{

	// Must be at least -1 and match PpmDecompress. Warning: Exponential memory usage at O(257^n).
	private const int MODEL_ORDER = 3;


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: public static void main(String[] args) throws java.io.IOException
	public static void Main(string[] args)
	{
		/* @@@@ PORT
		// Handle command line arguments
		if (args.Length != 2)
		{
			Console.Error.WriteLine("Usage: java PpmCompress InputFile OutputFile");
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
		// Set up encoder and model. In this PPM model, symbol 256 represents EOF;
		// its frequency is 1 in the order -1 context but its frequency
		// is 0 in all other contexts (which have non-negative order).
		ArithmeticEncoder enc = new ArithmeticEncoder(32, @out);
		PpmModel model = new PpmModel(MODEL_ORDER, 257, 256);
		int[] history = new int[0];

		while (true)
		{
			// Read and encode one byte
			int symbol = @in.ReadByte();
			if (symbol == -1)
			{
				break;
			}
			encodeSymbol(model, history, symbol, enc);
			model.incrementContexts(history, symbol);

			if (model.modelOrder >= 1)
			{
				// Prepend current symbol, dropping oldest symbol if necessary
				if (history.Length < model.modelOrder)
				{
					history = Arrays.CopyOf(history, history.Length + 1);
				}
				Array.Copy(history, 0, history, 1, history.Length - 1);
				history[0] = symbol;
			}
		}

		encodeSymbol(model, history, 256, enc); // EOF
		enc.finish(); // Flush remaining code bits
	}


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: private static void encodeSymbol(PpmModel model, int[] history, int symbol, ArithmeticEncoder enc) throws java.io.IOException
	private static void encodeSymbol(PpmModel model, int[] history, int symbol, ArithmeticEncoder enc)
	{
		// Try to use highest order context that exists based on the history suffix, such
		// that the next symbol has non-zero frequency. When symbol 256 is produced at a context
		// at any non-negative order, it means "escape to the next lower order with non-empty
		// context". When symbol 256 is produced at the order -1 context, it means "EOF".
		for (int order = history.Length; order >= 0; order--)
		{
			PpmModel.Context ctx = model.rootContext;
			for (int i = 0; i < order; i++)
			{
				Debug.Assert(ctx.subcontexts == null);

				ctx = ctx.subcontexts[history[i]];
				if (ctx == null)
				{
					goto outerContinue;
				}
			}
			if (symbol != 256 && ctx.frequencies.get(symbol) > 0)
			{
				enc.write(ctx.frequencies, symbol);
				return;
			}
			// Else write context escape symbol and continue decrementing the order
			enc.write(ctx.frequencies, 256);
			outerContinue:;
		}

		//outerBreak:
		// Logic for order = -1
		enc.write(model.orderMinus1Freqs, symbol);
	}

}
