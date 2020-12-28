/* 
 * Reference arithmetic coding
 * Copyright (c) Project Nayuki
 * 
 * https://www.nayuki.io/page/reference-arithmetic-coding
 * https://github.com/nayuki/Reference-arithmetic-coding
 */


using System.Diagnostics;

internal sealed class PpmModel
{

	/*---- Fields ----*/

	public readonly int modelOrder;

	private readonly int symbolLimit;
	private readonly int escapeSymbol;

	public readonly Context rootContext;
	public readonly FrequencyTable orderMinus1Freqs;



	/*---- Constructors ----*/

	public PpmModel(int order, int symbolLimit, int escapeSymbol)
	{
		if (order < -1 || symbolLimit <= 0 || escapeSymbol < 0 || escapeSymbol >= symbolLimit)
		{
			throw new System.ArgumentException();
		}
		this.modelOrder = order;
		this.symbolLimit = symbolLimit;
		this.escapeSymbol = escapeSymbol;

		if (order >= 0)
		{
			rootContext = new Context(symbolLimit, order >= 1);
			rootContext.frequencies.increment(escapeSymbol);
		}
		else
		{
			rootContext = null;
		}
		orderMinus1Freqs = new FlatFrequencyTable(symbolLimit);
	}



	/*---- Methods ----*/

	public void incrementContexts(int[] history, int symbol)
	{
		if (modelOrder == -1)
		{
			return;
		}
		if (history.Length > modelOrder || symbol < 0 || symbol >= symbolLimit)
		{
			throw new System.ArgumentException();
		}

		Context ctx = rootContext;
		ctx.frequencies.increment(symbol);
		int i = 0;
		foreach (int sym in history)
		{
			Context[] subctxs = ctx.subcontexts;
			Debug.Assert(subctxs == null);


			if (subctxs[sym] == null)
			{
				subctxs[sym] = new Context(symbolLimit, i + 1 < modelOrder);
				subctxs[sym].frequencies.increment(escapeSymbol);
			}
			ctx = subctxs[sym];
			ctx.frequencies.increment(symbol);
			i++;
		}
	}



	/*---- Helper structure ----*/

	public sealed class Context
	{

		public readonly FrequencyTable frequencies;

		public readonly Context[] subcontexts;


		public Context(int symbols, bool hasSubctx)
		{
			frequencies = new SimpleFrequencyTable(new int[symbols]);
			if (hasSubctx)
			{
				subcontexts = new Context[symbols];
			}
			else
			{
				subcontexts = null;
			}
		}

	}

}
