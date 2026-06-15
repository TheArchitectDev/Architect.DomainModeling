using System.Runtime.CompilerServices;

namespace Architect.DomainModeling.Comparisons;

/// <summary>
/// Performs default comparisons with type inference, where the standard syntax does not allow for type inference.
/// </summary>
public static class InferredTypeDefaultComparer
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool Equals<T>(T left, T right)
	{
		return EqualityComparer<T>.Default.Equals(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Compare<T>(T left, T right)
	{
		return Comparer<T>.Default.Compare(left, right);
	}
}
