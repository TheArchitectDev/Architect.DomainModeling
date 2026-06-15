using System.Runtime.CompilerServices;

namespace Architect.DomainModeling;

/// <summary>
/// Provides extension methods related to <see cref="ValueObject"/> types.
/// </summary>
public static class ValueObjectExtensions
{
	/// <summary>
	/// Returns whether the current <typeparamref name="TValueObject"/> is equal to the type's default value, according to its own <see cref="IEquatable{T}.Equals(T)"/>.
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsDefault<TValueObject>(this TValueObject instance)
		where TValueObject : struct, IValueObject, IEquatable<TValueObject>
	{
		return instance.Equals(default);
	}
}
