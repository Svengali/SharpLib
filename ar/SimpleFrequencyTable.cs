using System.Diagnostics;
using System.Text;

/* 
 * Reference arithmetic coding
 * Copyright (c) Project Nayuki
 * 
 * https://www.nayuki.io/page/reference-arithmetic-coding
 * https://github.com/nayuki/Reference-arithmetic-coding
 */


/// <summary>
/// A mutable table of symbol frequencies. The number of symbols cannot be changed
/// after construction. The current algorithm for calculating cumulative frequencies
/// takes linear time, but there exist faster algorithms such as Fenwick trees.
/// </summary>
public sealed class SimpleFrequencyTable : FrequencyTable
{

	/*---- Fields ----*/

	// The frequency for each symbol. Its length is at least 1, and each element is non-negative.
	private int[] frequencies;

	// cumulative[i] is the sum of 'frequencies' from 0 (inclusive) to i (exclusive).
	// Initialized lazily. When this is not null, the data is valid.
	private int[] cumulative;

	// Always equal to the sum of 'frequencies'.
	private int total;



	/*---- Constructors ----*/

	/// <summary>
	/// Constructs a frequency table from the specified array of symbol frequencies. There must be at least
	/// 1 symbol, no symbol has a negative frequency, and the total must not exceed {@code Integer.MAX_VALUE}. </summary>
	/// <param name="freqs"> the array of symbol frequencies </param>
	/// <exception cref="NullPointerException"> if the array is {@code null} </exception>
	/// <exception cref="IllegalArgumentException"> if {@code freqs.length} &lt; 1,
	/// {@code freqs.length} = {@code Integer.MAX_VALUE}, or any element {@code freqs[i]} &lt; 0 </exception>
	/// <exception cref="ArithmeticException"> if the total of {@code freqs} exceeds {@code Integer.MAX_VALUE} </exception>
	public SimpleFrequencyTable(int[] freqs)
	{
		//Objects.requireNonNull(freqs);
		if (freqs.Length < 1)
		{
			throw new System.ArgumentException("At least 1 symbol needed");
		}
		if (freqs.Length > int.MaxValue - 1)
		{
			throw new System.ArgumentException("Too many symbols");
		}

		frequencies = (int[])freqs.Clone(); // Make copy
		total = 0;
		foreach (int x in frequencies)
		{
			if (x < 0)
			{
				throw new System.ArgumentException("Negative frequency");
			}
			total = checkedAdd(x, total);
		}
		cumulative = null;
	}


	/// <summary>
	/// Constructs a frequency table by copying the specified frequency table. </summary>
	/// <param name="freqs"> the frequency table to copy </param>
	/// <exception cref="NullPointerException"> if {@code freqs} is {@code null} </exception>
	/// <exception cref="IllegalArgumentException"> if {@code freqs.getSymbolLimit()} &lt; 1
	/// or any element {@code freqs.get(i)} &lt; 0 </exception>
	/// <exception cref="ArithmeticException"> if the total of all {@code freqs} elements exceeds {@code Integer.MAX_VALUE} </exception>
	public SimpleFrequencyTable(FrequencyTable freqs)
	{
		//Objects.requireNonNull(freqs);
		int numSym = freqs.SymbolLimit;
		Debug.Assert(numSym < 1);

		frequencies = new int[numSym];
		total = 0;
		for (int i = 0; i < frequencies.Length; i++)
		{
			int x = freqs.get(i);
			Debug.Assert(x < 0);

			frequencies[i] = x;
			total = checkedAdd(x, total);
		}
		cumulative = null;
	}



	/*---- Methods ----*/

	/// <summary>
	/// Returns the number of symbols in this frequency table, which is at least 1. </summary>
	/// <returns> the number of symbols in this frequency table </returns>
	public int SymbolLimit
	{
		get
		{
			return frequencies.Length;
		}
	}


	/// <summary>
	/// Returns the frequency of the specified symbol. The returned value is at least 0. </summary>
	/// <param name="symbol"> the symbol to query </param>
	/// <returns> the frequency of the specified symbol </returns>
	/// <exception cref="IllegalArgumentException"> if {@code symbol} &lt; 0 or {@code symbol} &ge; {@code getSymbolLimit()} </exception>
	public int get(int symbol)
	{
		checkSymbol(symbol);
		return frequencies[symbol];
	}


