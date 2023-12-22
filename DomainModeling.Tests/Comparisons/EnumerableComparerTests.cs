using System.Collections;
using System.Collections.Immutable;
using Architect.DomainModeling.Comparisons;
using Architect.DomainModeling.Tests.Comparisons.EnumerableComparerTestTypes;
using Xunit;

namespace Architect.DomainModeling.Tests.Comparisons
{
	namespace Implementations
	{
		public sealed class ArrayComparerTests : EnumerableComparerTests
		{
			protected override IEnumerable<T> CreateCollectionCore<T>(IEnumerable<T> elements) => elements.ToArray();
		}

		public sealed class ListComparerTests : EnumerableComparerTests
		{
			protected override IEnumerable<T> CreateCollectionCore<T>(IEnumerable<T> elements) => elements.ToList();
		}

		public sealed class ImmutableArrayComparerTests : EnumerableComparerTests
		{
			protected override IEnumerable<T> CreateCollectionCore<T>(IEnumerable<T> elements) => elements.ToImmutableArray();
		}

		public sealed class CustomListComparerTests : EnumerableComparerTests
		{
			protected override IEnumerable<T> CreateCollectionCore<T>(IEnumerable<T> elements) => new CustomList<T>(elements.ToList());

			private sealed class CustomList<T> : IList<T>
			{
				private IList<T> WrappedList { get; } = new List<T>();
				public T this[int index]
				{
					get => this.WrappedList[index];
					set => this.WrappedList[index] = value;
				}
				public int Count => this.WrappedList.Count;
				public bool IsReadOnly => this.WrappedList.IsReadOnly;
				public void Add(T item) => this.WrappedList.Add(item);
				public IEnumerator<T> GetEnumerator() => this.WrappedList.GetEnumerator();
				IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
				public void Clear() => throw new NotSupportedException();
				public bool Contains(T item) => throw new NotSupportedException();
				public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
				public int IndexOf(T item) => throw new NotSupportedException();
				public void Insert(int index, T item) => throw new NotSupportedException();
				public bool Remove(T item) => throw new NotSupportedException();
				public void RemoveAt(int index) => throw new NotSupportedException();

				public CustomList(List<T> list)
				{
					this.WrappedList = list;
				}
			}
		}

		public sealed class CustomReadOnlySetComparerTests : EnumerableComparerTests
		{
			protected override IEnumerable<T> CreateCollectionCore<T>(IEnumerable<T> elements) => new CustomReadOnlySet<T>(elements.ToList(), Comparer<T>.Default);
			protected override IEnumerable<T> CreateCollectionWithEqualityComparer<T>(IEnumerable<T> elements, IComparer<T> comparer) =>
				new CustomReadOnlySet<T>(elements.ToList(), comparer);

			private sealed class CustomReadOnlySet<T> : IReadOnlySet<T>
			{
				private ISet<T> WrappedSet { get; }
				public int Count => this.WrappedSet.Count;
				public IEnumerator<T> GetEnumerator() => this.WrappedSet.GetEnumerator();
				IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
				public bool Contains(T item) => this.WrappedSet.Contains(item);
				public bool IsProperSubsetOf(IEnumerable<T> other) => this.WrappedSet.IsProperSubsetOf(other);
				public bool IsProperSupersetOf(IEnumerable<T> other) => this.WrappedSet.IsProperSupersetOf(other);
				public bool IsSubsetOf(IEnumerable<T> other) => this.WrappedSet.IsSubsetOf(other);
				public bool IsSupersetOf(IEnumerable<T> other) => this.WrappedSet.IsSupersetOf(other);
				public bool Overlaps(IEnumerable<T> other) => this.WrappedSet.Overlaps(other);
				public bool SetEquals(IEnumerable<T> other) => this.WrappedSet.SetEquals(other);

				public CustomReadOnlySet(IEnumerable<T> list, IComparer<T> comparer)
				{
					this.WrappedSet = new SortedSet<T>(list, comparer);
				}
			}
		}

		public sealed class CustomReadOnlyCollectionComparerTests : EnumerableComparerTests
		{
			protected override IEnumerable<T> CreateCollectionCore<T>(IEnumerable<T> elements) => new CustomReadOnlyCollection<T>(elements.ToList());

