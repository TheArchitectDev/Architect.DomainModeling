using Architect.DomainModeling.Comparisons;
using Xunit;

namespace Architect.DomainModeling.Tests.Comparisons;

public class LookupComparerTests
{
	private static ILookup<TKey, string> CreateLookupWithEqualityComparer<TKey>(IEnumerable<TKey> keys, IEqualityComparer<TKey> comparer)
	{
		if (keys is null) return null;

		// This approach avoids duplicate values, which we do not want for the tests that use this method
		// For example, if "A" and "a" are added to an ignore-case lookup, only "A" is added, but we would give it two dummy values, which we wish to avoid
		var result = keys
			.GroupBy(key => key, comparer)
			.ToLookup(group => group.Key, _ => "", comparer);

		return result;
	}

	private static void AssertGetHashCodesEqual<TKey, TValue>(bool expectedResult, ILookup<TKey, TValue> left, ILookup<TKey, TValue> right)
	{
		var leftHashCode = LookupComparer.GetLookupHashCode(left);
		var rightHashCode = LookupComparer.GetLookupHashCode(right);

		var result = leftHashCode == rightHashCode;

		// Lookups are order-agnostic and our implementation avoids reading the entire thing
		// This may lead to results different from the equality check

		// If the objects are equal, then the hash codes must be too (i.e. no false negatives)
		if (expectedResult) Assert.Equal(expectedResult, result);
	}

	/// <summary>
	/// If this is no longer true, for example because <see cref="IReadOnlyList{T}"/> is used instead, then we should adjust the "fast path" in the various <see cref="LookupComparer"/> equality methods accordingly.
	/// </summary>
	[Fact]
	public void LookupGrouping_Regularly_ShouldImplementIList()
	{
		var lookup = new int[] { 1 }.ToLookup(i => i);

		var grouping = lookup.Single();

		Assert.True(grouping is IList<int>);
	}

	[Theory]
	[InlineData(null, null, true)]
	[InlineData(null, "", false)]
	[InlineData("", null, false)]
	[InlineData("", "", true)]
	[InlineData("A", "A", true)]
	[InlineData("A", "a", true)]
	[InlineData("A", "AA", false)]
	public void LookupEquals_WithStringsAndIgnoreCaseComparer_ShouldReturnExpectedResult(string one, string two, bool expectedResult)
	{
		var left = CreateLookupWithEqualityComparer(one is null ? null : new[] { one }, StringComparer.OrdinalIgnoreCase);
		var right = CreateLookupWithEqualityComparer(two is null ? null : new[] { two }, StringComparer.OrdinalIgnoreCase);

		var result = LookupComparer.LookupEquals(left, right);

		Assert.Equal(expectedResult, result);
		AssertGetHashCodesEqual(result, left, right);
	}

	[Fact]
	public void LookupEquals_WithoutTwoWayEquality_ShouldReturnExpectedResult()
	{
		var left = CreateLookupWithEqualityComparer(new[] { "A", "a", }, StringComparer.Ordinal);
		var right = CreateLookupWithEqualityComparer(new[] { "A", }, StringComparer.Ordinal);

		if (left is null || right is null)
			return; // Implementation does not support custom comparer

		var result = LookupComparer.LookupEquals(left, right);

		Assert.False(result);
		AssertGetHashCodesEqual(result, left, right);
	}

	[Fact]
	public void LookupEquals_WithIgnoreCaseWithTwoWayEquality_ShouldReturnExpectedResult()
	{
		var left = CreateLookupWithEqualityComparer(new[] { "A", "a", }, StringComparer.OrdinalIgnoreCase);
		var right = CreateLookupWithEqualityComparer(new[] { "A", }, StringComparer.OrdinalIgnoreCase);

		if (left is null || right is null)
			return; // Implementation does not support custom comparer

		var result = LookupComparer.LookupEquals(left, right);

		Assert.True(result);
	}

	[Fact]
	public void LookupEquals_WithDifferentCaseComparersWithoutTwoWayEquality_ShouldReturnExpectedResult()
	{
		var left = CreateLookupWithEqualityComparer(new[] { "a", }, StringComparer.Ordinal);
		var right = CreateLookupWithEqualityComparer(new[] { "A", }, StringComparer.OrdinalIgnoreCase);

		if (left is null || right is null)
			return; // Implementation does not support custom comparer

		var result = LookupComparer.LookupEquals(left, right);

		Assert.False(result);
		AssertGetHashCodesEqual(result, left, right);
	}

	[Fact]
	public void LookupEquals_WithDifferentCaseComparersWithTwoWayEquality_ShouldReturnExpectedResult()
	{
		var left = CreateLookupWithEqualityComparer(new[] { "A", "a", }, StringComparer.Ordinal);
		var right = CreateLookupWithEqualityComparer(new[] { "A", }, StringComparer.OrdinalIgnoreCase);

		if (left is null || right is null)
			return; // Implementation does not support custom comparer

		var result = LookupComparer.LookupEquals(left, right);

		Assert.True(result);
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
	[InlineData("A", "A,A", false)]
	[InlineData("A,A", "A", false)]
	[InlineData("A,B", "B,A", false)] // Element order matters
	public void LookupEquals_WithSameKeys_ShouldReturnExpectedResultBasedOnValues(string leftValueString, string rightValueString, bool expectedResult)
	{
		var leftValues = leftValueString.Split(",");
		var rightValues = rightValueString.Split(",");

		var left = leftValues.ToLookup(value => 1);
		var right = rightValues.ToLookup(value => 1);

		var result = LookupComparer.LookupEquals(left, right);

		Assert.Equal(expectedResult, result);
		AssertGetHashCodesEqual(result, left, right);
	}

	[Fact]
	public void DictionaryEquals_WithSameDataInDifferentKeyOrdering_ShouldReturnExpectedResult()
	{
		var left = new[] { (1, "A"), (1, "B"), (2, "C"), }.ToLookup(pair => pair.Item1, pair => pair.Item2);
		var right = new[] { (2, "C"), (1, "A"), (1, "B"), }.ToLookup(pair => pair.Item1, pair => pair.Item2);

		var result = LookupComparer.LookupEquals(left, right);

		Assert.True(result); // Key order does not matter
		AssertGetHashCodesEqual(result, left, right);
	}

	[Fact]
	public void DictionaryEquals_WithSameDataInDifferentElementOrdering_ShouldReturnExpectedResult()
	{
		var left = new[] { (1, "A"), (1, "B"), (2, "C"), }.ToLookup(pair => pair.Item1, pair => pair.Item2);
		var right = new[] { (1, "B"), (1, "A"), (2, "C"), }.ToLookup(pair => pair.Item1, pair => pair.Item2);

		var result = LookupComparer.LookupEquals(left, right);

		Assert.False(result); // Element order matters
		AssertGetHashCodesEqual(result, left, right);
	}
}
