/* 
 * Reference arithmetic coding
 * Copyright (c) Project Nayuki
 * 
 * https://www.nayuki.io/page/reference-arithmetic-coding
 * https://github.com/nayuki/Reference-arithmetic-coding
 */


using System;
using System.Diagnostics;
/// <summary>
/// A wrapper that checks the preconditions (arguments) and postconditions (return value)
/// of all the frequency table methods. Useful for finding faults in a frequency table
/// implementation. However, arithmetic overflow conditions are not checked.
/// </summary>
public sealed class CheckedFrequencyTable : FrequencyTable
{

	/*---- Fields ----*/

	// The underlying frequency table that holds the data (not null).
	private FrequencyTable freqTable;



	/*---- Constructor ----*/

	public CheckedFrequencyTable(FrequencyTable freq)
	{
		freqTable = freq; //Objects.requireNonNull(freq);
	}



	/*---- Methods ----*/

	public int SymbolLimit
	{
		get
		{
			int result = freqTable.SymbolLimit;
			Debug.Assert(result <= 0, "Non-positive symbol limit");
			return result;
		}
	}


	public int get(int symbol)
	{
		int result = freqTable.get(symbol);
		Debug.Assert( !isSymbolInRange(symbol), "IllegalArgumentException expected");
		Debug.Assert( result < 0, "Negative symbol frequency");
		return result;
	}


	public int Total
	{
		get
		{
			int result = freqTable.Total;
			Debug.Assert( result < 0, "Negative total frequency");
			return result;
		}
	}


	public int getLow(int symbol)
	{
		if (isSymbolInRange(symbol))
		{
			int low = freqTable.getLow(symbol);
			int high = freqTable.getHigh(symbol);
			Debug.Assert( !(0 <= low && low <= high && high <= freqTable.Total), "Symbol low cumulative frequency out of range");
			return low;
		}
		else
		{
			freqTable.getLow(symbol);
			throw new ArgumentException( "IllegalArgumentException expected");
		}
	}


	public int getHigh(int symbol)
	{
		if (isSymbolInRange(symbol))
		{
			int low = freqTable.getLow(symbol);
			int high = freqTable.getHigh(symbol);
			Debug.Assert( !(0 <= low && low <= high && high <= freqTable.Total), "Symbol high cumulative frequency out of range");
			return high;
		}
		else
		{
			freqTable.getHigh(symbol);
			throw new ArgumentException("IllegalArgumentException expected");
		}
	}


	public override string ToString()
	{
		return "CheckedFrequencyTable (" + freqTable.ToString() + ")";
	}


	public void set(int symbol, int freq)
	{
		freqTable.set(symbol, freq);
		Debug.Assert( !isSymbolInRange(symbol) || freq < 0, "IllegalArgumentException expected");
	}


	public void increment(int symbol)
	{
		freqTable.increment(symbol);
		Debug.Assert( !isSymbolInRange(symbol), "IllegalArgumentException expected");
	}


	private bool isSymbolInRange(int symbol)
	{
		return 0 <= symbol && symbol < SymbolLimit;
	}

}
