using System.Diagnostics.CodeAnalysis;

namespace Architect.DomainModeling.Comparisons
{
	/// <summary>
	/// <para>
	/// Structurally compares <see cref="ILookup{TKey, TElement}"/> objects.
	/// </para>
	/// </summary>
	public static class LookupComparer
	{
		/// <summary>
		/// <para>
		/// Returns a hash code over some of the content of the given <paramref name="instance"/>.
		/// </para>
		/// </summary>
		public static int GetLookupHashCode<TKey, TElement>([AllowNull] ILookup<TKey, TElement> instance)
		{
			// Unfortunately, we can do no better than distinguish between null, empty, and non-empty
			// Example:
			// Left instance contains keys { "A", "a" } and has StringComparer.Ordinal
			// Right instance contains keys { "A" } and has StringComparer.OrdinalIgnoreCase
			// Both have the same keys
			// Each considers the other equal, because every query on each results in the same result
			// (Enumeration results in different results, but enumeration does not count, as such types have no ordering guarantees)
			// Equal objects MUST produce equal hash codes
			// But without knowledge of each other, there is no reliable way for the two to produce the same hash code

			if (instance is null) return 0;
			if (instance.Count == 0) return 1;
			return 2;
		}

		/// <summary>
		/// <para>
		/// Compares the given <see cref="ILookup{TKey, TElement}"/> objects for equality by comparing their elements.
		/// </para>
		/// </summary>
		public static bool LookupEquals<TKey, TElement>([AllowNull] ILookup<TKey, TElement> left, [AllowNull] ILookup<TKey, TElement> right)
		{
			if (ReferenceEquals(left, right)) return true;
			if (left is null || right is null) return false; // Double nulls are already handled above

			// The lookups must be equal from the perspective of each
			return LeftLeadingEquals(left, right) && LeftLeadingEquals(right, left);

			// Local function that compares two lookups from the perspective of the left one
			static bool LeftLeadingEquals(ILookup<TKey, TElement> left, ILookup<TKey, TElement> right)
			{
				foreach (var leftGroup in left)
				{
					var rightGroup = right[leftGroup.Key];

					// Fast path
					if (leftGroup is IList<TElement> leftList && rightGroup is IList<TElement> rightList)
					{
						if (leftList.Count != rightList.Count) return false;

						// EqualityComparer<T>.Default helps avoid an IEquatable<T> constraint yet still gets optimized: https://github.com/dotnet/coreclr/pull/14125
						for (var i = 0; i < leftList.Count; i++)
							if (!EqualityComparer<TElement>.Default.Equals(leftList[i], rightList[i]))
								return false;
					}
					// Slow path
					else
					{
						if (!ElementEnumerableEquals(leftGroup, rightGroup))
							return false;
					}
				}

				return true;
			}

			// Local function that compares two groups of elements by enumerating them
			static bool ElementEnumerableEquals(IEnumerable<TElement> leftGroup, IEnumerable<TElement> rightGroup)
			{
				using var rightGroupEnumerator = rightGroup.GetEnumerator();

				foreach (var leftElement in leftGroup)
					if (!rightGroupEnumerator.MoveNext() || !EqualityComparer<TElement>.Default.Equals(leftElement, rightGroupEnumerator.Current))
						return false;

				if (rightGroupEnumerator.MoveNext()) return false;

				return true;
			}
		}
	}
}
