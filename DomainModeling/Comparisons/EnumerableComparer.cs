using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Architect.DomainModeling.Comparisons;

/// <summary>
/// <para>
/// Structurally compares <see cref="IEnumerable"/>, <see cref="ReadOnlyMemory{T}"/>, or <see cref="ReadOnlySpan{T}"/> objects.
/// </para>
/// </summary>
public static class EnumerableComparer
{
	/// <summary>
	/// <para>
	/// Returns a hash code over some of the content of the given <see cref="IEnumerable"/>.
	/// </para>
	/// </summary>
	public static int GetEnumerableHashCode([AllowNull] IEnumerable enumerable)
	{
		// Use 0 for null
		if (enumerable is null)
			return 0;
		// Otherwise, we have no efficient (allocation-free, full-enumeration-free) hash code
		else
			return -1;
	}

	/// <summary>
	/// <para>
	/// Returns a hash code over some of the content of the given <see cref="IEnumerable{T}"/>.
	/// </para>
	/// </summary>
	public static int GetEnumerableHashCode<TElement>([AllowNull] IEnumerable<TElement> enumerable)
	{
		// We use IList rather than IReadOnlyList because certain types (such as ILookup's grouping) neglect to implement IReadOnlyList
		// We use IReadOnlyCollection because certain materialized types are immutable
		// TODO Enhancement: Change IList to IReadOnlyList once the former implements the latter: https://github.com/dotnet/runtime/issues/31001

		// Prefer to use the count, first element, and last element (if non-empty IList)
		if (enumerable is IList<TElement> list && list.Count > 0)
		{
			return HashCode.Combine(list.Count, list[0], list[^1]);
		}
		// Prefer to use the count, first element, and last element (if non-empty IReadOnlyList)
		if (enumerable is IReadOnlyList<TElement> readOnlyList && readOnlyList.Count > 0)
		{
			return HashCode.Combine(readOnlyList.Count, readOnlyList[0], readOnlyList[^1]);
		}
		// Avoid producing false negatives for sets, which may be order-agnostic or dictate the equality comparer
		if (enumerable is IReadOnlySet<TElement> set)
		{
			// See DictionaryComparer.GetDictionaryHashCode()
			if (set is null) return 0;
			if (set.Count == 0) return 1;
			return 2;
		}
		// Prefer to use the count (if IReadOnlyCollection)
		if (enumerable is IReadOnlyCollection<TElement> collection)
		{
			unchecked
			{
				return 1 + collection.Count; // Keep 0 available for null
			}
		}

		return GetEnumerableHashCode((IEnumerable?)enumerable);
	}

