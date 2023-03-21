using System.Diagnostics.CodeAnalysis;
using Architect.DomainModeling.Comparisons;
using Xunit;

namespace Architect.DomainModeling.Tests.Comparisons;

public class DictionaryComparerTests
{
	[return: NotNullIfNotNull(nameof(keys))]
	private static Dictionary<TKey, string>? CreateDictionaryWithEqualityComparer<TKey>(IEnumerable<TKey>? keys, IEqualityComparer<TKey> comparer)
		where TKey : notnull
	{
		if (keys is null)
			return null;

		var result = new Dictionary<TKey, string>(comparer);
		foreach (var key in keys)
			result[key] = "";
		return result;
	}

	private static void AssertGetHashCodesEqual<TKey, TValue>(bool expectedResult, IReadOnlyDictionary<TKey, TValue>? left, IReadOnlyDictionary<TKey, TValue>? right)
	{
		var leftHashCode = DictionaryComparer.GetDictionaryHashCode(left);
		var rightHashCode = DictionaryComparer.GetDictionaryHashCode(right);

		var result = leftHashCode == rightHashCode;

		// Dictionaries are order-agnostic and our implementation avoids reading the entire thing
		// This may lead to results different from the equality check

		// If the objects are equal, then the hash codes must be too (i.e. no false negatives)
		if (expectedResult) Assert.Equal(expectedResult, result);
	}

	private static void InterfaceAlternativesReturn<TKey, TValue>(bool expectedResult, Dictionary<TKey, TValue>? left, Dictionary<TKey, TValue>? right)
		where TKey : notnull
		where TValue : IEquatable<TValue>
	{
		Assert.Equal(expectedResult, DictionaryComparer.DictionaryEquals((IDictionary<TKey, TValue>?)left, (IDictionary<TKey, TValue>?)right));
		Assert.Equal(expectedResult, DictionaryComparer.DictionaryEquals((IReadOnlyDictionary<TKey, TValue>?)left, (IReadOnlyDictionary<TKey, TValue>?)right));
	}

	[Theory]
	[InlineData(null, null, true)]
	[InlineData(null, "", false)]
	[InlineData("", null, false)]
	[InlineData("", "", true)]
	[InlineData("A", "A", true)]
	[InlineData("A", "a", true)]
	[InlineData("A", "AA", false)]
	public void DictionaryEquals_WithStringsAndIgnoreCaseComparer_ShouldReturnExpectedResult(string one, string two, bool expectedResult)
	{
		var left = CreateDictionaryWithEqualityComparer(one is null ? null : new[] { one }, StringComparer.OrdinalIgnoreCase);
		var right = CreateDictionaryWithEqualityComparer(two is null ? null : new[] { two }, StringComparer.OrdinalIgnoreCase);

		var result = DictionaryComparer.DictionaryEquals(left, right);

		Assert.Equal(expectedResult, result);
		InterfaceAlternativesReturn(result, left, right);
		AssertGetHashCodesEqual(result, left, right);
	}

	[Fact]
	public void DictionaryEquals_WithoutTwoWayEquality_ShouldReturnExpectedResult()
	{
		var left = CreateDictionaryWithEqualityComparer(new[] { "A", "a", }, StringComparer.Ordinal);
		var right = CreateDictionaryWithEqualityComparer(new[] { "A", }, StringComparer.Ordinal);

		if (left is null || right is null)
			return; // Implementation does not support custom comparer

		var result = DictionaryComparer.DictionaryEquals(left, right);

		Assert.False(result);
		InterfaceAlternativesReturn(result, left, right);
		AssertGetHashCodesEqual(result, left, right);
	}

	[Fact]
	public void DictionaryEquals_WithIgnoreCaseWithTwoWayEquality_ShouldReturnExpectedResult()
	{
		var left = CreateDictionaryWithEqualityComparer(new[] { "A", "a", }, StringComparer.OrdinalIgnoreCase);
		var right = CreateDictionaryWithEqualityComparer(new[] { "A", }, StringComparer.OrdinalIgnoreCase);

		if (left is null || right is null)
			return; // Implementation does not support custom comparer

		var result = DictionaryComparer.DictionaryEquals(left, right);

		Assert.True(result);
		InterfaceAlternativesReturn(result, left, right);
	}

	[Fact]
	public void DictionaryEquals_WithDifferentCaseComparersWithoutTwoWayEquality_ShouldReturnExpectedResult()
	{
		var left = CreateDictionaryWithEqualityComparer(new[] { "a", }, StringComparer.Ordinal);
		var right = CreateDictionaryWithEqualityComparer(new[] { "A", }, StringComparer.OrdinalIgnoreCase);

		if (left is null || right is null)
			return; // Implementation does not support custom comparer

		var result = DictionaryComparer.DictionaryEquals(left, right);

		Assert.False(result);
		InterfaceAlternativesReturn(result, left, right);
		AssertGetHashCodesEqual(result, left, right);
	}

	[Fact]
	public void DictionaryEquals_WithDifferentCaseComparersWithTwoWayEquality_ShouldReturnExpectedResult()
	{
		var left = CreateDictionaryWithEqualityComparer(new[] { "A", "a", }, StringComparer.Ordinal);
		var right = CreateDictionaryWithEqualityComparer(new[] { "A", }, StringComparer.OrdinalIgnoreCase);

		if (left is null || right is null)
			return; // Implementation does not support custom comparer

		var result = DictionaryComparer.DictionaryEquals(left, right);

		Assert.True(result);
		InterfaceAlternativesReturn(result, left, right);
		AssertGetHashCodesEqual(result, left, right);
	}

	[Theory]
	[InlineData("", "", true)]
	[InlineData("A", "", false)]
	[InlineData("", "A", false)]
	[InlineData("A", "A", true)]
	[InlineData("A", "a", false)]
	[InlineData("a", "A", false)]
	[InlineData("A", "B", false)]
	public void DictionaryEquals_WithSameKeys_ShouldReturnExpectedResultBasedOnValue(string leftValue, string rightValue, bool expectedResult)
	{
		var left = new Dictionary<int, string>() { [1] = leftValue };
		var right = new Dictionary<int, string>() { [1] = rightValue };

		var result = DictionaryComparer.DictionaryEquals(left, right);

		Assert.Equal(expectedResult, result);
		InterfaceAlternativesReturn(result, left, right);
		AssertGetHashCodesEqual(result, left, right);
	}

	[Fact]
	public void DictionaryEquals_WithSameDataInDifferentOrdering_ShouldReturnExpectedResult()
	{
		var left = new Dictionary<int, string>() { [1] = "A", [2] = "B", };
		var right = new Dictionary<int, string>() { [2] = "B", [1] = "A", };

		var result = DictionaryComparer.DictionaryEquals(left, right);

		Assert.True(result);
		InterfaceAlternativesReturn(result, left, right);
		AssertGetHashCodesEqual(result, left, right);
	}
}