			private sealed class CustomReadOnlyCollection<T> : IReadOnlyCollection<T>
			{
				private IList<T> WrappedList { get; } = new List<T>();
				public int Count => this.WrappedList.Count;
				public IEnumerator<T> GetEnumerator() => this.WrappedList.GetEnumerator();
				IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

				public CustomReadOnlyCollection(List<T> list)
				{
					this.WrappedList = list;
				}
			}
		}

		public sealed class CustomEnumerableComparerTests : EnumerableComparerTests
		{
			protected override IEnumerable<T> CreateCollectionCore<T>(IEnumerable<T> elements) => new CustomEnumerable<T>(elements.ToList());

			private sealed class CustomEnumerable<T> : IEnumerable<T>
			{
				private IList<T> WrappedList { get; } = new List<T>();
				public IEnumerator<T> GetEnumerator() => this.WrappedList.GetEnumerator();
				IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

				public CustomEnumerable(List<T> list)
				{
					this.WrappedList = list;
				}
			}
		}
	}

	public abstract class EnumerableComparerTests
	{
		protected abstract IEnumerable<T> CreateCollectionCore<T>(IEnumerable<T> elements);

		protected IEnumerable<T> CreateCollection<T>(T singleElement)
		{
			return this.CreateCollectionCore(new[] { singleElement });
		}

		protected virtual IEnumerable<T>? CreateCollectionWithEqualityComparer<T>(IEnumerable<T> elements, IComparer<T> comparer)
		{
			return null;
		}

		private static void AssertGetHashCodesEqual<T>(bool expectedResult, IEnumerable<T> left, IEnumerable<T> right)
		{
			var leftHashCode = EnumerableComparer.GetEnumerableHashCode(left);
			var rightHashCode = EnumerableComparer.GetEnumerableHashCode(right);

			var result = leftHashCode == rightHashCode;

			// As our hash code implementation avoids allocations, we cannot enumerate certain types
			// This may lead to results different from the equality check

			// For types where we can be thorough enough, we expect a matching hash code
			if (left is IList<T> && right is IList<T>)
			{
				Assert.Equal(expectedResult, result);
			}
			// For other types, at least we expect no false negatives
			else
			{
				// If the objects are equal, then the hash codes must be too (i.e. no false negatives)
				if (expectedResult) Assert.Equal(expectedResult, result);

				// If the objects are not equal, then the hash codes could still be equal
			}
		}

		private static void AssertBoxingAlternativesReturn<T>(bool expectedResult, IEnumerable<T> left, IEnumerable<T> right)
		{
			// The non-generic IEnumerable comparer does not have the ability to special-case generic types
			if (left is not IReadOnlySet<T> && right is not IReadOnlySet<T>)
			{
#pragma warning disable IDE0004 // Cast is not redundant, but actually changes the resolved overload
				Assert.Equal(expectedResult, EnumerableComparer.EnumerableEquals((IEnumerable)left, (IEnumerable)right));
#pragma warning restore IDE0004
			}
		}

		[Theory]
		[InlineData(null, null, true)]
		[InlineData(null, "", false)]
		[InlineData("", null, false)]
		[InlineData("", "", true)]
		[InlineData("A", "A", true)]
		[InlineData("A", "a", false)]
		[InlineData("A", "AA", false)]
		public void EnumerableEquals_WithStrings_ShouldReturnExpectedResult(string? one, string? two, bool expectedResult)
		{
			var left = this.CreateCollection(one);
			var right = this.CreateCollection(two);

			var result = EnumerableComparer.EnumerableEquals(left, right);

			Assert.Equal(expectedResult, result);
			AssertBoxingAlternativesReturn(result, left, right);
			AssertGetHashCodesEqual(result, left, right);
		}

		[Theory]
		[InlineData(null, null, true)]
		[InlineData(null, "", false)]
		[InlineData("", null, false)]
		[InlineData("", "", true)]
		[InlineData("A", "A", true)]
		[InlineData("A", "a", false)]
		[InlineData("A", "AA", false)]
		public void EnumerableEquals_WithStringWrapperValueObjectsWithOrdinal_ShouldReturnExpectedResult(string? one, string? two, bool expectedResult)
		{
			var left = this.CreateCollection(one is null ? null : new StringWrapperValueObject(one, StringComparison.Ordinal));
			var right = this.CreateCollection(two is null ? null : new StringWrapperValueObject(two, StringComparison.Ordinal));

			var result = EnumerableComparer.EnumerableEquals(left, right);

			Assert.Equal(expectedResult, result);
			AssertBoxingAlternativesReturn(result, left, right);
			AssertGetHashCodesEqual(result, left, right);
		}