	/// <summary>
	/// <para>
	/// Compares the given <see cref="IEnumerable{T}"/> objects for equality by comparing their elements.
	/// </para>
	/// <para>
	/// This method performs equality checks on the <see cref="IEnumerable{T}"/>'s elements.
	/// It is not recursive. To support nested collections, use custom collections that override their equality checks accordingly.
	/// </para>
	/// </summary>
	public static bool EnumerableEquals<TElement>([AllowNull] IEnumerable<TElement> left, [AllowNull] IEnumerable<TElement> right)
	{
		// We use IList rather than IReadOnlyList because certain types (such as ILookup's grouping) neglect to implement IReadOnlyList
		// We use IReadOnlyCollection because certain materialized types are immutable
		// TODO Enhancement: Change IList to IReadOnlyList once the former implements the latter: https://github.com/dotnet/runtime/issues/31001

		if (ReferenceEquals(left, right)) return true;
		if (left is null || right is null) return false; // Double nulls are already handled above

		// Prefer common concrete types, to avoid (possibly many) virtualized calls
		if (left is List<TElement> leftList && right is List<TElement> rightList)
			return MemoryExtensions.SequenceEqual(System.Runtime.InteropServices.CollectionsMarshal.AsSpan(leftList), System.Runtime.InteropServices.CollectionsMarshal.AsSpan(rightList));
		if (left is TElement[] leftArray && right is TElement[] rightArray)
			return MemoryExtensions.SequenceEqual(leftArray.AsSpan(), rightArray.AsSpan());
		if (left is System.Collections.Immutable.ImmutableArray<TElement> leftImmutableArray && right is System.Collections.Immutable.ImmutableArray<TElement> rightImmutableArray)
			return MemoryExtensions.SequenceEqual(leftImmutableArray.AsSpan(), rightImmutableArray.AsSpan());

		// Prefer to index directly, to avoid allocation of an enumerator
		if (left is IList<TElement> leftIndexable && right is IList<TElement> rightIndexable)
			return IndexableEquals(leftIndexable, rightIndexable);

		// Honor sets, which may be order-agnostic or dictate the equality comparer
		if (left is IReadOnlySet<TElement> leftSet && right is IReadOnlySet<TElement> rightSet)
			return SetEquals(leftSet, rightSet);

		// Optimize for materialized collections
		if (left is IReadOnlyCollection<TElement> leftCollection && right is IReadOnlyCollection<TElement> rightCollection && leftCollection.Count != rightCollection.Count)
			return false;

		return GenericEnumerableEquals(left, right);

		// Local function that compares indexable collections
		static bool IndexableEquals(IList<TElement> leftIndexable, IList<TElement> rightIndexable)
		{
			var leftIndexableCount = leftIndexable.Count;

			if (leftIndexableCount != rightIndexable.Count) return false;

			// EqualityComparer<T>.Default helps avoid an IEquatable<T> constraint yet still gets optimized: https://github.com/dotnet/coreclr/pull/14125
			for (var i = 0; i < leftIndexableCount; i++)
				if (!EqualityComparer<TElement>.Default.Equals(leftIndexable[i], rightIndexable[i]))
					return false;

			return true;
		}

		// Local function that compares sets
		static bool SetEquals(IReadOnlySet<TElement> leftSet, IReadOnlySet<TElement> rightSet)
		{
			foreach (var leftElement in leftSet)
				if (!rightSet.Contains(leftElement))
					return false;

			foreach (var rightElement in rightSet)
				if (!leftSet.Contains(rightElement))
					return false;

			return true;
		}

		// Local function that compares generic enumerables
		static bool GenericEnumerableEquals(IEnumerable<TElement> leftEnumerable, IEnumerable<TElement> rightEnumerable)
		{
			// EqualityComparer<T>.Default helps avoid an IEquatable<T> constraint yet still gets optimized: https://github.com/dotnet/coreclr/pull/14125
			using var rightEnumerator = rightEnumerable.GetEnumerator();
			foreach (var leftElement in leftEnumerable)
				if (!rightEnumerator.MoveNext() || !EqualityComparer<TElement>.Default.Equals(leftElement, rightEnumerator.Current))
					return false;
			if (rightEnumerator.MoveNext()) return false;

			return true;
		}
	}

	/// <summary>
	/// <para>
	/// Compares the given <see cref="IEnumerable"/> objects for equality by comparing their elements.
	/// </para>
	/// <para>
	/// This method performs equality checks on the <see cref="IEnumerable{T}"/>'s elements.
	/// It is not recursive. To support nested collections, use custom collections that override their equality checks accordingly.
	/// </para>
	/// <para>
	/// <strong>This non-generic overload should be avoided if possible.</strong>
	/// It lacks the ability to special-case generic types, which may lead to unexpected results.
	/// For example, two <see cref="HashSet{T}"/> instances with an ignore-case comparer may consider each other equal despite having different-cased contents.
	/// However, the current method has no knowledge of their comparers or their order-agnosticism, and may return a different result.
	/// </para>
	/// <para>
	/// Unlike <see cref="EnumerableEquals{TElement}"/>, this method may cause boxing of elements that are of a value type.
	/// </para>
	/// </summary>
	public static bool EnumerableEquals([AllowNull] IEnumerable left, [AllowNull] IEnumerable right)
	{
		if (ReferenceEquals(left, right)) return true;
		if (left is null || right is null) return false; // Double nulls are already handled above

		var rightEnumerator = right.GetEnumerator();
		using (rightEnumerator as IDisposable)
		{
			foreach (var leftElement in left)
				if (!rightEnumerator.MoveNext() || !Equals(leftElement, rightEnumerator.Current))
					return false;
			if (rightEnumerator.MoveNext()) return false;
		}

		return true;
	}

