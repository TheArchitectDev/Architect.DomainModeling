namespace Architect.DomainModeling.Generator.Common;

/// <summary>
/// Wraps an <see cref="IReadOnlyList{T}"/> in a wrapper with structural equality using the collection's elements.
/// </summary>
/// <typeparam name="TCollection">The type of the collection to wrap.</typeparam>
/// <typeparam name="TElement">The type of the collection's elements.</typeparam>
internal sealed class StructuralList<TCollection, TElement>(
	TCollection value)
	: IEquatable<StructuralList<TCollection, TElement>>
	where TCollection : IReadOnlyList<TElement>
{
	public TCollection Value { get; } = value ?? throw new ArgumentNullException(nameof(value));

	public override int GetHashCode() => this.Value is TCollection value && value.Count > 0
		? CombineHashCodes(
			value.Count,
			value[0]?.GetHashCode() ?? 0,
			value[value.Count - 1]?.GetHashCode() ?? 0)
		: 0;
	public override bool Equals(object obj) => obj is StructuralList<TCollection, TElement> other && this.Equals(other);

	public bool Equals(StructuralList<TCollection, TElement> other)
	{
		if (other is null)
			return false;

		var left = this.Value;
		var right = other.Value;

		if (right.Count != left.Count)
			return false;

		for (var i = 0; i < left.Count; i++)
			if (left[i] is not TElement leftElement ? right[i] is not null : !leftElement.Equals(right[i]))
				return false;

		return true;
	}

	private static int CombineHashCodes(int count, int firstHashCode, int lastHashCode)
	{
		var countInHighBits = (ulong)count << 16;

		// In the upper half, combine the count with the first hash code
		// In the lower half, combine the count with the last hash code
		var combined = ((ulong)firstHashCode ^ countInHighBits) << 33; // Offset by 1 additional bit, because UInt64.GetHashCode() XORs its halves, which would cause 0 for identical first and last (e.g. single element)
		combined |= (ulong)lastHashCode ^ countInHighBits;

		return combined.GetHashCode();
	}

	public static implicit operator TCollection(StructuralList<TCollection, TElement> instance) => instance.Value;
	public static implicit operator StructuralList<TCollection, TElement>(TCollection value) => new StructuralList<TCollection, TElement>(value);
}
