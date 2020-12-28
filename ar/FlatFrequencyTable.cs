/* 
 * Reference arithmetic coding
 * Copyright (c) Project Nayuki
 * 
 * https://www.nayuki.io/page/reference-arithmetic-coding
 * https://github.com/nayuki/Reference-arithmetic-coding
 */


/// <summary>
/// An immutable frequency table where every symbol has the same frequency of 1.
/// Useful as a fallback model when no statistics are available.
/// </summary>
public sealed class FlatFrequencyTable : FrequencyTable
{

	/*---- Fields ----*/

	// Total number of symbols, which is at least 1.
	private readonly int numSymbols;



	/*---- Constructor ----*/

	/// <summary>
	/// Constructs a flat frequency table with the specified number of symbols. </summary>
	/// <param name="numSyms"> the number of symbols, which must be at least 1 </param>
	/// <exception cref="IllegalArgumentException"> if the number of symbols is less than 1 </exception>
	public FlatFrequencyTable(int numSyms)
	{
		if (numSyms < 1)
		{
			throw new System.ArgumentException("Number of symbols must be positive");
		}
		numSymbols = numSyms;
	}



	/*---- Methods ----*/

	/// <summary>
	/// Returns the number of symbols in this table, which is at least 1. </summary>
	/// <returns> the number of symbols in this table </returns>
	public int SymbolLimit
	{
		get
		{
			return numSymbols;
		}
	}


	/// <summary>
	/// Returns the frequency of the specified symbol, which is always 1. </summary>
	/// <param name="symbol"> the symbol to query </param>
	/// <returns> the frequency of the symbol, which is 1 </returns>
	/// <exception cref="IllegalArgumentException"> if {@code symbol} &lt; 0 or {@code symbol} &ge; {@code getSymbolLimit()} </exception>
	public int get(int symbol)
	{
		checkSymbol(symbol);
		return 1;
	}


	/// <summary>
	/// Returns the total of all symbol frequencies, which is
	/// always equal to the number of symbols in this table. </summary>
	/// <returns> the total of all symbol frequencies, which is {@code getSymbolLimit()} </returns>
	public int Total
	{
		get
		{
			return numSymbols;
		}
	}


	/// <summary>
	/// Returns the sum of the frequencies of all the symbols strictly below
	/// the specified symbol value. The returned value is equal to {@code symbol}. </summary>
	/// <param name="symbol"> the symbol to query </param>
	/// <returns> the sum of the frequencies of all the symbols below {@code symbol}, which is {@code symbol} </returns>
	/// <exception cref="IllegalArgumentException"> if {@code symbol} &lt; 0 or {@code symbol} &ge; {@code getSymbolLimit()} </exception>
	public int getLow(int symbol)
	{
		checkSymbol(symbol);
		return symbol;
	}


	/// <summary>
	/// Returns the sum of the frequencies of the specified symbol and all
	/// the symbols below. The returned value is equal to {@code symbol + 1}. </summary>
	/// <param name="symbol"> the symbol to query </param>
	/// <returns> the sum of the frequencies of {@code symbol} and all symbols below, which is {@code symbol + 1} </returns>
	/// <exception cref="IllegalArgumentException"> if {@code symbol} &lt; 0 or {@code symbol} &ge; {@code getSymbolLimit()} </exception>
	public int getHigh(int symbol)
	{
		checkSymbol(symbol);
		return symbol + 1;
	}


	// Returns silently if 0 <= symbol < numSymbols, otherwise throws an exception.
	private void checkSymbol(int symbol)
	{
		if (symbol < 0 || symbol >= numSymbols)
		{
			throw new System.ArgumentException("Symbol out of range");
		}
	}


	/// <summary>
	/// Returns a string representation of this frequency table. The format is subject to change. </summary>
	/// <returns> a string representation of this frequency table </returns>
	public override string ToString()
	{
		return "FlatFrequencyTable=" + numSymbols;
	}


	/// <summary>
	/// Unsupported operation, because this frequency table is immutable. </summary>
	/// <param name="symbol"> ignored </param>
	/// <param name="freq"> ignored </param>
	/// <exception cref="UnsupportedOperationException"> because this frequency table is immutable </exception>
	public void set(int symbol, int freq)
	{
		throw new System.NotSupportedException();
	}


	/// <summary>
	/// Unsupported operation, because this frequency table is immutable. </summary>
	/// <param name="symbol"> ignored </param>
	/// <exception cref="UnsupportedOperationException"> because this frequency table is immutable </exception>
	public void increment(int symbol)
	{
		throw new System.NotSupportedException();
	}

}