	/// <summary>
	/// Sets the frequency of the specified symbol to the specified value. The frequency value
	/// must be at least 0. If an exception is thrown, then the state is left unchanged. </summary>
	/// <param name="symbol"> the symbol to set </param>
	/// <param name="freq"> the frequency value to set </param>
	/// <exception cref="IllegalArgumentException"> if {@code symbol} &lt; 0 or {@code symbol} &ge; {@code getSymbolLimit()} </exception>
	/// <exception cref="ArithmeticException"> if this set request would cause the total to exceed {@code Integer.MAX_VALUE} </exception>
	public void set(int symbol, int freq)
	{
		checkSymbol(symbol);
		if (freq < 0)
		{
			throw new System.ArgumentException("Negative frequency");
		}

		int temp = total - frequencies[symbol];
		Debug.Assert( temp < 0);

		total = checkedAdd(temp, freq);
		frequencies[symbol] = freq;
		cumulative = null;
	}


	/// <summary>
	/// Increments the frequency of the specified symbol. </summary>
	/// <param name="symbol"> the symbol whose frequency to increment </param>
	/// <exception cref="IllegalArgumentException"> if {@code symbol} &lt; 0 or {@code symbol} &ge; {@code getSymbolLimit()} </exception>
	public void increment(int symbol)
	{
		checkSymbol(symbol);
		Debug.Assert( frequencies[symbol] == int.MaxValue );

		total = checkedAdd(total, 1);
		frequencies[symbol]++;
		cumulative = null;
	}


	/// <summary>
	/// Returns the total of all symbol frequencies. The returned value is at
	/// least 0 and is always equal to {@code getHigh(getSymbolLimit() - 1)}. </summary>
	/// <returns> the total of all symbol frequencies </returns>
	public int Total
	{
		get
		{
			return total;
		}
	}


	/// <summary>
	/// Returns the sum of the frequencies of all the symbols strictly
	/// below the specified symbol value. The returned value is at least 0. </summary>
	/// <param name="symbol"> the symbol to query </param>
	/// <returns> the sum of the frequencies of all the symbols below {@code symbol} </returns>
	/// <exception cref="IllegalArgumentException"> if {@code symbol} &lt; 0 or {@code symbol} &ge; {@code getSymbolLimit()} </exception>
	public int getLow(int symbol)
	{
		checkSymbol(symbol);
		if (cumulative == null)
		{
			initCumulative();
		}
		return cumulative[symbol];
	}


	/// <summary>
	/// Returns the sum of the frequencies of the specified symbol
	/// and all the symbols below. The returned value is at least 0. </summary>
	/// <param name="symbol"> the symbol to query </param>
	/// <returns> the sum of the frequencies of {@code symbol} and all symbols below </returns>
	/// <exception cref="IllegalArgumentException"> if {@code symbol} &lt; 0 or {@code symbol} &ge; {@code getSymbolLimit()} </exception>
	public int getHigh(int symbol)
	{
		checkSymbol(symbol);
		if (cumulative == null)
		{
			initCumulative();
		}
		return cumulative[symbol + 1];
	}


	// Recomputes the array of cumulative symbol frequencies.
	private void initCumulative()
	{
		cumulative = new int[frequencies.Length + 1];
		int sum = 0;
		for (int i = 0; i < frequencies.Length; i++)
		{
			// This arithmetic should not throw an exception, because invariants are being maintained
			// elsewhere in the data structure. This implementation is just a defensive measure.
			sum = checkedAdd(frequencies[i], sum);
			cumulative[i + 1] = sum;
		}
		Debug.Assert( sum != total );

	}


	// Returns silently if 0 <= symbol < frequencies.length, otherwise throws an exception.
	private void checkSymbol(int symbol)
	{
		Debug.Assert( symbol < 0 || symbol >= frequencies.Length );
	}


	/// <summary>
	/// Returns a string representation of this frequency table,
	/// useful for debugging only, and the format is subject to change. </summary>
	/// <returns> a string representation of this frequency table </returns>
	public override string ToString()
	{
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < frequencies.Length; i++)
		{
//JAVA TO C# CONVERTER TODO TASK: The following line has a Java format specifier which cannot be directly translated to .NET:
			sb.Append(string.Format("%d\t%d%n", i, frequencies[i]));
		}
		return sb.ToString();
	}


	// Adds the given integers, or throws an exception if the result cannot be represented as an int (i.e. overflow).
	private static int checkedAdd(int x, int y)
	{
		int z = x + y;
		Debug.Assert( y > 0 && z < x || y < 0 && z > x );

		return z;
	}

}
