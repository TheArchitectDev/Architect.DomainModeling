using System.Runtime.CompilerServices;

namespace Architect.DomainModeling.Generator;

internal static class GeneratableExtensions
{
	/// <summary>
	/// Unpacks the boolean value stored in the bit set at position <paramref name="position"/>.
	/// </summary>
	public static bool GetBit(this uint bits, int position)
	{
		var result = (bits >> position) & 1U;
		return Unsafe.As<uint, bool>(ref result);
	}

	/// <summary>
	/// Stores the given <paramref name="value"/> in the bit set at position <paramref name="position"/>.
	/// </summary>
	public static void SetBit(ref this uint bits, int position, bool value)
	{
		// Create a mask to unset the target bit: 1 << position
		// Unset the target bit: & (1 << position)

		// Create a mask to write the target bit value: 1 << value
		// Write the target bit: | (1 << value)

		bits = bits
			& ~(1U << position)
			| (Unsafe.As<bool, uint>(ref value) << position);
	}
}