		[Theory]
		[InlineData(null, null, true)]
		[InlineData(null, "", false)]
		[InlineData("", null, false)]
		[InlineData("", "", true)]
		[InlineData("A", "A", true)]
		[InlineData("A", "a", true)]
		[InlineData("A", "AA", false)]
		public void EnumerableEquals_WithStringWrapperValueObjectsWithOrdinalIgnoreCase_ShouldReturnExpectedResult(string? one, string? two, bool expectedResult)
		{
			var left = this.CreateCollection(one is null ? null : new StringWrapperValueObject(one, StringComparison.OrdinalIgnoreCase));
			var right = this.CreateCollection(two is null ? null : new StringWrapperValueObject(two, StringComparison.OrdinalIgnoreCase));

			var result = EnumerableComparer.EnumerableEquals(left, right);

			Assert.Equal(expectedResult, result);
			AssertBoxingAlternativesReturn(result, left, right);
			AssertGetHashCodesEqual(result, left, right);
		}

		[Theory]
		[InlineData(null, null, true)]
		[InlineData(null, "", true)]
		[InlineData("", null, true)]
		[InlineData("", "", true)]
		[InlineData("A", "A", true)]
		[InlineData("A", "a", false)]
		[InlineData("A", "AA", false)]
		public void EnumerableEquals_WithStringIdentities_ShouldReturnExpectedResult(string? one, string? two, bool expectedResult)
		{
			var left = this.CreateCollection((SomeStringId)one);
			var right = this.CreateCollection((SomeStringId)two);

			var result = EnumerableComparer.EnumerableEquals(left, right);

			Assert.Equal(expectedResult, result);
			AssertBoxingAlternativesReturn(result, left, right);
			AssertGetHashCodesEqual(result, left, right);
		}

		[Theory]
		[InlineData(null, null, true)]
		[InlineData(null, "", false)]
		[InlineData("", null, false)]
		[InlineData("", "", true)]
		[InlineData("A", "A", true)]
		[InlineData("A", "a", true)]
		[InlineData("A", "AA", false)]
		public void EnumerableEquals_WithStringsAndIgnoreCaseComparer_ShouldReturnExpectedResult(string? one, string? two, bool expectedResult)
		{
			var left = this.CreateCollectionWithEqualityComparer(new[] { one }, StringComparer.OrdinalIgnoreCase);
			var right = this.CreateCollectionWithEqualityComparer(new[] { two }, StringComparer.OrdinalIgnoreCase);

			if (left is null || right is null)
				return; // Implementation does not support custom comparer

			var result = EnumerableComparer.EnumerableEquals(left, right);

			Assert.Equal(expectedResult, result);
			AssertBoxingAlternativesReturn(result, left, right);
			AssertGetHashCodesEqual(result, left, right);
		}

		[Fact]
		public void EnumerableEquals_WithoutTwoWayEquality_ShouldReturnExpectedResult()
		{
			var left = this.CreateCollectionWithEqualityComparer(new[] { "A", "a", }, StringComparer.Ordinal);
			var right = this.CreateCollectionWithEqualityComparer(new[] { "A", }, StringComparer.Ordinal);

			if (left is null || right is null)
				return; // Implementation does not support custom comparer

			var result = EnumerableComparer.EnumerableEquals(left, right);

			Assert.False(result);
			AssertBoxingAlternativesReturn(result, left, right);
			AssertGetHashCodesEqual(result, left, right);
		}

		[Fact]
		public void EnumerableEquals_WithIgnoreCaseWithTwoWayEquality_ShouldReturnExpectedResult()
		{
			var left = this.CreateCollectionWithEqualityComparer(new[] { "A", "a", }, StringComparer.OrdinalIgnoreCase);
			var right = this.CreateCollectionWithEqualityComparer(new[] { "A", }, StringComparer.OrdinalIgnoreCase);

			if (left is null || right is null)
				return; // Implementation does not support custom comparer

			var result = EnumerableComparer.EnumerableEquals(left, right);

			Assert.True(result);
		}

