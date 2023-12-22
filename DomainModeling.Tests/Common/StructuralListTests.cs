using System.Collections.Immutable;
using Architect.DomainModeling.Generator.Common;
using Xunit;

namespace Architect.DomainModeling.Tests.Common;

public sealed class StructuralListTests
{
	[Theory]
	[InlineData("", "")]
	[InlineData("A", "A")]
	[InlineData("ABC", "ABC")]
	[InlineData("abc", "abc")]
	public void Equals_WithEqualElements_ShouldReturnTrue(string leftChars, string rightChars)
	{
		var left = new StructuralList<ImmutableArray<char>, char>([.. leftChars]);
		var right = new StructuralList<ImmutableArray<char>, char>([.. rightChars]);

		Assert.Equal(left, right);
	}

	[Theory]
	[InlineData("", " ")]
	[InlineData(" ", "")]
	[InlineData("A", "B")]
	[InlineData("A", " A")]
	[InlineData(" A", "A")]
	[InlineData(" A", "A ")]
	[InlineData("A ", " A")]
	[InlineData("ABC", "abc")]
	[InlineData("abc", "ABC")]
	public void Equals_WithUnequalElements_ShouldReturnFalse(string leftChars, string rightChars)
	{
		var left = new StructuralList<ImmutableArray<char>, char>([.. leftChars]);
		var right = new StructuralList<ImmutableArray<char>, char>([.. rightChars]);

		Assert.NotEqual(left, right);
	}

	/// <summary>
	/// Although technically two unequal objects could have the same hash code, we can test better by constraining our test set and pretending that the hash codes should then be unequal too.
	/// </summary>
	[Theory]
	[InlineData("", "")]
	[InlineData("A", "A")]
	[InlineData("ABC", "ABC")]
	[InlineData("abc", "abc")]
	[InlineData("", " ")]
	[InlineData(" ", "")]
	[InlineData("A", "B")]
	[InlineData("A", " A")]
	[InlineData(" A", "A")]
	[InlineData(" A", "A ")]
	[InlineData("A ", " A")]
	[InlineData("ABC", "abc")]
	[InlineData("abc", "ABC")]
	public void GetHashCode_BetweenTwoCollections_ShouldMatchTheyEquality(string leftChars, string rightChars)
	{
		var left = new StructuralList<ImmutableArray<char>, char>([.. leftChars]);
		var right = new StructuralList<ImmutableArray<char>, char>([.. rightChars]);

		var expectedResult = left.Equals(right);

		var result = left.GetHashCode().Equals(right.GetHashCode());

		Assert.Equal(expectedResult, result);
	}
}
