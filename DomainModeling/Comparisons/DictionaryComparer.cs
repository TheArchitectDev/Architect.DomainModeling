using System.Diagnostics.CodeAnalysis;

namespace Architect.DomainModeling.Comparisons
{
	/// <summary>
	/// <para>
	/// Structurally compares <see cref="IReadOnlyDictionary{TKey, TValue}"/> or <see cref="IDictionary{TKey, TValue}"/> objects.
	/// </para>
	/// </summary>
	public static class DictionaryComparer
	{
		/// <summary>
		/// <para>
		/// Returns a hash code over some of the content of the given <paramref name="instance"/>.
		/// </para>
		/// </summary>
		public static int GetDictionaryHashCode<TKey, TValue>([AllowNull] IReadOnlyDictionary<TKey, TValue> instance)
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
		/// Returns a hash code over some of the content of the given <paramref name="instance"/>.
		/// </para>
		/// </summary>
		public static int GetDictionaryHashCode<TKey, TValue>([AllowNull] IDictionary<TKey, TValue> instance)
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
		/// Returns a hash code over some of the content of the given <paramref name="instance"/>.
		/// </para>
		/// </summary>
		public static int GetDictionaryHashCode<TKey, TValue>([AllowNull] Dictionary<TKey, TValue> instance)
			where TKey : notnull
		{
			return GetDictionaryHashCode((IReadOnlyDictionary<TKey, TValue>?)instance);
		}

		/// <summary>
		/// <para>
		/// Compares the given <see cref="IReadOnlyDictionary{TKey, TValue}"/> objects for equality by comparing their keys and values.
		/// </para>
		/// <para>
		/// This method performs equality checks on the keys and values.
		/// It is not recursive. To support nested collections, use custom collections that override their equality checks accordingly.
		/// </para>
		/// </summary>
		public static bool DictionaryEquals<TKey, TValue>([AllowNull] IReadOnlyDictionary<TKey, TValue> left, [AllowNull] IReadOnlyDictionary<TKey, TValue> right)
		{
			// Devirtualized path for practically all dictionaries
#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint. -- Was type-checked
			if (left is Dictionary<TKey, TValue> leftDict && right is Dictionary<TKey, TValue> rightDict)
				return DictionaryEquals(leftDict, rightDict);
#pragma warning restore CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.

			return GetResult(left, right);

			// Local function that performs the work
			static bool GetResult([AllowNull] IReadOnlyDictionary<TKey, TValue> left, [AllowNull] IReadOnlyDictionary<TKey, TValue> right)
			{
				if (ReferenceEquals(left, right)) return true;
				if (left is null || right is null) return false; // Double nulls are already handled above

				// EqualityComparer<T>.Default helps avoid an IEquatable<T> constraint yet still gets optimized: https://github.com/dotnet/coreclr/pull/14125

				foreach (var leftPair in left)
					if (!right.TryGetValue(leftPair.Key, out var rightValue) || !EqualityComparer<TValue>.Default.Equals(leftPair.Value, rightValue))
						return false;

				foreach (var rightPair in right)
					if (!left.TryGetValue(rightPair.Key, out var leftValue) || !EqualityComparer<TValue>.Default.Equals(rightPair.Value, leftValue))
						return false;

				return true;
			}
		}

		/// <summary>
		/// <para>
		/// Compares the given <see cref="IDictionary{TKey, TValue}"/> objects for equality by comparing their keys and values.
		/// </para>
		/// <para>
		/// This method performs equality checks on the keys and values.
		/// It is not recursive. To support nested collections, use custom collections that override their equality checks accordingly.
		/// </para>
		/// </summary>
		public static bool DictionaryEquals<TKey, TValue>([AllowNull] IDictionary<TKey, TValue> left, [AllowNull] IDictionary<TKey, TValue> right)
		{
			// Devirtualized path for practically all dictionaries
#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint. -- Was type-checked
			if (left is Dictionary<TKey, TValue> leftDict && right is Dictionary<TKey, TValue> rightDict)
				return DictionaryEquals(leftDict, rightDict);
#pragma warning restore CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.

			return GetResult(left, right);

			// Local function that performs the work
			static bool GetResult([AllowNull] IDictionary<TKey, TValue> left, [AllowNull] IDictionary<TKey, TValue> right)
			{
				if (ReferenceEquals(left, right)) return true;
				if (left is null || right is null) return false; // Double nulls are already handled above

				// EqualityComparer<T>.Default helps avoid an IEquatable<T> constraint yet still gets optimized: https://github.com/dotnet/coreclr/pull/14125

				foreach (var leftPair in left)
					if (!right.TryGetValue(leftPair.Key, out var rightValue) || !EqualityComparer<TValue>.Default.Equals(leftPair.Value, rightValue))
						return false;

				foreach (var rightPair in right)
					if (!left.TryGetValue(rightPair.Key, out var leftValue) || !EqualityComparer<TValue>.Default.Equals(rightPair.Value, leftValue))
						return false;

				return true;
			}
		}

		/// <summary>
		/// <para>
		/// Compares the given <see cref="Dictionary{TKey, TValue}"/> objects for equality by comparing their keys and values.
		/// </para>
		/// <para>
		/// This method performs equality checks on the keys and values.
		/// It is not recursive. To support nested collections, use custom collections that override their equality checks accordingly.
		/// </para>
		/// </summary>
		public static bool DictionaryEquals<TKey, TValue>([AllowNull] Dictionary<TKey, TValue> left, [AllowNull] Dictionary<TKey, TValue> right)
			where TKey : notnull
		{
			if (ReferenceEquals(left, right)) return true;
			if (left is null || right is null) return false; // Double nulls are already handled above

			// EqualityComparer<T>.Default helps avoid an IEquatable<T> constraint yet still gets optimized: https://github.com/dotnet/coreclr/pull/14125

			foreach (var leftPair in left)
				if (!right.TryGetValue(leftPair.Key, out var rightValue) || !EqualityComparer<TValue>.Default.Equals(leftPair.Value, rightValue))
					return false;

			foreach (var rightPair in right)
				if (!left.TryGetValue(rightPair.Key, out var leftValue) || !EqualityComparer<TValue>.Default.Equals(rightPair.Value, leftValue))
					return false;

			return true;
		}
	}
}
