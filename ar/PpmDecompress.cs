﻿using System;
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
/// Decompression application using prediction by partial matching (PPM) with arithmetic coding.
/// <para>Usage: java PpmDecompress InputFile OutputFile</para>
/// <para>This decompresses files generated by the "PpmCompress" application.</para>
/// </summary>
public sealed class PpmDecompress
{

	// Must be at least -1 and match PpmCompress. Warning: Exponential memory usage at O(257^n).
	private const int MODEL_ORDER = 3;


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: public static void main(String[] args) throws java.io.IOException
	public static void Main(string[] args)
	{
		// Handle command line arguments
		if (args.Length != 2)
		{
			Console.Error.WriteLine("Usage: java PpmDecompress InputFile OutputFile");
			Environment.Exit(1);
			return;
		}

		string inputFile = args[0]; //new File(args[0]);
		string outputFile =args[1]; //new File(args[1]);

		// Perform file decompression
		using( BitInputStream @in = new BitInputStream(new BufferedStream(new FileStream(inputFile, FileMode.Open, FileAccess.Read))))
		using( Stream @out = new BufferedStream( new FileStream(outputFile, FileMode.Create, FileAccess.Write)))
		{
			decompress(@in, @out);
		}
	}


	// To allow unit testing, this method is package-private instead of private.
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: static void decompress(BitInputStream in, java.io.OutputStream out) throws java.io.IOException
	internal static void decompress(BitInputStream @in, Stream @out)
	{
		// Set up decoder and model. In this PPM model, symbol 256 represents EOF;
		// its frequency is 1 in the order -1 context but its frequency
		// is 0 in all other contexts (which have non-negative order).
		ArithmeticDecoder dec = new ArithmeticDecoder(32, @in);
		PpmModel model = new PpmModel(MODEL_ORDER, 257, 256);
		int[] history = new int[0];

		while (true)
		{
			// Decode and write one byte
			int symbol = decodeSymbol(dec, model, history);
			if (symbol == 256) // EOF symbol
			{
				break;
			}
			@out.WriteByte((byte)symbol);
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
	}


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in C#:
//ORIGINAL LINE: private static int decodeSymbol(ArithmeticDecoder dec, PpmModel model, int[] history) throws java.io.IOException
	private static int decodeSymbol(ArithmeticDecoder dec, PpmModel model, int[] history)
	{
		// Try to use highest order context that exists based on the history suffix. When symbol 256
		// is consumed at a context at any non-negative order, it means "escape to the next lower order
		// with non-empty context". When symbol 256 is consumed at the order -1 context, it means "EOF".
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
			int symbol = dec.read(ctx.frequencies);
			if (symbol < 256)
			{
				return symbol;
			}
			// Else we read the context escape symbol, so continue decrementing the order
			outerContinue:;
		}

		//outerBreak:
		// Logic for order = -1
		return dec.read(model.orderMinus1Freqs);
	}

}