	/// <summary>
	/// <para>
	/// Returns a hash code over some of the content of the given <see cref="Memory{T}"/> wrapped in a <see cref="Nullable{T}"/>.
	/// </para>
	/// <para>
	/// For a corresponding equality check, use <see cref="MemoryExtensions.SequenceEqual{T}(ReadOnlySpan{T}, ReadOnlySpan{T})"/>.
	/// </para>
	/// </summary>
	public static int GetMemoryHashCode<TElement>(Memory<TElement>? memory)
	{
		return GetMemoryHashCode((ReadOnlyMemory<TElement>?)memory);
	}

	/// <summary>
	/// <para>
	/// Returns a hash code over some of the content of the given <see cref="ReadOnlyMemory{T}"/> wrapped in a <see cref="Nullable{T}"/>.
	/// </para>
	/// <para>
	/// For a corresponding equality check, use <see cref="MemoryExtensions.SequenceEqual{T}(ReadOnlySpan{T}, ReadOnlySpan{T})"/>.
	/// </para>
	/// </summary>
	public static int GetMemoryHashCode<TElement>(ReadOnlyMemory<TElement>? memory)
	{
		if (memory is null) return 0;
		return GetMemoryHashCode(memory.Value);
	}

	/// <summary>
	/// <para>
	/// Returns a hash code over some of the content of the given <see cref="Memory{T}"/>.
	/// </para>
	/// <para>
	/// For a corresponding equality check, use <see cref="MemoryExtensions.SequenceEqual{T}(ReadOnlySpan{T}, ReadOnlySpan{T})"/>.
	/// </para>
	/// </summary>
	public static int GetMemoryHashCode<TElement>(Memory<TElement> memory)
	{
		return GetMemoryHashCode((ReadOnlyMemory<TElement>)memory);
	}

	/// <summary>
	/// <para>
	/// Returns a hash code over some of the content of the given <see cref="ReadOnlyMemory{T}"/>.
	/// </para>
	/// <para>
	/// For a corresponding equality check, use <see cref="MemoryExtensions.SequenceEqual{T}(ReadOnlySpan{T}, ReadOnlySpan{T})"/>.
	/// </para>
	/// </summary>
	public static int GetMemoryHashCode<TElement>(ReadOnlyMemory<TElement> memory)
	{
		return GetSpanHashCode(memory.Span);
	}

	/// <summary>
	/// <para>
	/// Returns a hash code over some of the content of the given <see cref="Span{T}"/>.
	/// </para>
	/// <para>
	/// For a corresponding equality check, use <see cref="MemoryExtensions.SequenceEqual{T}(ReadOnlySpan{T}, ReadOnlySpan{T})"/>.
	/// </para>
	/// </summary>
	public static int GetSpanHashCode<TElement>(Span<TElement> span)
	{
		return GetSpanHashCode((ReadOnlySpan<TElement>)span);
	}

	/// <summary>
	/// <para>
	/// Returns a hash code over some of the content of the given <see cref="ReadOnlySpan{T}"/>.
	/// </para>
	/// <para>
	/// For a corresponding equality check, use <see cref="MemoryExtensions.SequenceEqual{T}(ReadOnlySpan{T}, ReadOnlySpan{T})"/>.
	/// </para>
	/// </summary>
	public static int GetSpanHashCode<TElement>(ReadOnlySpan<TElement> span)
	{
		// Note that we do not distinguish between a default span and a regular empty span
		// After all, this is structural equality, and such an equality comparison would not consider them to have a different structure either
		if (span.IsEmpty) return 1; // Differentiate between null and empty

		return HashCode.Combine(span.Length, span[0], span[^1]);
	}
}
