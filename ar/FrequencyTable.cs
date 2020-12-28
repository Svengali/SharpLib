/* 
 * Reference arithmetic coding
 * Copyright (c) Project Nayuki
 * 
 * https://www.nayuki.io/page/reference-arithmetic-coding
 * https://github.com/nayuki/Reference-arithmetic-coding
 */


/// <summary>
/// A table of symbol frequencies. The table holds data for symbols numbered from 0
/// to getSymbolLimit()&minus;1. Each symbol has a frequency, which is a non-negative integer.
/// <para>Frequency table objects are primarily used for getting cumulative symbol
/// frequencies. These objects can be mutable depending on the implementation.
/// The total of all symbol frequencies must not exceed Integer.MAX_VALUE.</para>
/// </summary>
public interface FrequencyTable
{

	/// <summary>
	/// Returns the number of symbols in this frequency table, which is a positive number. </summary>
	/// <returns> the number of symbols in this frequency table </returns>
	int SymbolLimit {get;}


	/// <summary>
	/// Returns the frequency of the specified symbol. The returned value is at least 0. </summary>
	/// <param name="symbol"> the symbol to query </param>
	/// <returns> the frequency of the symbol </returns>
	/// <exception cref="IllegalArgumentException"> if the symbol is out of range </exception>
	int get(int symbol);


	/// <summary>
	/// Sets the frequency of the specified symbol to the specified value.
	/// The frequency value must be at least 0. </summary>
	/// <param name="symbol"> the symbol to set </param>
	/// <param name="freq"> the frequency value to set </param>
	/// <exception cref="IllegalArgumentException"> if the frequency is negative or the symbol is out of range </exception>
	/// <exception cref="ArithmeticException"> if an arithmetic overflow occurs </exception>
	void set(int symbol, int freq);


	/// <summary>
	/// Increments the frequency of the specified symbol. </summary>
	/// <param name="symbol"> the symbol whose frequency to increment </param>
	/// <exception cref="IllegalArgumentException"> if the symbol is out of range </exception>
	/// <exception cref="ArithmeticException"> if an arithmetic overflow occurs </exception>
	void increment(int symbol);


	/// <summary>
	/// Returns the total of all symbol frequencies. The returned value is at
	/// least 0 and is always equal to {@code getHigh(getSymbolLimit() - 1)}. </summary>
	/// <returns> the total of all symbol frequencies </returns>
	int Total {get;}


	/// <summary>
	/// Returns the sum of the frequencies of all the symbols strictly
	/// below the specified symbol value. The returned value is at least 0. </summary>
	/// <param name="symbol"> the symbol to query </param>
	/// <returns> the sum of the frequencies of all the symbols below {@code symbol} </returns>
	/// <exception cref="IllegalArgumentException"> if the symbol is out of range </exception>
	int getLow(int symbol);


	/// <summary>
	/// Returns the sum of the frequencies of the specified symbol
	/// and all the symbols below. The returned value is at least 0. </summary>
	/// <param name="symbol"> the symbol to query </param>
	/// <returns> the sum of the frequencies of {@code symbol} and all symbols below </returns>
	/// <exception cref="IllegalArgumentException"> if the symbol is out of range </exception>
	int getHigh(int symbol);

}