		[Fact]
		public void EnumerableEquals_WithDifferentCaseComparersWithoutTwoWayEquality_ShouldReturnExpectedResult()
		{
			var left = this.CreateCollectionWithEqualityComparer(new[] { "a", }, StringComparer.Ordinal);
			var right = this.CreateCollectionWithEqualityComparer(new[] { "A", }, StringComparer.OrdinalIgnoreCase);

			if (left is null || right is null)
				return; // Implementation does not support custom comparer

			var result = EnumerableComparer.EnumerableEquals(left, right);

			Assert.False(result);
			AssertBoxingAlternativesReturn(result, left, right);
			AssertGetHashCodesEqual(result, left, right);
		}

		[Fact]
		public void EnumerableEquals_WithDifferentCaseComparersWithTwoWayEquality_ShouldReturnExpectedResult()
		{
			var left = this.CreateCollectionWithEqualityComparer(new[] { "A", "a", }, StringComparer.Ordinal);
			var right = this.CreateCollectionWithEqualityComparer(new[] { "A", }, StringComparer.OrdinalIgnoreCase);

			if (left is null || right is null)
				return; // Implementation does not support custom comparer

			var result = EnumerableComparer.EnumerableEquals(left, right);

			Assert.True(result);
			AssertBoxingAlternativesReturn(result, left, right);
			AssertGetHashCodesEqual(result, left, right);
		}

		[Theory]
		[InlineData(null, null, true)]
		[InlineData(null, "", false)]
		[InlineData("", null, false)]
		[InlineData("", "", true)]
		[InlineData("A", "A", true)]
		[InlineData("A", "a", false)]
		[InlineData("A", "AA", false)]
		public void GetMemoryHashCode_BetweenInstances_ShouldReturnExpectedResult(string? one, string? two, bool expectedResult)
		{
			var left = new[] { one, }.AsMemory();
			var right = new[] { two, }.AsMemory();

			var leftHashCode = EnumerableComparer.GetMemoryHashCode(left);
			var rightHashCode = EnumerableComparer.GetMemoryHashCode(right);

			Assert.Equal(expectedResult, leftHashCode == rightHashCode);
		}

		[Theory]
		[InlineData(null, null, true)]
		[InlineData(null, "", false)]
		[InlineData("", null, false)]
		[InlineData("", "", true)]
		[InlineData("A", "A", true)]
		[InlineData("A", "a", false)]
		[InlineData("A", "AA", false)]
		public void GetMemoryHashCode_BetweenNullableInstances_ShouldReturnExpectedResult(string? one, string? two, bool expectedResult)
		{
			var left = one is null ? null : (Memory<string>?)new[] { one, }.AsMemory();
			var right = two is null ? null : (Memory<string>?)new[] { two, }.AsMemory();

			var leftHashCode = EnumerableComparer.GetMemoryHashCode(left);
			var rightHashCode = EnumerableComparer.GetMemoryHashCode(right);

			Assert.Equal(expectedResult, leftHashCode == rightHashCode);
		}

		[Theory]
		[InlineData(null, null, true)]
		[InlineData(null, "", false)]
		[InlineData("", null, false)]
		[InlineData("", "", true)]
		[InlineData("A", "A", true)]
		[InlineData("A", "a", false)]
		[InlineData("A", "AA", false)]
		public void GetSpanHashCode_BetweenNullableInstances_ShouldReturnExpectedResult(string? one, string? two, bool expectedResult)
		{
			var left = new[] { one, }.AsSpan();
			var right = new[] { two }.AsSpan();

			var leftHashCode = EnumerableComparer.GetSpanHashCode(left);
			var rightHashCode = EnumerableComparer.GetSpanHashCode(right);

			Assert.Equal(expectedResult, leftHashCode == rightHashCode);
		}

		private sealed class StringIdEntity : Entity<SomeStringId, string>
		{
			public StringIdEntity(SomeStringId id)
				: base(id)
			{
			}
		}
	}

	// Use a namespace, since our source generators dislike nested types
	namespace EnumerableComparerTestTypes
	{
		[WrapperValueObject<string>]
		public sealed partial class StringWrapperValueObject : IComparable<StringWrapperValueObject>
		{
			protected sealed override StringComparison StringComparison { get; }

			public StringWrapperValueObject(string value, StringComparison stringComparison)
			{
				this.Value = value ?? throw new ArgumentNullException(nameof(value));
				this.StringComparison = stringComparison;
			}
		}
	}
}
